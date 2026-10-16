using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace PartyViewKR;

internal sealed unsafe partial class ListingRefreshHost
{
    // Supplied PFRemote 1.0.0.12: .cctor, EnqueueSlotJobsChange and FireEventOnNode.
    // Alliance navigation comes from the user's KR Addon Inspector observations.
    // The existing normal-party hover/click/out dispatch remains unchanged.
    private long roleOpenedRevision, roleClosedRevision;
    private SlotEditSession? slotEdit;
    private string slotCheck = "not_started";
    private string roleButtonRead = "";
    private string slotRouteRead = "not_started";
    private string SlotEditDiagnostic => $"slotEditCheck={slotCheck}; slotEditPhase={slotEdit?.Phase ?? "none"}; " +
        $"roleOpenedRevision={Interlocked.Read(ref roleOpenedRevision)}; roleClosedRevision={Interlocked.Read(ref roleClosedRevision)}; " +
        $"slotResetCalls={slotEdit?.ResetCalls ?? 0}; slotToggleCalls={slotEdit?.ToggleCalls ?? 0}; " +
        $"slotConfirmCalls={slotEdit?.ConfirmCalls ?? 0}; slotButtonsPlanned={slotEdit?.Sequence.Actions.Count ?? 0}\n" +
        $"slotRequestedFlags={slotEdit?.Edit.Selected.ToString("X16") ?? "unknown"}; " +
        $"slotKnownJobsMask={slotEdit?.Edit.Rules.AllFlags.ToString("X16") ?? "unknown"}; " +
        $"slotOriginalFlags={(slotEdit == null ? "unknown" : slotEdit.AllFlags[slotEdit.Edit.Position - 1].ToString("X16"))}; " +
        $"slotConfirmedFlags={(slotEdit?.ConfirmedTarget == null ? "unknown" : slotEdit.ConfirmedTarget.Slots.Slots[slotEdit.Edit.Position - 1].SlotFlags.ToString("X16"))}\n" +
        $"slotMemberSelectCalls={slotEdit?.MemberSelectCalls ?? 0}; {slotRouteRead}\n" +
        $"roleButtonRead={roleButtonRead}";

    internal void RoleOpened() => Interlocked.Increment(ref roleOpenedRevision);
    internal void RoleClosed() => Interlocked.Increment(ref roleClosedRevision);

    private sealed class SlotEditSession(RefreshTarget original, ListingJobEdit edit,
        ListingSlotRoute route, string invariant, ulong[] allFlags, RoleButtonSequence sequence, long openedRevision, long closedRevision)
    {
        internal readonly RefreshTarget Original = original;
        internal RefreshTarget? ConfirmedTarget;
        internal readonly ListingJobEdit Edit = edit;
        internal readonly ListingSlotRoute Route = route;
        internal readonly string Invariant = invariant;
        internal readonly ulong[] AllFlags = allFlags;
        internal readonly RoleButtonSequence Sequence = sequence;
        internal readonly long OpenedRevision = openedRevision, ClosedRevision = closedRevision;
        internal readonly long Started = Environment.TickCount64;
        internal string Phase = "opening";
        internal int ResetCalls, ToggleCalls, ConfirmCalls, MemberSelectCalls;
        internal long ConfirmedAt, MemberSelectedAt;
        internal bool Completed;
    }

