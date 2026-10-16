using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace PartyViewKR;

internal sealed unsafe partial class ListingRefreshHost
{
    private ObjectiveSession? objectiveSession;
    private string objectiveCheck = "not_started";
    private string ObjectiveEditDiagnostic => $"objectiveEditCheck={objectiveCheck}; objectiveEditPhase={objectiveSession?.Ui.Phase ?? "none"}; " +
        $"objectiveDirectCallbackCalls=0; objectiveCheckCalls={objectiveSession?.CheckCalls ?? 0}; objectiveOpenCalls={objectiveSession?.OpenCalls ?? 0}; " +
        $"objectiveItemClickCalls={objectiveSession?.ClickCalls ?? 0}; objectiveSelectCalls={objectiveSession?.SelectCalls ?? 0}; " +
        $"objectiveRequestedIndex={objectiveSession?.Edit.SelectedIndex.ToString() ?? "unknown"}; objectiveRequestedValue={objectiveSession?.Edit.Value?.ToString() ?? "unknown"}; " +
        $"objectiveUiIndex={objectiveSession?.State.Index.ToString() ?? "unknown"}; objectiveListIndex={objectiveSession?.State.ListIndex.ToString() ?? "unknown"}; " +
        $"objectiveStoredValue={objectiveSession?.State.Stored.ToString() ?? "unknown"}; objectiveDropdownOpen={objectiveSession?.State.Open.ToString() ?? "unknown"}; " +
        $"objectiveDropdownPending={objectiveSession?.State.Pending.ToString() ?? "unknown"}; objectiveCheckboxChecked={objectiveSession?.State.Checked.ToString() ?? "unknown"}";

    private sealed class ObjectiveSession(RefreshTarget original, ListingObjectiveEdit edit, string invariant,
        AddonLookingForGroupCondition* addon, AgentLookingForGroup* agent, AtkComponentDropDownList* dropdown)
    {
        internal readonly RefreshTarget Original = original;
        internal readonly ListingObjectiveEdit Edit = edit;
        internal readonly RefreshTarget Expected = edit.Expected(original);
        internal readonly string Invariant = invariant;
        internal readonly ListingObjectiveUi Ui = new(ListingObjectives.Index(ListingObjectives.Read(original.Criteria)), edit.SelectedIndex);
        internal readonly nint Addon = (nint)addon, Agent = (nint)agent, Dropdown = (nint)dropdown,
            Checkbox = (nint)dropdown->Checkbox, List = (nint)dropdown->List;
        internal ObjectiveUiState State;
        internal int CheckCalls, OpenCalls, ClickCalls, SelectCalls;
        internal bool Completed;
    }

