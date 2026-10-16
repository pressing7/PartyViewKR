using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace PartyViewKR;

internal sealed unsafe partial class ListingRefreshHost
{
    private ItemLevelSession? itemLevelSession;
    private string itemLevelCheck = "not_started", itemLevelControlRead = "not_started";
    private string ItemLevelEditDiagnostic => $"itemLevelEditCheck={itemLevelCheck}; itemLevelEditPhase={itemLevelSession?.Ui.Phase ?? "none"}; " +
        $"itemLevelDirectCallbackCalls=0; itemLevelCheckCalls={itemLevelSession?.CheckCalls ?? 0}; itemLevelClickCalls={itemLevelSession?.ClickCalls ?? 0}; " +
        $"itemLevelInputCalls={itemLevelSession?.InputCalls ?? 0}; itemLevelRequestedEnabled={itemLevelSession?.Edit.Enabled.ToString() ?? "unknown"}; " +
        $"itemLevelRequestedValue={itemLevelSession?.Edit.Value?.ToString() ?? "unknown"}; itemLevelUiChecked={itemLevelSession?.State.Checked.ToString() ?? "unknown"}; " +
        $"itemLevelStoredEnabled={itemLevelSession?.State.StoredEnabled?.ToString() ?? "unknown"}; itemLevelStoredEnabledRaw={itemLevelSession?.State.EnabledRaw.ToString() ?? "unknown"}; " +
        $"itemLevelUiValue={itemLevelSession?.State.NumericValue.ToString() ?? "unknown"}; itemLevelStoredValue={itemLevelSession?.State.StoredValue.ToString() ?? "unknown"}; " +
        $"itemLevelInputReady={itemLevelSession?.State.InputReady.ToString() ?? "unknown"}; itemLevelCheckboxNode={itemLevelSession?.CheckboxNodeId.ToString() ?? "unknown"}; " +
        $"itemLevelInputNode={itemLevelSession?.InputNodeId.ToString() ?? "unknown"}; itemLevelCheckboxParam=4; itemLevelInputParam=0\nitemLevelControlRead={itemLevelControlRead}";

    private sealed class ItemLevelSession(RefreshTarget original, ListingItemLevelEdit edit, string invariant,
        AddonLookingForGroupCondition* addon, AgentLookingForGroup* agent, AtkComponentCheckBox* checkbox, AtkComponentNumericInput* input)
    {
        internal readonly RefreshTarget Original = original, Expected = edit.Expected(original);
        internal readonly ListingItemLevelEdit Edit = edit;
        internal readonly string Invariant = invariant;
        internal readonly nint Addon = (nint)addon, Agent = (nint)agent, Checkbox = (nint)checkbox, Input = (nint)input,
            CheckboxNode = (nint)checkbox->OwnerNode, InputNode = (nint)input->OwnerNode;
        internal readonly uint CheckboxNodeId = checkbox->OwnerNode->NodeId, InputNodeId = input->OwnerNode->NodeId;
        internal readonly ListingItemLevelUi Ui = new(ListingItemLevels.Read(original.ExtraCriteria)!.Value, edit.Value!.Value);
        internal ItemLevelUiState State;
        internal int CheckCalls, ClickCalls, InputCalls;
        internal bool Completed;
    }

    public bool BeginItemLevelEdit(RefreshTarget target, ListingItemLevelEdit edit, out string error)
    {
        itemLevelSession = null; itemLevelCheck = "validating"; itemLevelControlRead = "not_read";
        error = "기존 평균 아이템 레벨 설정을 확인하지 못했습니다.";
        if (!edit.MatchesOriginal(target) || Visible("LookingForGroupSelectRole")) return false;
        if (!ValidateEditor(target, out var addon, out var agent, out error)) return false;
        if (!ReadItemLevelControls(addon, agent, out var checkbox, out var input, out var state, out _, out error)) return false;
        if (!ListingSlotRoute.TryCreate(target.Slots, 1, out var route) ||
            !route.MatchesEditorShape(agent->StoredRecruitmentInfo.NumberOfGroups, agent->StoredRecruitmentInfo.NumberOfSlotsInMainParty) ||
            !agent->StoredRecruitmentInfo.MemberContentIds[..target.TotalSlots].SequenceEqual(target.Slots.Slots.Select(s => s.MemberContentId).ToArray()))
        { itemLevelCheck = "members_or_shape_changed"; error = "모집 수정창의 파티 구성이 달라 중단했습니다."; return false; }
        var session = itemLevelSession = new(target, edit, ItemLevelInvariant(agent), addon, agent, checkbox, input) { State = state };
        return AdvanceItemLevelEdit(addon, agent, session, out _, out error);
    }

