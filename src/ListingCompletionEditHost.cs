using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace PartyViewKR;

internal sealed unsafe partial class ListingRefreshHost
{
    private CompletionSession? completionSession;
    private string completionCheck = "not_started", completionControls = "not_started";
    private string CompletionEditDiagnostic => $"completionEditCheck={completionCheck}; completionEditPhase={completionSession?.Ui.Phase ?? "none"}; " +
        $"completionDirectCallbackCalls=0; completionToggleCalls={completionSession?.ToggleCalls ?? 0}; completionOpenCalls={completionSession?.OpenCalls ?? 0}; " +
        $"completionItemClickCalls={completionSession?.ClickCalls ?? 0}; completionSelectCalls={completionSession?.SelectCalls ?? 0}; " +
        $"completionRequestedEnabled={completionSession?.Edit.Enabled.ToString() ?? "unknown"}; completionRequestedValue={completionSession?.Edit.Value?.ToString() ?? "unknown"}; " +
        $"completionUiChecked={completionSession?.State.Enabled.ToString() ?? "unknown"}; completionUiIndex={completionSession?.State.Index.ToString() ?? "unknown"}; " +
        $"completionListIndex={completionSession?.State.ListIndex.ToString() ?? "unknown"}; completionStoredValue={completionSession?.State.Stored.ToString() ?? "unknown"}; " +
        $"completionDropdownOpen={completionSession?.State.Open.ToString() ?? "unknown"}; completionDropdownPending={completionSession?.State.Pending.ToString() ?? "unknown"}; " +
        $"completionDropdownChecked={completionSession?.State.DropdownChecked.ToString() ?? "unknown"}; completionCanSelect={completionSession?.State.CanSelect.ToString() ?? "unknown"}; " +
        $"completionCheckboxParam=23; completionDropdownParam=22\ncompletionControls={completionControls}";

    private sealed class CompletionSession(RefreshTarget original, ListingCompletionEdit edit, string invariant,
        AddonLookingForGroupCondition* addon, AgentLookingForGroup* agent)
    {
        internal readonly RefreshTarget Original = original, Expected = edit.Expected(original);
        internal readonly ListingCompletionEdit Edit = edit;
        internal readonly string Invariant = invariant;
        internal readonly ListingCompletionUi Ui = new(ListingCompletions.Read(original.Criteria)!.Value, edit.Value!.Value);
        internal readonly nint Addon = (nint)addon, Agent = (nint)agent;
        internal readonly nint[] Components = [(nint)addon->CompletionStatusCheckBox, (nint)addon->CompletionStatusDropDown,
            (nint)addon->CompletionStatusDropDown->Checkbox, (nint)addon->CompletionStatusDropDown->List];
        internal readonly nint[] Nodes = [(nint)addon->CompletionStatusCheckBox->OwnerNode, (nint)addon->CompletionStatusDropDown->OwnerNode,
            (nint)addon->CompletionStatusDropDown->Checkbox->OwnerNode, (nint)addon->CompletionStatusDropDown->List->OwnerNode];
        internal readonly uint[] NodeIds = [addon->CompletionStatusCheckBox->OwnerNode->NodeId, addon->CompletionStatusDropDown->OwnerNode->NodeId,
            addon->CompletionStatusDropDown->Checkbox->OwnerNode->NodeId, addon->CompletionStatusDropDown->List->OwnerNode->NodeId];
        internal CompletionUiState State;
        internal int ToggleCalls, OpenCalls, ClickCalls, SelectCalls;
        internal bool Completed;
    }

    public bool BeginCompletionEdit(RefreshTarget target, ListingCompletionEdit edit, out string error)
    {
        completionSession = null; completionCheck = "validating"; completionControls = "not_read";
        error = "기존 완료조건을 확인하지 못했습니다.";
        if (!edit.MatchesOriginal(target) || Visible("LookingForGroupSelectRole")) return false;
        if (!ValidateEditor(target, out var addon, out var agent, out error)) return false;
        if (!ReadCompletionControls(addon, agent, edit, out var state, out error)) return false;
        if (!ListingSlotRoute.TryCreate(target.Slots, 1, out var route) ||
            !route.MatchesEditorShape(agent->StoredRecruitmentInfo.NumberOfGroups, agent->StoredRecruitmentInfo.NumberOfSlotsInMainParty) ||
            !target.Slots.MatchesMembers(agent->StoredRecruitmentInfo.MemberContentIds))
        { completionCheck = "members_or_shape_changed"; error = "모집 수정창의 파티 구성이 달라 중단했습니다."; return false; }
        if (!ReadCompletionRoutes(addon, out _, out _, out error)) return false;
        var session = completionSession = new(target, edit, CompletionInvariant(agent), addon, agent) { State = state };
        var action = session.Ui.Next(state, Environment.TickCount64); completionCheck = session.Ui.Check;
        if (action is not (CompletionUiAction.Toggle or CompletionUiAction.Open))
        { error = "완료조건의 기존 체크 상태와 선택값이 일치하지 않거나 선택할 수 없는 항목입니다."; return false; }
        return DispatchCompletion(addon, agent, session, action, out error);
    }

