using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace PartyViewKR;

internal sealed unsafe partial class ListingRefreshHost
{
    // Supplied PFRemote 1.0.0.12: .cctor, EnqueueSlotJobsChange and FireEventOnNode.
    // Only normal-party slot nodes 51–58 are established. Do not reuse these
    // positions for alliance/custom-match tabs without verifying their mapping.
    private long roleOpenedRevision, roleClosedRevision;
    private SlotEditSession? slotEdit;
    private string slotCheck = "not_started";
    private string roleButtonRead = "";
    private string SlotEditDiagnostic => $"slotEditCheck={slotCheck}; slotEditPhase={slotEdit?.Phase ?? "none"}; " +
        $"roleOpenedRevision={Interlocked.Read(ref roleOpenedRevision)}; roleClosedRevision={Interlocked.Read(ref roleClosedRevision)}; " +
        $"slotResetCalls={slotEdit?.ResetCalls ?? 0}; slotToggleCalls={slotEdit?.ToggleCalls ?? 0}; " +
        $"slotConfirmCalls={slotEdit?.ConfirmCalls ?? 0}; slotButtonsPlanned={slotEdit?.Sequence.Actions.Count ?? 0}\n" +
        $"roleButtonRead={roleButtonRead}";

    internal void RoleOpened() => Interlocked.Increment(ref roleOpenedRevision);
    internal void RoleClosed() => Interlocked.Increment(ref roleClosedRevision);

    private sealed class SlotEditSession(RefreshTarget original, RefreshTarget expected, ListingJobEdit edit,
        string invariant, ulong[] allFlags, RoleButtonSequence sequence, long openedRevision, long closedRevision)
    {
        internal readonly RefreshTarget Original = original, Expected = expected;
        internal readonly ListingJobEdit Edit = edit;
        internal readonly string Invariant = invariant;
        internal readonly ulong[] AllFlags = allFlags;
        internal readonly RoleButtonSequence Sequence = sequence;
        internal readonly long OpenedRevision = openedRevision, ClosedRevision = closedRevision;
        internal readonly long Started = Environment.TickCount64;
        internal string Phase = "opening";
        internal int ResetCalls, ToggleCalls, ConfirmCalls;
        internal long ConfirmedAt;
        internal bool Completed;
    }

    public bool BeginSlotEdit(RefreshTarget target, ListingJobEdit edit, out string error)
    {
        slotEdit = null; slotCheck = "validating"; roleButtonRead = "";
        error = "빈자리의 기존 모집 조건을 확인하지 못했습니다.";
        if (!edit.MatchesOriginal(target) || Visible("LookingForGroupSelectRole")) return false;
        if (!ListingRoleButtons.TryPlan(edit.Rules, edit.Selected, out var sequence, out error))
        { slotCheck = "job_button_plan_unavailable"; return false; }
        if (!ValidateEditor(target, out var addon, out var agent, out error)) return false;
        ref var draft = ref agent->StoredRecruitmentInfo;
        if (draft.NumberOfGroups != 1 || draft.NumberOfSlotsInMainParty != target.TotalSlots ||
            !draft.MemberContentIds[..target.TotalSlots].SequenceEqual(target.Slots.Slots.Select(s => s.MemberContentId).ToArray()))
        { slotCheck = "editor_members_or_shape_changed"; error = "모집 수정 창의 인원 또는 파티 구성이 달라 중단했습니다."; return false; }
        if (!TryEventNode((AtkUnitBase*)addon, (uint)(50 + edit.Position), out var node, out var component, out var reason))
        { slotCheck = $"slot_button_unavailable; reason={reason}"; error = "선택한 빈자리의 게임 버튼을 확인하지 못했습니다."; return false; }
        slotEdit = new(target, edit.Expected(target), edit, DraftInvariant(agent), draft.SlotFlags.ToArray(), sequence,
            Interlocked.Read(ref roleOpenedRevision), Interlocked.Read(ref roleClosedRevision));
        slotCheck = "opening_slot";
        // Dispatch once, matching the verified hover/click/out event sequence.
        FireNode((AtkUnitBase*)addon, node, component, AtkEventType.MouseOver, 8);
        FireNode((AtkUnitBase*)addon, node, component, AtkEventType.ButtonClick, 8);
        FireNode((AtkUnitBase*)addon, node, component, AtkEventType.MouseOut, 8);
        error = "";
        return true;
    }

