using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace PartyViewKR;

internal sealed unsafe partial class ListingRefreshHost
{
    private BeginnerSession? beginnerSession;
    private string beginnerCheck = "not_started";
    private string BeginnerEditDiagnostic => $"beginnerEditCheck={beginnerCheck}; beginnerEditPhase={beginnerSession?.Ui.Phase ?? "none"}; " +
        $"beginnerDirectCallbackCalls=0; beginnerCheckCalls={beginnerSession?.CheckCalls ?? 0}; beginnerClickCalls={beginnerSession?.ClickCalls ?? 0}; " +
        $"beginnerRequested={beginnerSession?.Edit.Enabled.ToString() ?? "unknown"}; beginnerUiChecked={beginnerSession?.State.Checked.ToString() ?? "unknown"}; " +
        $"beginnerStoredValue={beginnerSession?.State.Stored.ToString() ?? "unknown"}; beginnerNode=17; beginnerParam=21";

    private sealed class BeginnerSession(RefreshTarget original, ListingBeginnerEdit edit, string invariant,
        AddonLookingForGroupCondition* addon, AgentLookingForGroup* agent, AtkComponentCheckBox* checkbox)
    {
        internal readonly RefreshTarget Original = original;
        internal readonly ListingBeginnerEdit Edit = edit;
        internal readonly RefreshTarget Expected = edit.Expected(original);
        internal readonly string Invariant = invariant;
        internal readonly nint Addon = (nint)addon, Agent = (nint)agent, Checkbox = (nint)checkbox;
        internal readonly ListingCheckboxUi Ui = new(ListingBeginners.Read(original.Criteria)!.Value, edit.Enabled, "beginner");
        internal CheckboxUiState State;
        internal int CheckCalls, ClickCalls;
        internal bool Completed;
    }

    public bool BeginBeginnerEdit(RefreshTarget target, ListingBeginnerEdit edit, out string error)
    {
        beginnerSession = null; beginnerCheck = "validating";
        error = "기존 초행 및 초보자 환영 설정을 확인하지 못했습니다.";
        if (!edit.MatchesOriginal(target) || Visible("LookingForGroupSelectRole")) return false;
        if (!ValidateEditor(target, out var addon, out var agent, out error)) return false;
        if (!ReadBeginnerControl(addon, agent, out var checkbox, out var state, out _, out error)) return false;
        if (!ListingSlotRoute.TryCreate(target.Slots, 1, out var route) ||
            !route.MatchesEditorShape(agent->StoredRecruitmentInfo.NumberOfGroups, agent->StoredRecruitmentInfo.NumberOfSlotsInMainParty) ||
            !agent->StoredRecruitmentInfo.MemberContentIds[..target.TotalSlots].SequenceEqual(target.Slots.Slots.Select(s => s.MemberContentId).ToArray()))
        { beginnerCheck = "members_or_shape_changed"; error = "모집 수정창의 파티 구성이 달라 중단했습니다."; return false; }

        var session = beginnerSession = new(target, edit, BeginnerInvariant(agent), addon, agent, checkbox) { State = state };
        var action = session.Ui.Next(state, Environment.TickCount64);
        beginnerCheck = session.Ui.Check;
        if (action != CheckboxUiAction.Click)
        { error = "초보자 환영의 기존 체크 상태와 저장값이 일치하지 않아 중단했습니다."; return false; }

        // Follow the native checkbox UI path: set the control to the requested
        // check state, then deliver its registered ButtonClick(25), Param 21.
        // The KR manual trace reaches editor node 17; the game itself emits
        // callback 18 (on) or 19 (off). PVKR does not construct those callbacks
        // or write StoredRecruitmentInfo.BeginnerFriendly.
        session.CheckCalls++;
        checkbox->SetChecked(edit.Enabled);
        if (!ValidateBeginnerGuard(addon, agent, session, false, out var clickEvent, out error)) return false;
        if (session.State.Checked != edit.Enabled || ListingBeginners.Decode(session.State.Stored) != ListingBeginners.Read(target.Criteria))
        { beginnerCheck = "beginner_preclick_state_mismatch"; error = "체크박스의 클릭 준비 상태를 확인하지 못했습니다."; return false; }
        session.ClickCalls++;
        clickEvent.Node = (AtkResNode*)checkbox->OwnerNode;
        clickEvent.NextEvent = null;
        var data = new AtkEventData();
        addon->ReceiveEvent(AtkEventType.ButtonClick, (int)clickEvent.Param, &clickEvent, &data);
        beginnerCheck = "beginner_click_requested";
        error = "";
        return true;
    }