    public SlotEditProgress UpdateItemLevelEdit(out RefreshTarget expected, out string error)
    {
        expected = null!; error = "평균 아이템 레벨 설정을 확인하지 못했습니다.";
        var session = itemLevelSession;
        if (session == null || session.Completed) return SlotEditProgress.Failed;
        if (!ReferenceEquals(editingTarget, session.Original))
        { itemLevelCheck = "item_level_binding_changed"; return SlotEditProgress.Failed; }
        var addon = Plugin.GameGui.GetAddonByName<AddonLookingForGroupCondition>("LookingForGroupCondition", 1);
        var agent = AgentLookingForGroup.Instance();
        if (!ValidateItemLevelGuard(addon, agent, session, false, out _, out error) ||
            !AdvanceItemLevelEdit(addon, agent, session, out var complete, out error)) return SlotEditProgress.Failed;
        if (!complete || !buttonEnabled) return SlotEditProgress.Waiting;
        if (!ValidateItemLevelGuard(addon, agent, session, true, out _, out error)) return SlotEditProgress.Failed;
        editingTarget = session.Expected;
        if (!ValidateEditor(session.Expected, out _, out _, out error)) return SlotEditProgress.Failed;
        session.Completed = true; expected = session.Expected;
        return SlotEditProgress.Complete;
    }

    private bool AdvanceItemLevelEdit(AddonLookingForGroupCondition* addon, AgentLookingForGroup* agent,
        ItemLevelSession session, out bool complete, out string error)
    {
        complete = false; error = "평균 아이템 레벨의 체크·입력·저장값이 요청과 달라 중단했습니다.";
        var action = session.Ui.Next(session.State, Environment.TickCount64);
        itemLevelCheck = session.Ui.Check;
        if (action == ItemLevelUiAction.Failed) return false;
        if (action == ItemLevelUiAction.Complete) { complete = true; error = ""; return true; }
        if (action == ItemLevelUiAction.Wait) { error = ""; return true; }
        if (!ValidateItemLevelGuard(addon, agent, session, false, out var clickEvent, out error)) return false;
        if (action == ItemLevelUiAction.Toggle)
        {
            var before = session.State;
            session.CheckCalls++;
            addon->AvgItemLevelCheckbox->SetChecked(session.Edit.Enabled);
            if (!ValidateItemLevelGuard(addon, agent, session, false, out clickEvent, out error)) return false;
            if (session.State.Checked != session.Edit.Enabled || session.State.EnabledRaw != before.EnabledRaw ||
                session.State.StoredValue != before.StoredValue || session.State.NumericValue != before.NumericValue)
            { itemLevelCheck = "item_level_preclick_state_mismatch"; error = "평균 아이템 레벨 체크박스의 클릭 준비 상태가 예상과 달라 중단했습니다."; return false; }
            // KR manual trace: ButtonClick(25), Param 4 -> callback 11,
            // UInt current level, Bool enabled. The native editor constructs it.
            session.ClickCalls++;
            clickEvent.NextEvent = null;
            var data = new AtkEventData();
            addon->ReceiveEvent(AtkEventType.ButtonClick, (int)clickEvent.Param, &clickEvent, &data);
        }
        else if (action == ItemLevelUiAction.Input)
        {
            var input = addon->AvgItemLevelNumericInput;
            var value = session.Edit.Value!.Value;
            if (!session.State.Checked || session.State.StoredEnabled != true || !session.State.CanEnter(value))
            { itemLevelCheck = "item_level_input_or_range_unavailable"; error = "게임의 평균 아이템 레벨 입력창이 준비되지 않았거나 입력 범위를 벗어났습니다."; return false; }
            // Use the numeric control's own ValueUpdate(27)/0 event. The game
            // produces command 11, UInt level, Bool true; no direct callback
            // or write to AgentLookingForGroup.AvgItemLv/AvgItemLvEnabled.
            session.InputCalls++;
            input->InnerSetValue(value, triggerCallback: true, playSoundEffect: false);
        }
        error = ""; return true;
    }