    public SlotEditProgress UpdateCompletionEdit(out RefreshTarget expected, out string error)
    {
        expected = null!; error = "게임 완료조건 선택을 확인하지 못했습니다.";
        var session = completionSession;
        if (session == null || session.Completed || !ReferenceEquals(editingTarget, session.Original)) return SlotEditProgress.Failed;
        // As with the proven objective dropdown route, item click and list
        // selection belong to one native click. At most those two actions run
        // together, with binding and unrelated-criteria checks between them.
        for (var step = 0; step < 2; step++)
        {
            var addon = Plugin.GameGui.GetAddonByName<AddonLookingForGroupCondition>("LookingForGroupCondition", 1);
            var agent = AgentLookingForGroup.Instance();
            if (!ValidateCompletionGuard(addon, agent, session, false, out error)) return SlotEditProgress.Failed;
            var action = session.Ui.Next(session.State, Environment.TickCount64); completionCheck = session.Ui.Check;
            if (action == CompletionUiAction.Wait) return SlotEditProgress.Waiting;
            if (action == CompletionUiAction.Failed)
            { error = "완료조건의 체크·선택·저장값이 예상과 달라 중단했습니다. 진단 정보를 확인해 주세요."; return SlotEditProgress.Failed; }
            if (action == CompletionUiAction.Complete)
            {
                if (!buttonEnabled) return SlotEditProgress.Waiting;
                if (!ValidateCompletionGuard(addon, agent, session, true, out error)) return SlotEditProgress.Failed;
                editingTarget = session.Expected;
                if (!ValidateEditor(session.Expected, out _, out _, out error)) return SlotEditProgress.Failed;
                session.Completed = true; expected = session.Expected;
                return SlotEditProgress.Complete;
            }
            if (!DispatchCompletion(addon, agent, session, action, out error)) return SlotEditProgress.Failed;
            if (action != CompletionUiAction.ClickItem) return SlotEditProgress.Waiting;
        }
        return SlotEditProgress.Waiting;
    }