    public bool BeginObjectiveEdit(RefreshTarget target, ListingObjectiveEdit edit, out string error)
    {
        objectiveSession = null; objectiveCheck = "validating";
        error = "기존 모집 목적을 확인하지 못했습니다.";
        if (!edit.MatchesOriginal(target) || Visible("LookingForGroupSelectRole")) return false;
        if (!ValidateEditor(target, out var addon, out var agent, out error)) return false;
        if (!ReadObjectiveControl(addon, agent, edit.SelectedIndex, out var dropdown, out var state, out error)) return false;
        if (!ListingSlotRoute.TryCreate(target.Slots, 1, out var route) ||
            !route.MatchesEditorShape(agent->StoredRecruitmentInfo.NumberOfGroups, agent->StoredRecruitmentInfo.NumberOfSlotsInMainParty) ||
            !target.Slots.MatchesMembers(agent->StoredRecruitmentInfo.MemberContentIds))
        { objectiveCheck = "members_or_shape_changed"; error = "모집 수정창의 파티 구성이 달라 중단했습니다."; return false; }
        if (!ReadObjectiveRoutes(addon, dropdown, out var openEvent, out error)) return false;

        var session = objectiveSession = new(target, edit, ObjectiveInvariant(agent), addon, agent, dropdown) { State = state };
        var action = session.Ui.Next(state, Environment.TickCount64);
        objectiveCheck = session.Ui.Check;
        if (action != ObjectiveUiAction.Open)
        { error = "목적 드롭다운이 기존 값으로 닫힌 상태인지 확인하지 못했습니다."; return false; }

        // Manual trace: checkbox MouseClick checks the button BEFORE the
        // dropdown receives ButtonClick(2). Use the native button method to
        // establish that same UI state, then deliver its registered event.
        // This sets the UI button only, never the recruitment Objective field.
        session.CheckCalls++;
        dropdown->Checkbox->SetChecked(true);
        if (!ValidateObjectiveGuard(addon, agent, session, false, out error)) return false;
        if (!session.State.Checked || session.State.Open || session.State.Pending ||
            !session.State.Matches(ListingObjectives.Index(ListingObjectives.Read(target.Criteria))))
        { objectiveCheck = "checkbox_open_state_mismatch"; error = "목적 드롭다운 버튼의 열림 상태를 준비하지 못했습니다."; return false; }
        if (!ReadObjectiveRoutes(addon, dropdown, out openEvent, out error)) return false;
        session.OpenCalls++;
        openEvent.NextEvent = null;
        var data = new AtkEventData();
        ((AtkEventListener*)dropdown)->ReceiveEvent(AtkEventType.ButtonClick, (int)openEvent.Param, &openEvent, &data);
        objectiveCheck = "dropdown_open_requested";
        error = "";
        return true;
    }

    public SlotEditProgress UpdateObjectiveEdit(out RefreshTarget expected, out string error)
    {
        expected = null!; error = "게임 목적 선택을 확인하지 못했습니다.";
        var session = objectiveSession;
        if (session == null || session.Completed) return SlotEditProgress.Failed;
        // ItemClick and SelectItem occur within the SAME native mouse click in
        // the trace. Permit at most these two actions in one update, rechecking
        // bindings and every unrelated condition between them.
        for (var step = 0; step < 2; step++)
        {
            ReadEditorAction();
            if (SelfId != session.Original.SelfId || BlockReason != null ||
                !ReferenceEquals(editingTarget, session.Original) || EditorClosedRevision != editingFromClosedRevision ||
                !buttonTextReadable || buttonMode != RecruitmentEditorMode.Refresh || Visible("LookingForGroupSelectRole"))
            { objectiveCheck = "editor_binding_changed"; error = "모집 수정창의 상태가 바뀌어 중단했습니다."; return SlotEditProgress.Failed; }
            var addon = Plugin.GameGui.GetAddonByName<AddonLookingForGroupCondition>("LookingForGroupCondition", 1);
            var agent = AgentLookingForGroup.Instance();
            if (!ValidateObjectiveGuard(addon, agent, session, false, out error)) return SlotEditProgress.Failed;
            var dropdown = addon->ObjectiveDropDown;
            var action = session.Ui.Next(session.State, Environment.TickCount64);
            objectiveCheck = session.Ui.Check;
            switch (action)
            {
                case ObjectiveUiAction.Wait:
                    error = "목적 드롭다운의 선택 반영을 기다리고 있습니다.";
                    return SlotEditProgress.Waiting;
                case ObjectiveUiAction.ClickItem:
                    if (!dropdown->List->OwnerNode->IsVisible())
                    { objectiveCheck = "objective_list_hidden"; error = "목적 목록이 화면에 표시되지 않았습니다."; return SlotEditProgress.Failed; }
                    session.ClickCalls++;
                    // Native dispatch populates the list event payload and uses
                    // the registered ListItemClick(35), Param 1 -> dropdown.
                    dropdown->List->DispatchItemEvent(session.Edit.SelectedIndex, AtkEventType.ListItemClick);
                    continue;
                case ObjectiveUiAction.SelectItem:
                    session.SelectCalls++;
                    // Update the real list selection AND dispatch its highlight
                    // event. The game forwards it to the editor (Param 20) and
                    // constructs its own callback; PVKR does not send command 20.
                    dropdown->List->SelectItem(session.Edit.SelectedIndex, true);
                    error = "";
                    return SlotEditProgress.Waiting;
                case ObjectiveUiAction.Complete:
                    if (!buttonEnabled) { error = "게임의 갱신 버튼이 준비되기를 기다리고 있습니다."; return SlotEditProgress.Waiting; }
                    if (!ValidateObjectiveGuard(addon, agent, session, true, out error)) return SlotEditProgress.Failed;
                    editingTarget = session.Expected;
                    if (!ValidateEditor(session.Expected, out _, out _, out error)) return SlotEditProgress.Failed;
                    session.Completed = true; expected = session.Expected;
                    return SlotEditProgress.Complete;
                default:
                    error = "목적 선택의 화면 상태와 내부 값이 예상한 흐름과 달라 중단했습니다. 진단 정보를 확인해 주세요.";
                    return SlotEditProgress.Failed;
            }
        }
        return SlotEditProgress.Waiting;
    }

