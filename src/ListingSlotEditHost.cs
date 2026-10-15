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
    private static readonly uint[] JobNodeCandidates =
        [18, 19, 20, 21, 23, 24, 30, 31, 32, 33, 35, 42, 43, 44, 45, 46,
         47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 58, 59, 60, 61, 62, 63];
    private long roleOpenedRevision, roleClosedRevision;
    private SlotEditSession? slotEdit;
    private string slotCheck = "not_started";
    private string roleNameCheck = "not_started", roleButtonRead = "", roleNameRead = "";
    private int matchedRoleButtons;
    private string SlotEditDiagnostic => $"slotEditCheck={slotCheck}; slotEditPhase={slotEdit?.Phase ?? "none"}; " +
        $"roleButtonsMatched={matchedRoleButtons}; roleOpenedRevision={Interlocked.Read(ref roleOpenedRevision)}; " +
        $"roleClosedRevision={Interlocked.Read(ref roleClosedRevision)}; slotToggleCalls={slotEdit?.ToggleCalls ?? 0}\n" +
        $"roleNameCheck={roleNameCheck}\nroleButtonRead={roleButtonRead}\nroleNameRead={roleNameRead}";

    internal void RoleOpened() => Interlocked.Increment(ref roleOpenedRevision);
    internal void RoleClosed() => Interlocked.Increment(ref roleClosedRevision);

    private sealed class SlotEditSession(RefreshTarget original, RefreshTarget expected, ListingJobEdit edit,
        string invariant, ulong[] allFlags, long openedRevision, long closedRevision)
    {
        internal readonly RefreshTarget Original = original, Expected = expected;
        internal readonly ListingJobEdit Edit = edit;
        internal readonly string Invariant = invariant;
        internal readonly ulong[] AllFlags = allFlags;
        internal readonly long OpenedRevision = openedRevision, ClosedRevision = closedRevision;
        internal readonly long Started = Environment.TickCount64;
        internal string Phase = "opening";
        internal int ToggleCalls;
        internal bool Completed;
        internal JobToggleSequence? Toggles;
    }

    public bool BeginSlotEdit(RefreshTarget target, ListingJobEdit edit, out string error)
    {
        slotEdit = null; matchedRoleButtons = 0; slotCheck = "validating";
        roleNameCheck = "not_started"; roleButtonRead = roleNameRead = "";
        error = "빈자리의 기존 모집 조건을 확인하지 못했습니다.";
        if (!edit.MatchesOriginal(target) || Visible("LookingForGroupSelectRole")) return false;
        if (!ValidateEditor(target, out var addon, out var agent, out error)) return false;
        ref var draft = ref agent->StoredRecruitmentInfo;
        if (draft.NumberOfGroups != 1 || draft.NumberOfSlotsInMainParty != target.TotalSlots ||
            !draft.MemberContentIds[..target.TotalSlots].SequenceEqual(target.Slots.Slots.Select(s => s.MemberContentId).ToArray()))
        { slotCheck = "editor_members_or_shape_changed"; error = "모집 수정 창의 인원 또는 파티 구성이 달라 중단했습니다."; return false; }
        if (!TryButton((AtkUnitBase*)addon, (uint)(50 + edit.Position), ComponentType.Button, out var node, out var button))
        { slotCheck = "slot_button_unavailable"; error = "선택한 빈자리의 게임 버튼을 확인하지 못했습니다."; return false; }
        slotEdit = new(target, edit.Expected(target), edit, DraftInvariant(agent), draft.SlotFlags.ToArray(),
            Interlocked.Read(ref roleOpenedRevision), Interlocked.Read(ref roleClosedRevision));
        slotCheck = "opening_slot";
        // Dispatch once, matching the verified hover/click/out event sequence.
        FireNode((AtkUnitBase*)addon, node, button, AtkEventType.MouseOver, 8);
        FireNode((AtkUnitBase*)addon, node, button, AtkEventType.ButtonClick, 8);
        FireNode((AtkUnitBase*)addon, node, button, AtkEventType.MouseOut, 8);
        error = "";
        return true;
    }

    public SlotEditProgress UpdateSlotEdit(out RefreshTarget expected, out string error)
    {
        expected = null!; error = "직업 수정 상태를 확인하지 못했습니다.";
        var session = slotEdit;
        if (session == null) { error = "진행 중인 직업 수정이 없습니다."; return SlotEditProgress.Failed; }
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
            if (!ValidateSlotGuard(agent, session, true, out error)) return SlotEditProgress.Failed;
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
            if (session.Phase != "opening" || Environment.TickCount64 - session.Started > 7000)
            { error = "직업 선택 창을 확인하지 못했습니다."; return SlotEditProgress.Failed; }
            return SlotEditProgress.Waiting;
        }
        if (!ReadJobButtons(role, session.Edit.Rules, out var nodes, out var observed, out error))
        {
            // Setup can precede registration of the hover tooltips. Retry only
            // these reads before the first click; never repeat a sent toggle.
            if (session.Phase == "opening" && Environment.TickCount64 - session.Started <= 3000)
                return SlotEditProgress.Waiting;
            return SlotEditProgress.Failed;
        }
        if (session.Phase == "opening")
        {
            var originalFlags = session.Original.Slots.Slots[session.Edit.Position - 1].SlotFlags & session.Edit.Rules.AllFlags;
            if (observed != originalFlags)
            { slotCheck = $"initial_role_mask_mismatch; expected={originalFlags:X16}; observed={observed:X16}"; error = "직업 선택 창이 선택한 자리의 기존 조건과 다릅니다."; return SlotEditProgress.Failed; }
            session.Toggles = new(originalFlags, session.Edit.Selected, session.Edit.Rules.Choices);
            session.Phase = "selecting";
        }
        if (!session.Toggles!.Next(observed, Environment.TickCount64, out var next, out var complete, out error))
            return SlotEditProgress.Failed;
        if (next != null)
        {
            if (!TryButton(role, nodes[next.Id], ComponentType.CheckBox, out var node, out var button))
            { error = "직업 체크박스가 준비되지 않았습니다."; return SlotEditProgress.Failed; }
            session.ToggleCalls++;
            slotCheck = $"toggling_job; jobId={next.Id}";
            FireNode(role, node, button, AtkEventType.ButtonClick, 3);
        }
        else if (complete)
        {
            if (!TryButton(role, 69, ComponentType.Button, out var node, out var button))
            { error = "직업 선택 확인 버튼을 찾지 못했습니다."; return SlotEditProgress.Failed; }
            session.Phase = "closing"; slotCheck = "confirming_jobs_once";
            FireNode(role, node, button, AtkEventType.ButtonClick, 0);
        }
        return SlotEditProgress.Waiting;
    }

    private bool ReadJobButtons(AtkUnitBase* role, ListingJobRules rules, out Dictionary<uint, uint> nodes,
        out ulong flags, out string error)
    {
        nodes = []; flags = 0; error = "";
        matchedRoleButtons = 0;
        if (!ListingRoleTooltips.Read(role, out var tooltips, out roleNameCheck))
        {
            slotCheck = "role_tooltips_unreadable";
            error = "게임 직업 선택창의 툴팁 정보를 읽지 못했습니다. 진단 정보를 확인해 주세요.";
            return false;
        }
        var observations = new List<RoleButtonNames>();
        var buttonReads = new List<string>();
        var nameReads = new List<string>();
        foreach (var nodeId in JobNodeCandidates)
        {
            if (!TryButton(role, nodeId, ComponentType.CheckBox, out var node, out var button, out var reason, requireEnabled: false))
            { buttonReads.Add($"{nodeId}:{reason}"); continue; }
            NativeReadGuard.TextNode(button->ButtonTextNode, out var label);
            var hoverNames = ListingRoleTooltips.ForButton(node, button, tooltips);
            observations.Add(new(nodeId, button->IsChecked, label, hoverNames));
            buttonReads.Add($"{nodeId}:ok,tooltip={hoverNames.Length},textChars={label.Length}");
            var preview = string.Join("|", hoverNames.Append(label).Where(n => n.Length > 0).Distinct())
                .Replace('\r', ' ').Replace('\n', ' ').Replace(';', ',');
            nameReads.Add($"{nodeId}:{(preview.Length > 80 ? preview[..80] : preview)}");
        }
        roleButtonRead = string.Join(";", buttonReads);
        roleNameRead = string.Join(";", nameReads);
        var result = ListingRoleNames.Read(rules, observations);
        nodes = result.Nodes; flags = result.CheckedFlags;
        matchedRoleButtons = nodes.Count;
        if (result.Complete) return true;
        slotCheck = $"role_job_names_incomplete; missingJobIds={string.Join(",", result.MissingJobs)}; " +
            $"ambiguousNodes={string.Join(",", result.AmbiguousNodes)}; duplicateJobIds={string.Join(",", result.DuplicateJobs)}";
        error = "게임 직업 선택창의 버튼과 툴팁 이름을 모두 연결하지 못했습니다. 진단 정보를 확인해 주세요.";
        return false;
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
            if (flags[i] != expected) { slotCheck = $"editor_slot_mismatch; position={i + 1}"; return false; }
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

    private static bool TryButton(AtkUnitBase* addon, uint id, ComponentType type, out AtkResNode* node,
        out AtkComponentButton* button, bool requireEnabled = true)
        => TryButton(addon, id, type, out node, out button, out _, requireEnabled);

    private static bool TryButton(AtkUnitBase* addon, uint id, ComponentType type, out AtkResNode* node,
        out AtkComponentButton* button, out string reason, bool requireEnabled = true)
    {
        node = null; button = null; reason = "addon_not_ready";
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
        var component = componentNode->Component;
        if (!NativeReadGuard.Readable(component, (nuint)sizeof(AtkComponentButton))) return false;
        var actualType = component->GetComponentType();
        reason = $"component_type_{(int)actualType}";
        if (actualType != type) return false;
        button = (AtkComponentButton*)component;
        reason = "owner_mismatch";
        if (button->OwnerNode != componentNode) return false;
        reason = "disabled";
        if (requireEnabled && !button->IsEnabled) return false;
        reason = "vtable_unreadable";
        if (!NativeReadGuard.Readable(addon->VirtualTable, (nuint)sizeof(AtkUnitBase.AtkUnitBaseVirtualTable))) return false;
        reason = "ok";
        return true;
    }

    private static void FireNode(AtkUnitBase* addon, AtkResNode* node, AtkComponentButton* button, AtkEventType type, int param)
    {
        var evt = new AtkEvent { Param = (uint)param, Node = node, Target = (AtkEventTarget*)node, Listener = (AtkEventListener*)button };
        var data = new AtkEventData();
        addon->ReceiveEvent(type, param, &evt, &data);
    }
}