    private bool DispatchCompletion(AddonLookingForGroupCondition* addon, AgentLookingForGroup* agent,
        CompletionSession session, CompletionUiAction action, out string error)
    {
        if (!ValidateCompletionGuard(addon, agent, session, false, out error) ||
            !ReadCompletionRoutes(addon, out var toggleEvent, out var openEvent, out error)) return false;
        var dropdown = addon->CompletionStatusDropDown;
        var data = new AtkEventData();
        switch (action)
        {
            case CompletionUiAction.Toggle:
                var before = session.State;
                session.ToggleCalls++;
                addon->CompletionStatusCheckBox->SetChecked(session.Edit.Enabled);
                if (!ValidateCompletionGuard(addon, agent, session, false, out error)) return false;
                if (session.State.Enabled != session.Edit.Enabled || session.State.Stored != before.Stored || !session.State.Closed)
                { completionCheck = "completion_preclick_state_mismatch"; error = "완료조건 체크박스의 클릭 준비 상태를 확인하지 못했습니다."; return false; }
                if (!ReadCompletionRoutes(addon, out toggleEvent, out _, out error)) return false;
                toggleEvent.NextEvent = null;
                // User trace confirms ButtonClick(25)/23. A nested receive in
                // the manual log is native forwarding, not a second click.
                addon->ReceiveEvent(AtkEventType.ButtonClick, (int)toggleEvent.Param, &toggleEvent, &data);
                break;
            case CompletionUiAction.Open:
                var old = session.State;
                if (!old.CanSelect || !old.Closed)
                { completionCheck = "completion_dropdown_unavailable"; error = "완료조건 목록을 열 수 없는 상태입니다."; return false; }
                session.OpenCalls++;
                dropdown->Checkbox->SetChecked(true);
                if (!ValidateCompletionGuard(addon, agent, session, false, out error)) return false;
                if (!session.State.DropdownChecked || session.State.Open || session.State.Pending || !session.State.Matches(old.Stored))
                { completionCheck = "completion_open_state_mismatch"; error = "완료조건 목록의 열림 상태를 준비하지 못했습니다."; return false; }
                if (!ReadCompletionRoutes(addon, out _, out openEvent, out error)) return false;
                openEvent.NextEvent = null;
                ((AtkEventListener*)dropdown)->ReceiveEvent(AtkEventType.ButtonClick, (int)openEvent.Param, &openEvent, &data);
                break;
            case CompletionUiAction.ClickItem:
                if (!session.State.CanSelect || !dropdown->List->OwnerNode->IsVisible())
                { completionCheck = "completion_list_or_item_unavailable"; error = "완료조건 목록 또는 선택 항목을 사용할 수 없습니다."; return false; }
                session.ClickCalls++;
                dropdown->List->DispatchItemEvent(session.Edit.SelectedIndex, AtkEventType.ListItemClick);
                break;
            case CompletionUiAction.SelectItem:
                if (!session.State.CanSelect)
                { completionCheck = "completion_item_disabled"; error = "현재 게임 수정창에서 해당 완료조건을 선택할 수 없습니다."; return false; }
                session.SelectCalls++;
                // The game constructs the observed ListItemHighlight(37)/22
                // and command20/2-or-3/2; PVKR does not create that callback.
                dropdown->List->SelectItem(session.Edit.SelectedIndex, true);
                break;
            default:
                completionCheck = "completion_dispatch_unexpected"; error = "완료조건 수정 단계가 예상과 달라 중단했습니다."; return false;
        }
        error = ""; return true;
    }

    private bool ValidateCompletionGuard(AddonLookingForGroupCondition* addon, AgentLookingForGroup* agent,
        CompletionSession session, bool final, out string error)
    {
        error = "완료조건 수정 중 창 또는 다른 모집 조건이 바뀌어 중단했습니다.";
        ReadEditorAction();
        if (SelfId != session.Original.SelfId || BlockReason != null || EditorClosedRevision != editingFromClosedRevision ||
            !buttonTextReadable || buttonMode != RecruitmentEditorMode.Refresh || Visible("LookingForGroupSelectRole") ||
            addon != Plugin.GameGui.GetAddonByName<AddonLookingForGroupCondition>("LookingForGroupCondition", 1))
        { completionCheck = "completion_session_changed"; return false; }
        if ((nint)addon != session.Addon || (nint)agent != session.Agent ||
            !NativeReadGuard.Readable(addon, (nuint)sizeof(AddonLookingForGroupCondition)) || !addon->IsVisible || !addon->IsReady ||
            !NativeReadGuard.Readable(agent, (nuint)sizeof(AgentLookingForGroup)))
        { completionCheck = "completion_window_changed"; return false; }
        if (CompletionInvariant(agent) != session.Invariant) { completionCheck = "other_criteria_changed"; return false; }
        if (!ReadCompletionControls(addon, agent, session.Edit, out var state, out error)) return false;
        nint[] components = [(nint)addon->CompletionStatusCheckBox, (nint)addon->CompletionStatusDropDown,
            (nint)addon->CompletionStatusDropDown->Checkbox, (nint)addon->CompletionStatusDropDown->List];
        for (var i = 0; i < components.Length; i++)
        {
            var component = (AtkComponentBase*)components[i];
            if (components[i] != session.Components[i] || (nint)component->OwnerNode != session.Nodes[i] || component->OwnerNode->NodeId != session.NodeIds[i])
            { completionCheck = "completion_component_changed"; error = "완료조건 입력 컨트롤이 바뀌어 중단했습니다."; return false; }
        }
        session.State = state;
        if (!ReadCompletionRoutes(addon, out _, out _, out error)) return false;
        if (final && (!state.Matches(session.Edit.Value!.Value) || !state.Closed || session.Edit.Enabled && !state.CanSelect))
        { completionCheck = "completion_final_ui_or_stored_mismatch"; error = "완료조건 표시와 저장값의 일치를 확인하지 못했습니다."; return false; }
        error = ""; return true;
    }