    public SlotEditProgress UpdateSlotEdit(out RefreshTarget expected, out string error)
    {
        expected = null!; error = "직업 수정 상태를 확인하지 못했습니다.";
        var session = slotEdit;
        if (session == null) { error = "진행 중인 직업 수정이 없습니다."; return SlotEditProgress.Failed; }
        var now = Environment.TickCount64;
        ReadEditorAction(); // A child role dialog may temporarily disable the submit button.
        if (SelfId != session.Original.SelfId || BlockReason != null ||
            !ReferenceEquals(editingTarget, session.Original) || EditorClosedRevision != editingFromClosedRevision ||
            !buttonTextReadable || buttonMode != RecruitmentEditorMode.Refresh)
        { slotCheck = "editor_binding_changed"; error = "모집 수정 창의 상태가 바뀌어 중단했습니다."; return SlotEditProgress.Failed; }
        var agent = AgentLookingForGroup.Instance();
        if (!NativeReadGuard.Readable(agent, (nuint)sizeof(AgentLookingForGroup)) ||
            !ValidateSlotGuard(agent, session, false, out error)) return SlotEditProgress.Failed;
        var role = Plugin.GameGui.GetAddonByName<AtkUnitBase>("LookingForGroupSelectRole", 1);
        var roleVisible = NativeReadGuard.Readable(role, (nuint)sizeof(AtkUnitBase)) && role->IsVisible;
        var opened = Interlocked.Read(ref roleOpenedRevision);
        var closed = Interlocked.Read(ref roleClosedRevision);
        if (session.Phase == "closing")
        {
            if (opened != session.OpenedRevision + 1 || closed > session.ClosedRevision + 1)
            { error = "직업 선택 창이 다시 열려 중단했습니다."; return SlotEditProgress.Failed; }
            if (roleVisible) return SlotEditProgress.Waiting;
            if (!buttonEnabled) return SlotEditProgress.Waiting;
            if (!ValidateSlotGuard(agent, session, true, out error))
            {
                // The child dialog can disappear before the draft flags update.
                // Read only while waiting; never resend Confirm or any job event.
                return now - session.ConfirmedAt <= 3000 ? SlotEditProgress.Waiting : SlotEditProgress.Failed;
            }
            editingTarget = session.Expected;
            if (!ValidateEditor(session.Expected, out _, out _, out error)) return SlotEditProgress.Failed;
            session.Completed = true; session.Phase = "complete"; slotCheck = "slot_change_verified";
            expected = session.Expected;
            return SlotEditProgress.Complete;
        }
        if (closed != session.ClosedRevision || opened > session.OpenedRevision + 1)
        { error = "직업 선택 창이 닫히거나 다시 열려 중단했습니다."; return SlotEditProgress.Failed; }
        if (!roleVisible || !role->IsReady || opened <= session.OpenedRevision)
        {
            if (session.Phase != "opening" || now - session.Started > 7000)
            { error = "직업 선택 창을 확인하지 못했습니다."; return SlotEditProgress.Failed; }
            return SlotEditProgress.Waiting;
        }
        if (session.Phase == "opening")
        {
            if (now - session.Started < 200) return SlotEditProgress.Waiting;
            if (!CheckRoleNodes(role, session.Sequence, out error))
                return now - session.Started <= 3000 ? SlotEditProgress.Waiting : SlotEditProgress.Failed;
            session.Phase = "selecting";
        }
        var next = session.Sequence.Take(now);
        if (next != null)
        {
            if (!TryEventNode(role, next.NodeId, out var node, out var component, out var reason))
            {
                slotCheck = $"role_button_unavailable; nodeId={next.NodeId}; reason={reason}";
                error = "직업 선택창의 버튼을 확인하지 못했습니다.";
                return SlotEditProgress.Failed;
            }
            switch (next.Kind)
            {
                case RoleButtonActionKind.Anyone: session.ResetCalls++; break;
                case RoleButtonActionKind.ExcludeJob: session.ToggleCalls++; break;
                case RoleButtonActionKind.Confirm:
                    session.ConfirmCalls++;
                    session.ConfirmedAt = now;
                    session.Phase = "closing";
                    break;
            }
            slotCheck = $"dispatching_{next.Kind}; nodeId={next.NodeId}; jobId={next.JobId}; param={next.Param}";
            FireNode(role, node, component, AtkEventType.ButtonClick, next.Param);
        }
        return SlotEditProgress.Waiting;
    }