    public bool BeginSlotEdit(RefreshTarget target, ListingJobEdit edit, out string error)
    {
        slotEdit = null; slotCheck = "validating"; roleButtonRead = ""; slotRouteRead = "not_started";
        error = "빈자리의 기존 모집 조건을 확인하지 못했습니다.";
        if (!edit.MatchesOriginal(target) || Visible("LookingForGroupSelectRole")) return false;
        if (!ListingSlotRoute.TryCreate(target.Slots, edit.Position, out var route))
        { slotCheck = "unsupported_slot_layout"; return false; }
        if (!ListingRoleButtons.TryPlan(edit.Rules, edit.Selected, out var sequence, out error))
        { slotCheck = "job_button_plan_unavailable"; return false; }
        if (!ValidateEditor(target, out var addon, out var agent, out error)) return false;
        ref var draft = ref agent->StoredRecruitmentInfo;
        slotRouteRead = $"slotLayout={route.Layout}; slotGroup={(char)('A' + route.GroupIndex)}; slotMember={route.MemberIndex + 1}; " +
            $"slotOpenNode={route.OpenNodeId}; slotOpenParam={route.OpenParam?.ToString() ?? "registered"}; " +
            $"slotMemberNode={route.MemberNodeId}; slotMemberParam={route.MemberParam}; " +
            $"editorGroups={draft.NumberOfGroups}; editorMainPartySlots={draft.NumberOfSlotsInMainParty}";
        if (!route.MatchesEditorShape(draft.NumberOfGroups, draft.NumberOfSlotsInMainParty) ||
            !draft.MemberContentIds[..target.TotalSlots].SequenceEqual(target.Slots.Slots.Select(s => s.MemberContentId).ToArray()))
        { slotCheck = "editor_members_or_shape_changed"; error = "모집 수정 창의 인원 또는 파티 구성이 달라 중단했습니다."; return false; }
        if (!TryEventNode((AtkUnitBase*)addon, route.OpenNodeId, out var node, out var component, out var reason))
        { slotCheck = $"slot_button_unavailable; reason={reason}"; error = "선택한 빈자리의 게임 버튼을 확인하지 못했습니다."; return false; }
        var openParam = route.OpenParam ?? 0;
        // Use the verified normal/48p constants directly, like PFRemote.
        // Only 24p lacks a supplied Param and needs registration lookup.
        if (route.OpenParam == null &&
            !TryRegisteredClickParam((AtkUnitBase*)addon, node, out openParam, out reason))
        { slotCheck = $"slot_click_unverified; nodeId={route.OpenNodeId}; reason={reason}"; error = "선택한 자리의 클릭 이벤트를 확인하지 못했습니다."; return false; }
        slotRouteRead += $"; slotResolvedOpenParam={openParam}; slotOpenParamSource={(route.OpenParam.HasValue ? "verified_map" : "registered_event")}";
        slotEdit = new(target, edit, route, DraftInvariant(agent), draft.SlotFlags.ToArray(), sequence,
            Interlocked.Read(ref roleOpenedRevision), Interlocked.Read(ref roleClosedRevision));
        slotCheck = "opening_slot";
        // Normal / 24p: a slot button. 48p: a party button, followed by a
        // member selector only after the new role window is ready.
        if (!route.SelectMember) FireNode((AtkUnitBase*)addon, node, component, AtkEventType.MouseOver, openParam);
        FireNode((AtkUnitBase*)addon, node, component, AtkEventType.ButtonClick, openParam);
        if (!route.SelectMember) FireNode((AtkUnitBase*)addon, node, component, AtkEventType.MouseOut, openParam);
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
            session.ConfirmedTarget = session.Edit.Expected(session.Original,
                agent->StoredRecruitmentInfo.SlotFlags[session.Edit.Position - 1]);
            editingTarget = session.ConfirmedTarget;
            if (!ValidateEditor(session.ConfirmedTarget, out _, out _, out error)) return SlotEditProgress.Failed;
            session.Completed = true; session.Phase = "complete"; slotCheck = "slot_change_verified";
            expected = session.ConfirmedTarget;
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
            if (session.Route.SelectMember)
            {
                if (!TryEventNode(role, session.Route.MemberNodeId, out var memberNode, out var memberComponent, out var reason))
                {
                    slotCheck = $"member_button_unavailable; nodeId={session.Route.MemberNodeId}; reason={reason}";
                    error = "직업 선택창의 파티원 선택 버튼을 확인하지 못했습니다.";
                    return now - session.Started <= 3000 ? SlotEditProgress.Waiting : SlotEditProgress.Failed;
                }
                // Advance BEFORE dispatch. A failed click must never be retried.
                session.Phase = "selecting_member";
                session.MemberSelectedAt = now;
                session.MemberSelectCalls++;
                slotCheck = $"selecting_member; nodeId={session.Route.MemberNodeId}; param={session.Route.MemberParam}";
                FireNode(role, memberNode, memberComponent, AtkEventType.ButtonClick, session.Route.MemberParam);
                return SlotEditProgress.Waiting;
            }
            if (!CheckRoleNodes(role, session.Sequence, out error))
                return now - session.Started <= 3000 ? SlotEditProgress.Waiting : SlotEditProgress.Failed;
            session.Phase = "selecting";
        }
        if (session.Phase == "selecting_member")
        {
            if (now - session.MemberSelectedAt < 200) return SlotEditProgress.Waiting;
            if (!CheckRoleNodes(role, session.Sequence, out error))
                return now - session.MemberSelectedAt <= 3000 ? SlotEditProgress.Waiting : SlotEditProgress.Failed;
            session.Phase = "selecting";
        }
        foreach (var next in session.Sequence.TakeBatch(now))
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
            if (!available) error = "게임 직업 선택창의 버튼이 준비되지 않았습니다. 진단 정보를 확인해 주세요.";
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
            if (i == session.Edit.Position - 1)
            {
                if (!final) continue;
                if (!session.Edit.MatchesSelectedFlags(flags[i]))
                {
                    slotCheck = $"editor_job_selection_mismatch; position={i + 1}; requestedFlags={session.Edit.Selected:X16}; " +
                        $"knownJobsMask={session.Edit.Rules.AllFlags:X16}; actualFlags={flags[i]:X16}";
                    error = "게임 수정창의 허용 직업이 선택한 내용과 달라 중단했습니다.";
                    return false;
                }
                if (session.ConfirmedTarget == null) continue;
            }
            // Once the game draft has been verified, pin its exact raw value
            // until Submit. Unedited slots are always compared byte for byte.
            var expected = i == session.Edit.Position - 1 ?
                session.ConfirmedTarget!.Slots.Slots[i].SlotFlags : session.AllFlags[i];
            if (flags[i] != expected)
            {
                slotCheck = $"editor_slot_mismatch; position={i + 1}; expectedFlags={expected:X16}; actualFlags={flags[i]:X16}";
                return false;
            }
        }
        error = "";
        return true;
    }

    private static string DraftInvariant(AgentLookingForGroup* agent, bool includeComment = true, bool includeObjective = true, bool includeBeginner = true, bool includeUniqueJobs = true, bool includePassword = true, bool includeCompletion = true, bool includeItemLevel = true)
    {
        ref var draft = ref agent->StoredRecruitmentInfo;
        if (!NativeReadGuard.FixedText(draft.Comment, out var comment)) throw new InvalidDataException("draft_comment_unreadable");
        return Criteria((uint)draft.SelectedCategory, draft.SelectedDutyId, includeObjective ? (byte)draft.Objective : (byte)0,
            includeBeginner ? draft.BeginnerFriendly : (byte)0,
            includeCompletion ? (byte)draft.CompletionStatus : (byte)0, (byte)draft.DutyFinderSettingFlags, (byte)draft.LootRule, (byte)draft.LanguageFlags, []) +
            $"/{(includePassword ? draft.Password : (ushort)0)}/{draft.NumberOfSlotsInMainParty}/{draft.NumberOfGroups}/{draft.LimitRecruitingToWorld}/" +
            $"{(includeUniqueJobs ? draft.OnePlayerPerJob : (byte)0)}/{(includeItemLevel ? agent->AvgItemLv : (ushort)0)}/{(includeItemLevel ? agent->AvgItemLvEnabled : (byte)0)}/" +
            Convert.ToHexString(MemoryMarshal.AsBytes(draft.MemberContentIds)) + (includeComment ? "/" + comment : "");
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

    private static bool TryRegisteredClickParam(AtkUnitBase* addon, AtkResNode* node,
        out int param, out string reason)
    {
        param = 0; reason = "click_event_missing";
        // A bounded, read-only walk. Never substitute a guessed callback when
        // the known node has no unique click registered to the expected addon.
        var seen = new HashSet<nuint>();
        var matches = 0;
        for (var evt = node->AtkEventManager.Event; evt != null; evt = evt->NextEvent)
        {
            if (seen.Count >= 32 || !seen.Add((nuint)evt))
            { reason = "event_chain_invalid"; return false; }
            if (!NativeReadGuard.Readable(evt, (nuint)sizeof(AtkEvent)))
            { reason = "event_unreadable"; return false; }
            if (evt->State.EventType != AtkEventType.ButtonClick) continue;
            // AtkEvent.Node is an optional extra node argument, not the button
            // identity. RegisterEvent accepts nodeParam separately from target.
            // See FFXIVClientStructs AtkEvent.cs / AtkEventManager.cs.
            if (evt->Target != (AtkEventTarget*)node)
            { reason = "event_target_mismatch"; return false; }
            if (evt->Listener != (AtkEventListener*)addon)
            { reason = "event_listener_mismatch"; return false; }
            if (++matches != 1 || evt->Param > int.MaxValue)
            { reason = "click_event_ambiguous"; return false; }
            param = (int)evt->Param;
        }
        if (matches != 1) return false;
        reason = "ok";
        return true;
    }
}