    private bool ReadCompletionControls(AddonLookingForGroupCondition* addon, AgentLookingForGroup* agent,
        ListingCompletionEdit edit, out CompletionUiState state, out string error)
    {
        state = default; error = "현재 게임 수정창에서 완료조건을 선택할 수 없습니다.";
        var checkbox = addon->CompletionStatusCheckBox; var dropdown = addon->CompletionStatusDropDown;
        if (!NativeReadGuard.Readable(checkbox, (nuint)sizeof(AtkComponentCheckBox)) || !OwnsComponent((AtkComponentBase*)checkbox) ||
            !checkbox->OwnerNode->IsVisible() || !checkbox->IsEnabled ||
            !NativeReadGuard.Readable(dropdown, (nuint)sizeof(AtkComponentDropDownList)) || !OwnsComponent((AtkComponentBase*)dropdown) ||
            !NativeReadGuard.Readable(dropdown->Checkbox, (nuint)sizeof(AtkComponentCheckBox)) || !OwnsComponent((AtkComponentBase*)dropdown->Checkbox) ||
            !NativeReadGuard.Readable(dropdown->List, (nuint)sizeof(AtkComponentList)) || !OwnsComponent((AtkComponentBase*)dropdown->List) ||
            !NativeReadGuard.Readable(dropdown->List->VirtualTable, (nuint)sizeof(AtkComponentList.AtkComponentListVirtualTable)))
        { completionCheck = "completion_controls_unavailable"; return false; }
        var count = dropdown->List->ListLength;
        var canSelect = edit.Enabled && edit.SelectedIndex is 0 or 1 && count is >= 2 and <= 3 &&
            dropdown->OwnerNode->IsVisible() && dropdown->Checkbox->IsEnabled && !dropdown->List->GetItemDisabledState(edit.SelectedIndex);
        state = new(checkbox->IsChecked, dropdown->GetSelectedItemIndex(), dropdown->List->SelectedItemIndex,
            (byte)agent->StoredRecruitmentInfo.CompletionStatus, dropdown->IsOpen, dropdown->OpenStateChangePending,
            dropdown->Checkbox->IsChecked, count, canSelect);
        completionControls = $"read; checkboxNode={checkbox->OwnerNode->NodeId}; dropdownNode={dropdown->OwnerNode->NodeId}; " +
            $"openNode={dropdown->Checkbox->OwnerNode->NodeId}; listNode={dropdown->List->OwnerNode->NodeId}; itemCount={count}; " +
            $"dropdownEnabled={dropdown->Checkbox->IsEnabled}";
        error = ""; return true;
    }

    private static bool OwnsComponent(AtkComponentBase* component) =>
        NativeReadGuard.Readable(component->OwnerNode, (nuint)sizeof(AtkComponentNode)) && component->OwnerNode->Component == component;

    private bool ReadCompletionRoutes(AddonLookingForGroupCondition* addon, out AtkEvent toggleEvent, out AtkEvent openEvent, out string error)
    {
        openEvent = default; error = "완료조건 이벤트 연결이 확인된 경로와 달라 중단했습니다.";
        var dropdown = addon->CompletionStatusDropDown;
        if (!ReadRegisteredUiEvent((AtkResNode*)addon->CompletionStatusCheckBox->OwnerNode, (AtkEventListener*)addon, AtkEventType.ButtonClick, 23, out toggleEvent) ||
            !ReadRegisteredUiEvent((AtkResNode*)dropdown->Checkbox->OwnerNode, (AtkEventListener*)dropdown, AtkEventType.ButtonClick, 2, out openEvent) ||
            !ReadRegisteredUiEvent((AtkResNode*)dropdown->List->OwnerNode, (AtkEventListener*)dropdown, AtkEventType.ListItemClick, 1, out _) ||
            !ReadRegisteredUiEvent((AtkResNode*)dropdown->List->OwnerNode, (AtkEventListener*)dropdown, AtkEventType.ListItemHighlight, 1, out _) ||
            !ReadRegisteredUiEvent((AtkResNode*)dropdown->OwnerNode, (AtkEventListener*)addon, AtkEventType.ListItemHighlight, 22, out _))
        { completionCheck = "completion_event_route_mismatch"; return false; }
        error = ""; return true;
    }

    private static string CompletionInvariant(AgentLookingForGroup* agent) => DraftInvariant(agent, includeCompletion: false) + "/" +
        Convert.ToHexString(MemoryMarshal.AsBytes(agent->StoredRecruitmentInfo.SlotFlags));
}