    private bool CheckRoleNodes(AtkUnitBase* role, RoleButtonSequence sequence, out string error)
    {
        error = "";
        var reads = new List<string>();
        foreach (var action in sequence.Actions)
        {
            var available = TryEventNode(role, action.NodeId, out _, out _, out var reason);
            reads.Add($"{action.NodeId}:{reason}");
            if (!available) error = "ゲーム 직업 선택창의 버튼이 준비되지 않았습니다. 진단 정보를 확인해 주세요.";
        }
        roleButtonRead = string.Join(";", reads);
        slotCheck = error.Length == 0 ? "role_nodes_ready" : "role_nodes_unavailable";
        return error.Length == 0;
    }

    private bool ValidateSlotGuard(AgentLookingForGroup* agent, SlotEditSession session, bool final, out string error)
    {
        error = "직업 수정 중 다른 모집 조건 또는 참가자가 바뀌어 중단했습니다.";
        if (DraftInvariant(agent) != session.Invariant) { slotCheck = "editor_invariant_changed"; return false; }
        var flags = agent->StoredRecruitmentInfo.SlotFlags;
        for (var i = 0; i < flags.Length; i++)
        {
            if (i == session.Edit.Position - 1 && !final) continue;
            var expected = i == session.Edit.Position - 1 ? session.Edit.DesiredFlags : session.AllFlags[i];
            if (flags[i] != expected)
            {
                slotCheck = $"editor_slot_mismatch; position={i + 1}; expectedFlags={expected:X16}; actualFlags={flags[i]:X16}";
                return false;
            }
        }
        error = "";
        return true;
    }

    private static string DraftInvariant(AgentLookingForGroup* agent)
    {
        ref var draft = ref agent->StoredRecruitmentInfo;
        if (!NativeReadGuard.FixedText(draft.Comment, out var comment)) throw new InvalidDataException("draft_comment_unreadable");
        return Criteria((uint)draft.SelectedCategory, draft.SelectedDutyId, (byte)draft.Objective, draft.BeginnerFriendly,
            (byte)draft.CompletionStatus, (byte)draft.DutyFinderSettingFlags, (byte)draft.LootRule, (byte)draft.LanguageFlags, []) +
            $"/{draft.Password}/{draft.NumberOfSlotsInMainParty}/{draft.NumberOfGroups}/{draft.LimitRecruitingToWorld}/" +
            $"{draft.OnePlayerPerJob}/{agent->AvgItemLv}/{agent->AvgItemLvEnabled}/" +
            Convert.ToHexString(MemoryMarshal.AsBytes(draft.MemberContentIds)) + "/" + comment;
    }

    private static bool TryEventNode(AtkUnitBase* addon, uint id, out AtkResNode* node,
        out AtkComponentBase* component, out string reason)
    {
        node = null; component = null; reason = "addon_not_ready";
        if (!NativeReadGuard.Readable(addon, (nuint)sizeof(AtkUnitBase)) || !addon->IsVisible || !addon->IsReady) return false;
        reason = "node_unreadable";
        node = addon->GetNodeById(id);
        if (!NativeReadGuard.Readable(node, (nuint)sizeof(AtkResNode))) return false;
        reason = "node_hidden";
        if (!node->IsVisible()) return false;
        reason = "component_node_unreadable";
        var componentNode = node->GetAsAtkComponentNode();
        if (!NativeReadGuard.Readable(componentNode, (nuint)sizeof(AtkComponentNode))) return false;
        reason = "component_unreadable";
        component = componentNode->Component;
        if (!NativeReadGuard.Readable(component, (nuint)sizeof(AtkComponentBase))) return false;
        // The original dispatch uses the component as the event listener.
        // These icon buttons need not be CheckBox components or expose IsChecked.
        reason = "owner_mismatch";
        if (component->OwnerNode != componentNode) return false;
        reason = "vtable_unreadable";
        if (!NativeReadGuard.Readable(addon->VirtualTable, (nuint)sizeof(AtkUnitBase.AtkUnitBaseVirtualTable))) return false;
        reason = "ok";
        return true;
    }

    private static void FireNode(AtkUnitBase* addon, AtkResNode* node, AtkComponentBase* component, AtkEventType type, int param)
    {
        var evt = new AtkEvent { Param = (uint)param, Node = node, Target = (AtkEventTarget*)node, Listener = (AtkEventListener*)component };
        var data = new AtkEventData();
        addon->ReceiveEvent(type, param, &evt, &data);
    }
}