    private bool ValidateObjectiveGuard(AddonLookingForGroupCondition* addon, AgentLookingForGroup* agent,
        ObjectiveSession session, bool final, out string error)
    {
        error = "목적 수정 중 창 또는 다른 모집 조건이 바뀌어 중단했습니다.";
        if (SelfId != session.Original.SelfId || BlockReason != null || EditorClosedRevision != editingFromClosedRevision ||
            addon != Plugin.GameGui.GetAddonByName<AddonLookingForGroupCondition>("LookingForGroupCondition", 1))
        { objectiveCheck = "objective_session_changed"; return false; }
        if ((nint)addon != session.Addon || (nint)agent != session.Agent ||
            !NativeReadGuard.Readable(addon, (nuint)sizeof(AddonLookingForGroupCondition)) || !addon->IsVisible || !addon->IsReady ||
            !NativeReadGuard.Readable(agent, (nuint)sizeof(AgentLookingForGroup)))
        { objectiveCheck = "objective_window_changed"; return false; }
        var dropdown = addon->ObjectiveDropDown;
        if ((nint)dropdown != session.Dropdown || !NativeReadGuard.Readable(dropdown, (nuint)sizeof(AtkComponentDropDownList)) ||
            (nint)dropdown->Checkbox != session.Checkbox || (nint)dropdown->List != session.List)
        { objectiveCheck = "objective_component_changed"; return false; }
        if (ObjectiveInvariant(agent) != session.Invariant)
        { objectiveCheck = "other_criteria_changed"; return false; }
        if (!ReadObjectiveControl(addon, agent, session.Edit.SelectedIndex, out dropdown, out var state, out error)) return false;
        session.State = state;
        if (!ReadObjectiveRoutes(addon, dropdown, out _, out error)) return false;
        if (final && (!state.Matches(session.Edit.SelectedIndex) || !state.SettledClosed))
        { objectiveCheck = "objective_final_ui_or_stored_mismatch"; error = "목적 표시·저장값·목록 닫힘을 모두 확인하지 못했습니다."; return false; }
        error = "";
        return true;
    }

