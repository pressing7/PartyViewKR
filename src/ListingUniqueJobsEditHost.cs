using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace PartyViewKR;

internal sealed unsafe partial class ListingRefreshHost
{
    private UniqueJobsSession? uniqueJobsSession;
    private string uniqueJobsCheck = "not_started";
    private string UniqueJobsEditDiagnostic => $"uniqueJobsEditCheck={uniqueJobsCheck}; uniqueJobsEditPhase={uniqueJobsSession?.Ui.Phase ?? "none"}; " +
        $"uniqueJobsDirectCallbackCalls=0; uniqueJobsCheckCalls={uniqueJobsSession?.CheckCalls ?? 0}; uniqueJobsClickCalls={uniqueJobsSession?.ClickCalls ?? 0}; " +
        $"uniqueJobsRequested={uniqueJobsSession?.Edit.Enabled.ToString() ?? "unknown"}; uniqueJobsUiChecked={uniqueJobsSession?.State.Checked.ToString() ?? "unknown"}; " +
        $"uniqueJobsStoredValue={uniqueJobsSession?.State.Stored.ToString() ?? "unknown"}; uniqueJobsNode={uniqueJobsSession?.NodeId.ToString() ?? "unknown"}; uniqueJobsParam=32";

    private sealed class UniqueJobsSession(RefreshTarget original, ListingUniqueJobsEdit edit, string invariant,
        AddonLookingForGroupCondition* addon, AgentLookingForGroup* agent, AtkComponentCheckBox* checkbox)
    {
        internal readonly RefreshTarget Original = original;
        internal readonly ListingUniqueJobsEdit Edit = edit;
        internal readonly RefreshTarget Expected = edit.Expected(original);
        internal readonly string Invariant = invariant;
        internal readonly uint NodeId = checkbox->OwnerNode->NodeId;
        internal readonly nint Addon = (nint)addon, Agent = (nint)agent, Checkbox = (nint)checkbox, Node = (nint)checkbox->OwnerNode;
        internal readonly ListingCheckboxUi Ui = new(ListingUniqueJobs.Read(original.ExtraCriteria)!.Value, edit.Enabled, "unique_jobs");
        internal CheckboxUiState State;
        internal int CheckCalls, ClickCalls;
        internal bool Completed;
    }

    public bool BeginUniqueJobsEdit(RefreshTarget target, ListingUniqueJobsEdit edit, out string error)
    {
        uniqueJobsSession = null; uniqueJobsCheck = "validating";
        error = "기존 잡 중복 없음 설정을 확인하지 못했습니다.";
        if (!edit.MatchesOriginal(target) || Visible("LookingForGroupSelectRole")) return false;
        if (!ValidateEditor(target, out var addon, out var agent, out error)) return false;
        if (!ReadUniqueJobsControl(addon, agent, out var checkbox, out var state, out _, out error)) return false;
        if (!ListingSlotRoute.TryCreate(target.Slots, 1, out var route) ||
            !route.MatchesEditorShape(agent->StoredRecruitmentInfo.NumberOfGroups, agent->StoredRecruitmentInfo.NumberOfSlotsInMainParty) ||
            !target.Slots.MatchesMembers(agent->StoredRecruitmentInfo.MemberContentIds))
        { uniqueJobsCheck = "members_or_shape_changed"; error = "모집 수정창의 파티 구성이 달라 중단했습니다."; return false; }

        var session = uniqueJobsSession = new(target, edit, UniqueJobsInvariant(agent), addon, agent, checkbox) { State = state };
        var action = session.Ui.Next(state, Environment.TickCount64);
        uniqueJobsCheck = session.Ui.Check;
        if (action != CheckboxUiAction.Click)
        { error = "잡 중복 없음의 기존 체크 상태와 저장값이 일치하지 않아 중단했습니다."; return false; }

        // Manual KR trace: registered ButtonClick(25), Param 32 -> editor.
        // The native checkbox controls the check state. The game creates
        // callback 23 with value 1/0; PVKR neither builds that callback nor
        // writes StoredRecruitmentInfo.OnePlayerPerJob.
        session.CheckCalls++;
        checkbox->SetChecked(edit.Enabled);
        if (!ValidateUniqueJobsGuard(addon, agent, session, false, out var clickEvent, out error)) return false;
        if (session.State.Checked != edit.Enabled || ListingCheckboxValues.Decode(session.State.Stored) != ListingUniqueJobs.Read(target.ExtraCriteria))
        { uniqueJobsCheck = "unique_jobs_preclick_state_mismatch"; error = "체크박스의 클릭 준비 상태를 확인하지 못했습니다."; return false; }
        session.ClickCalls++;
        // Preserve the registered optional Node argument. The manual trace
        // reports nodeId=unknown; it is not the component's identity.
        clickEvent.NextEvent = null;
        var data = new AtkEventData();
        addon->ReceiveEvent(AtkEventType.ButtonClick, (int)clickEvent.Param, &clickEvent, &data);
        uniqueJobsCheck = "unique_jobs_click_requested";
        error = "";
        return true;
    }