    private bool ValidateItemLevelGuard(AddonLookingForGroupCondition* addon, AgentLookingForGroup* agent,
        ItemLevelSession session, bool final, out AtkEvent clickEvent, out string error)
    {
        clickEvent = default; error = "평균 아이템 레벨 수정 중 창 또는 다른 모집 조건이 바뀌어 중단했습니다.";
        ReadEditorAction();
        if (SelfId != session.Original.SelfId || BlockReason != null || EditorClosedRevision != editingFromClosedRevision ||
            !buttonTextReadable || buttonMode != RecruitmentEditorMode.Refresh || Visible("LookingForGroupSelectRole") ||
            addon != Plugin.GameGui.GetAddonByName<AddonLookingForGroupCondition>("LookingForGroupCondition", 1))
        { itemLevelCheck = "item_level_session_changed"; return false; }
        if ((nint)addon != session.Addon || (nint)agent != session.Agent ||
            !NativeReadGuard.Readable(addon, (nuint)sizeof(AddonLookingForGroupCondition)) || !addon->IsVisible || !addon->IsReady ||
            !NativeReadGuard.Readable(agent, (nuint)sizeof(AgentLookingForGroup)))
        { itemLevelCheck = "item_level_window_changed"; return false; }
        if ((nint)addon->AvgItemLevelCheckbox != session.Checkbox || (nint)addon->AvgItemLevelNumericInput != session.Input)
        { itemLevelCheck = "item_level_component_changed"; return false; }
        if (ItemLevelInvariant(agent) != session.Invariant)
        { itemLevelCheck = "other_criteria_changed"; return false; }
        if (!ReadItemLevelControls(addon, agent, out var checkbox, out var input, out var state, out clickEvent, out error)) return false;
        session.State = state;
        if ((nint)checkbox->OwnerNode != session.CheckboxNode || checkbox->OwnerNode->NodeId != session.CheckboxNodeId ||
            (nint)input->OwnerNode != session.InputNode || input->OwnerNode->NodeId != session.InputNodeId)
        { itemLevelCheck = "item_level_node_changed"; error = "평균 아이템 레벨 입력 컨트롤이 바뀌어 중단했습니다."; return false; }
        if (final && !state.Matches(session.Edit.Value!.Value))
        { itemLevelCheck = "item_level_final_ui_or_stored_mismatch"; error = "평균 아이템 레벨의 체크·입력·저장값 일치를 확인하지 못했습니다."; return false; }
        error = ""; return true;
    }

    private bool ReadItemLevelControls(AddonLookingForGroupCondition* addon, AgentLookingForGroup* agent,
        out AtkComponentCheckBox* checkbox, out AtkComponentNumericInput* input, out ItemLevelUiState state,
        out AtkEvent clickEvent, out string error)
    {
        checkbox = addon->AvgItemLevelCheckbox; input = addon->AvgItemLevelNumericInput;
        state = default; clickEvent = default; error = "현재 게임 수정창의 평균 아이템 레벨 입력 컨트롤을 확인하지 못했습니다.";
        if (!NativeReadGuard.Readable(checkbox, (nuint)sizeof(AtkComponentCheckBox)) || !OwnsComponent((AtkComponentBase*)checkbox) ||
            !checkbox->OwnerNode->IsVisible() || !checkbox->IsEnabled ||
            !NativeReadGuard.Readable(input, (nuint)sizeof(AtkComponentNumericInput)) || !OwnsComponent((AtkComponentBase*)input))
        { itemLevelCheck = "item_level_control_unavailable"; return false; }
        state = new(checkbox->IsChecked, agent->AvgItemLvEnabled, agent->AvgItemLv, input->Value,
            input->OwnerNode->IsVisible() && (input->OwnerNode->NodeFlags & NodeFlags.Enabled) != 0 && input->EnableTextInput,
            input->Data.Min, input->Data.Max);
        itemLevelControlRead = $"read; checkboxNode={checkbox->OwnerNode->NodeId}; inputNode={input->OwnerNode->NodeId}; " +
            $"inputVisible={input->OwnerNode->IsVisible()}; inputNodeEnabled={(input->OwnerNode->NodeFlags & NodeFlags.Enabled) != 0}; " +
            $"textInputEnabled={input->EnableTextInput}; minimum={input->Data.Min}; maximum={input->Data.Max}";
        if (!state.StoredEnabled.HasValue || !ListingItemLevels.Decode(state.StoredValue).HasValue)
        { itemLevelCheck = "item_level_stored_unknown"; return false; }
        if (!ReadRegisteredUiEvent((AtkResNode*)checkbox->OwnerNode, (AtkEventListener*)addon, AtkEventType.ButtonClick, 4, out clickEvent))
        { itemLevelCheck = "item_level_checkbox_route_mismatch"; return false; }
        if (!ReadRegisteredUiEvent((AtkResNode*)input->OwnerNode, (AtkEventListener*)addon, AtkEventType.ValueUpdate, 0, out _))
        { itemLevelCheck = "item_level_input_route_mismatch"; return false; }
        error = ""; return true;
    }

    private static string ItemLevelInvariant(AgentLookingForGroup* agent) => DraftInvariant(agent, includeItemLevel: false) + "/" +
        Convert.ToHexString(MemoryMarshal.AsBytes(agent->StoredRecruitmentInfo.SlotFlags));
}