    private bool ReadObjectiveControl(AddonLookingForGroupCondition* addon, AgentLookingForGroup* agent, int selected,
        out AtkComponentDropDownList* dropdown, out ObjectiveUiState state, out string error)
    {
        dropdown = addon->ObjectiveDropDown; state = default;
        error = "현재 게임 수정창에서 목적을 선택할 수 없습니다.";
        if (!NativeReadGuard.Readable(dropdown, (nuint)sizeof(AtkComponentDropDownList)) ||
            !ComponentOwnsNode((AtkComponentBase*)dropdown, 16) || !dropdown->OwnerNode->IsVisible() ||
            !NativeReadGuard.Readable(dropdown->Checkbox, (nuint)sizeof(AtkComponentCheckBox)) ||
            !ComponentOwnsNode((AtkComponentBase*)dropdown->Checkbox, 2) || !dropdown->Checkbox->IsEnabled ||
            !NativeReadGuard.Readable(dropdown->List, (nuint)sizeof(AtkComponentList)) ||
            !ComponentOwnsNode((AtkComponentBase*)dropdown->List, 3) ||
            !NativeReadGuard.Readable(dropdown->List->VirtualTable, (nuint)sizeof(AtkComponentList.AtkComponentListVirtualTable)))
        { objectiveCheck = "objective_controls_unavailable"; return false; }
        if (selected is < 0 or > 3 || dropdown->List->ListLength != 4 || dropdown->List->GetItemDisabledState(selected))
        { objectiveCheck = "objective_item_unavailable"; return false; }
        state = new(dropdown->GetSelectedItemIndex(), dropdown->List->SelectedItemIndex, (byte)agent->StoredRecruitmentInfo.Objective,
            dropdown->IsOpen, dropdown->OpenStateChangePending, dropdown->Checkbox->IsChecked, dropdown->List->ListLength);
        error = "";
        return true;
    }

    private static bool ComponentOwnsNode(AtkComponentBase* component, uint id) =>
        NativeReadGuard.Readable(component->OwnerNode, (nuint)sizeof(AtkComponentNode)) &&
        component->OwnerNode->NodeId == id && component->OwnerNode->Component == component;

    private bool ReadObjectiveRoutes(AddonLookingForGroupCondition* addon, AtkComponentDropDownList* dropdown, out AtkEvent openEvent, out string error)
    {
        error = "목적 드롭다운의 이벤트 연결이 수동 기록과 달라 중단했습니다.";
        if (!ReadRegisteredUiEvent((AtkResNode*)dropdown->Checkbox->OwnerNode, (AtkEventListener*)dropdown, AtkEventType.ButtonClick, 2, out openEvent) ||
            !ReadRegisteredUiEvent((AtkResNode*)dropdown->List->OwnerNode, (AtkEventListener*)dropdown, AtkEventType.ListItemClick, 1, out _) ||
            !ReadRegisteredUiEvent((AtkResNode*)dropdown->List->OwnerNode, (AtkEventListener*)dropdown, AtkEventType.ListItemHighlight, 1, out _) ||
            !ReadRegisteredUiEvent((AtkResNode*)dropdown->OwnerNode, (AtkEventListener*)addon, AtkEventType.ListItemHighlight, 20, out _))
        { objectiveCheck = "objective_event_route_mismatch"; return false; }
        error = "";
        return true;
    }

    private static bool ReadRegisteredUiEvent(AtkResNode* node, AtkEventListener* listener, AtkEventType type, uint param, out AtkEvent value)
    {
        value = default; var matches = 0; var seen = new HashSet<nuint>();
        if (!NativeReadGuard.Readable(listener->VirtualTable, (nuint)sizeof(AtkEventListener.AtkEventListenerVirtualTable))) return false;
        for (var evt = node->AtkEventManager.Event; evt != null; evt = evt->NextEvent)
        {
            if (seen.Count >= 32 || !seen.Add((nuint)evt) || !NativeReadGuard.Readable(evt, (nuint)sizeof(AtkEvent))) return false;
            if (evt->State.EventType != type) continue;
            if (evt->Param != param || evt->Target != (AtkEventTarget*)node || evt->Listener != listener || ++matches != 1) return false;
            value = *evt;
        }
        return matches == 1;
    }

    private static string ObjectiveInvariant(AgentLookingForGroup* agent) => DraftInvariant(agent, includeObjective: false) + "/" +
        Convert.ToHexString(MemoryMarshal.AsBytes(agent->StoredRecruitmentInfo.SlotFlags));
}