    public SlotEditProgress UpdateBeginnerEdit(out RefreshTarget expected, out string error)
    {
        expected = null!; error = "초행 및 초보자 환영 설정을 확인하지 못했습니다.";
        var session = beginnerSession;
        if (session == null || session.Completed) return SlotEditProgress.Failed;
        if (!ReferenceEquals(editingTarget, session.Original))
        { beginnerCheck = "beginner_binding_changed"; return SlotEditProgress.Failed; }
        var addon = Plugin.GameGui.GetAddonByName<AddonLookingForGroupCondition>("LookingForGroupCondition", 1);
        var agent = AgentLookingForGroup.Instance();
        if (!ValidateBeginnerGuard(addon, agent, session, false, out _, out error)) return SlotEditProgress.Failed;
        var action = session.Ui.Next(session.State, Environment.TickCount64);
        beginnerCheck = session.Ui.Check;
        if (action == CheckboxUiAction.Wait) return SlotEditProgress.Waiting;
        if (action != CheckboxUiAction.Complete)
        { error = "초보자 환영 체크 상태와 저장값이 요청과 달라 중단했습니다. 진단 정보를 확인해 주세요."; return SlotEditProgress.Failed; }
        if (!buttonEnabled) { error = "게임의 갱신 버튼이 준비되기를 기다리고 있습니다."; return SlotEditProgress.Waiting; }
        if (!ValidateBeginnerGuard(addon, agent, session, true, out _, out error)) return SlotEditProgress.Failed;
        editingTarget = session.Expected;
        if (!ValidateEditor(session.Expected, out _, out _, out error)) return SlotEditProgress.Failed;
        session.Completed = true; expected = session.Expected;
        return SlotEditProgress.Complete;
    }

    private bool ValidateBeginnerGuard(AddonLookingForGroupCondition* addon, AgentLookingForGroup* agent,
        BeginnerSession session, bool final, out AtkEvent clickEvent, out string error)
    {
        clickEvent = default;
        error = "초보자 환영 설정 중 창 또는 다른 모집 조건이 바뀌어 중단했습니다.";
        ReadEditorAction();
        if (SelfId != session.Original.SelfId || BlockReason != null || EditorClosedRevision != editingFromClosedRevision ||
            !buttonTextReadable || buttonMode != RecruitmentEditorMode.Refresh || Visible("LookingForGroupSelectRole") ||
            addon != Plugin.GameGui.GetAddonByName<AddonLookingForGroupCondition>("LookingForGroupCondition", 1))
        { beginnerCheck = "beginner_session_changed"; return false; }
        if ((nint)addon != session.Addon || (nint)agent != session.Agent ||
            !NativeReadGuard.Readable(addon, (nuint)sizeof(AddonLookingForGroupCondition)) || !addon->IsVisible || !addon->IsReady ||
            !NativeReadGuard.Readable(agent, (nuint)sizeof(AgentLookingForGroup)))
        { beginnerCheck = "beginner_window_changed"; return false; }
        if ((nint)addon->BeginnersWelcomeCheckBox != session.Checkbox)
        { beginnerCheck = "beginner_component_changed"; return false; }
        if (BeginnerInvariant(agent) != session.Invariant)
        { beginnerCheck = "other_criteria_changed"; return false; }
        if (!ReadBeginnerControl(addon, agent, out _, out var state, out clickEvent, out error)) return false;
        session.State = state;
        if (final && !state.Matches(session.Edit.Enabled))
        { beginnerCheck = "beginner_final_ui_or_stored_mismatch"; error = "초보자 환영의 체크 상태와 저장값이 일치하지 않습니다."; return false; }
        error = "";
        return true;
    }

    private bool ReadBeginnerControl(AddonLookingForGroupCondition* addon, AgentLookingForGroup* agent,
        out AtkComponentCheckBox* checkbox, out CheckboxUiState state, out AtkEvent clickEvent, out string error)
    {
        checkbox = addon->BeginnersWelcomeCheckBox; state = default; clickEvent = default;
        error = "현재 게임 수정창에서 초행 및 초보자 환영을 선택할 수 없습니다.";
        if (!NativeReadGuard.Readable(checkbox, (nuint)sizeof(AtkComponentCheckBox)) ||
            !ComponentOwnsNode((AtkComponentBase*)checkbox, 17) || !checkbox->OwnerNode->IsVisible() || !checkbox->IsEnabled)
        { beginnerCheck = "beginner_control_unavailable"; return false; }
        state = new(checkbox->IsChecked, agent->StoredRecruitmentInfo.BeginnerFriendly);
        if (!ListingBeginners.Decode(state.Stored).HasValue)
        { beginnerCheck = "beginner_stored_unknown"; return false; }
        if (!ReadRegisteredUiEvent((AtkResNode*)checkbox->OwnerNode, (AtkEventListener*)addon, AtkEventType.ButtonClick, 21, out clickEvent))
        { beginnerCheck = "beginner_event_route_mismatch"; error = "초보자 환영 버튼의 이벤트 연결이 수동 기록과 달라 중단했습니다."; return false; }
        error = "";
        return true;
    }

    private static string BeginnerInvariant(AgentLookingForGroup* agent) => DraftInvariant(agent, includeBeginner: false) + "/" +
        Convert.ToHexString(MemoryMarshal.AsBytes(agent->StoredRecruitmentInfo.SlotFlags));
}