    public SlotEditProgress UpdateUniqueJobsEdit(out RefreshTarget expected, out string error)
    {
        expected = null!; error = "잡 중복 없음 설정을 확인하지 못했습니다.";
        var session = uniqueJobsSession;
        if (session == null || session.Completed) return SlotEditProgress.Failed;
        if (!ReferenceEquals(editingTarget, session.Original))
        { uniqueJobsCheck = "unique_jobs_binding_changed"; return SlotEditProgress.Failed; }
        var addon = Plugin.GameGui.GetAddonByName<AddonLookingForGroupCondition>("LookingForGroupCondition", 1);
        var agent = AgentLookingForGroup.Instance();
        if (!ValidateUniqueJobsGuard(addon, agent, session, false, out _, out error)) return SlotEditProgress.Failed;
        var action = session.Ui.Next(session.State, Environment.TickCount64);
        uniqueJobsCheck = session.Ui.Check;
        if (action == CheckboxUiAction.Wait) return SlotEditProgress.Waiting;
        if (action != CheckboxUiAction.Complete)
        { error = "잡 중복 없음 체크 상태와 저장값이 요청과 달라 중단했습니다. 진단 정보를 확인해 주세요."; return SlotEditProgress.Failed; }
        if (!buttonEnabled) { error = "게임의 갱신 버튼이 준비되기를 기다리고 있습니다."; return SlotEditProgress.Waiting; }
        if (!ValidateUniqueJobsGuard(addon, agent, session, true, out _, out error)) return SlotEditProgress.Failed;
        editingTarget = session.Expected;
        if (!ValidateEditor(session.Expected, out _, out _, out error)) return SlotEditProgress.Failed;
        session.Completed = true; expected = session.Expected;
        return SlotEditProgress.Complete;
    }

    private bool ValidateUniqueJobsGuard(AddonLookingForGroupCondition* addon, AgentLookingForGroup* agent,
        UniqueJobsSession session, bool final, out AtkEvent clickEvent, out string error)
    {
        clickEvent = default;
        error = "잡 중복 없음 설정 중 창 또는 다른 모집 조건이 바뀌어 중단했습니다.";
        ReadEditorAction();
        if (SelfId != session.Original.SelfId || BlockReason != null || EditorClosedRevision != editingFromClosedRevision ||
            !buttonTextReadable || buttonMode != RecruitmentEditorMode.Refresh || Visible("LookingForGroupSelectRole") ||
            addon != Plugin.GameGui.GetAddonByName<AddonLookingForGroupCondition>("LookingForGroupCondition", 1))
        { uniqueJobsCheck = "unique_jobs_session_changed"; return false; }
        if ((nint)addon != session.Addon || (nint)agent != session.Agent ||
            !NativeReadGuard.Readable(addon, (nuint)sizeof(AddonLookingForGroupCondition)) || !addon->IsVisible || !addon->IsReady ||
            !NativeReadGuard.Readable(agent, (nuint)sizeof(AgentLookingForGroup)))
        { uniqueJobsCheck = "unique_jobs_window_changed"; return false; }
        if ((nint)addon->OnePlayerPerJobCheckbox != session.Checkbox)
        { uniqueJobsCheck = "unique_jobs_component_changed"; return false; }
        if (UniqueJobsInvariant(agent) != session.Invariant)
        { uniqueJobsCheck = "other_criteria_changed"; return false; }
        if (!ReadUniqueJobsControl(addon, agent, out _, out var state, out clickEvent, out error)) return false;
        session.State = state;
        if ((nint)addon->OnePlayerPerJobCheckbox->OwnerNode != session.Node || addon->OnePlayerPerJobCheckbox->OwnerNode->NodeId != session.NodeId)
        { uniqueJobsCheck = "unique_jobs_node_changed"; return false; }
        if (final && !state.Matches(session.Edit.Enabled))
        { uniqueJobsCheck = "unique_jobs_final_ui_or_stored_mismatch"; error = "잡 중복 없음의 체크 상태와 저장값이 일치하지 않습니다."; return false; }
        error = "";
        return true;
    }

    private bool ReadUniqueJobsControl(AddonLookingForGroupCondition* addon, AgentLookingForGroup* agent,
        out AtkComponentCheckBox* checkbox, out CheckboxUiState state, out AtkEvent clickEvent, out string error)
    {
        checkbox = addon->OnePlayerPerJobCheckbox; state = default; clickEvent = default;
        error = "현재 게임 수정창에서 잡 중복 없음을 선택할 수 없습니다.";
        if (!NativeReadGuard.Readable(checkbox, (nuint)sizeof(AtkComponentCheckBox)) ||
            !NativeReadGuard.Readable(checkbox->OwnerNode, (nuint)sizeof(AtkComponentNode)) ||
            checkbox->OwnerNode->Component != (AtkComponentBase*)checkbox || !checkbox->OwnerNode->IsVisible() || !checkbox->IsEnabled)
        { uniqueJobsCheck = "unique_jobs_control_unavailable"; return false; }
        state = new(checkbox->IsChecked, agent->StoredRecruitmentInfo.OnePlayerPerJob);
        if (!ListingCheckboxValues.Decode(state.Stored).HasValue)
        { uniqueJobsCheck = "unique_jobs_stored_unknown"; return false; }
        if (!ReadRegisteredUiEvent((AtkResNode*)checkbox->OwnerNode, (AtkEventListener*)addon, AtkEventType.ButtonClick, 32, out clickEvent))
        { uniqueJobsCheck = "unique_jobs_event_route_mismatch"; error = "잡 중복 없음 버튼의 이벤트 연결이 수동 기록과 달라 중단했습니다."; return false; }
        error = "";
        return true;
    }

    private static string UniqueJobsInvariant(AgentLookingForGroup* agent) => DraftInvariant(agent, includeUniqueJobs: false) + "/" +
        Convert.ToHexString(MemoryMarshal.AsBytes(agent->StoredRecruitmentInfo.SlotFlags));
}
