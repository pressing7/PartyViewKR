using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace PartyViewKR;

internal sealed unsafe partial class ListingRefreshHost
{
    private RuleSession? ruleSession;
    private string ruleCheck = "not_started", ruleControlRead = "not_started";
    private string RuleEditDiagnostic => $"ruleEditCheck={ruleCheck}; ruleKind={ruleSession?.Edit.Kind.ToString() ?? "unknown"}; " +
        $"ruleEditPhase={ruleSession?.ToggleUi?.Phase ?? ruleSession?.LootUi?.Phase ?? "none"}; ruleDirectCallbackCalls=0; " +
        $"ruleCheckCalls={ruleSession?.CheckCalls ?? 0}; ruleClickCalls={ruleSession?.ClickCalls ?? 0}; ruleOpenCalls={ruleSession?.OpenCalls ?? 0}; " +
        $"ruleItemClickCalls={ruleSession?.ItemClickCalls ?? 0}; ruleSelectCalls={ruleSession?.SelectCalls ?? 0}; " +
        $"ruleRequestedValue={ruleSession?.Edit.Value.ToString() ?? "unknown"}; ruleDutyFlags={ruleSession?.State.Flags.ToString() ?? "unknown"}; " +
        $"ruleUiChecked={ruleSession?.State.Toggle.Checked.ToString() ?? "unknown"}; ruleStoredBit={ruleSession?.State.Toggle.Stored.ToString() ?? "unknown"}; " +
        $"ruleLootUiIndex={ruleSession?.State.Loot.Index.ToString() ?? "unknown"}; ruleLootListIndex={ruleSession?.State.Loot.ListIndex.ToString() ?? "unknown"}; " +
        $"ruleLootStored={ruleSession?.State.Loot.Stored.ToString() ?? "unknown"}; ruleDropdownOpen={ruleSession?.State.Loot.Open.ToString() ?? "unknown"}; " +
        $"ruleDropdownPending={ruleSession?.State.Loot.Pending.ToString() ?? "unknown"}; ruleDropdownChecked={ruleSession?.State.Loot.Checked.ToString() ?? "unknown"}\n" +
        $"ruleControlRead={ruleControlRead}";

    private readonly record struct RuleControlState(nint Control, nint Node, uint NodeId, nint OpenButton, nint OpenNode,
        nint List, nint ListNode, byte Flags, CheckboxUiState Toggle, LootUiState Loot)
    {
        internal bool SameControls(RuleControlState other) => Control == other.Control && Node == other.Node && NodeId == other.NodeId &&
            OpenButton == other.OpenButton && OpenNode == other.OpenNode && List == other.List && ListNode == other.ListNode;
    }

    private sealed class RuleSession(RefreshTarget original, ListingRuleEdit edit, string invariant,
        AddonLookingForGroupCondition* addon, AgentLookingForGroup* agent, RuleControlState state)
    {
        internal readonly RefreshTarget Original = original, Expected = edit.Expected(original);
        internal readonly ListingRuleEdit Edit = edit;
        internal readonly string Invariant = invariant;
        internal readonly nint Addon = (nint)addon, Agent = (nint)agent;
        internal readonly RuleControlState Binding = state;
        internal RuleControlState State = state;
        internal readonly ListingCheckboxUi? ToggleUi = edit.Kind == ListingRuleKind.Loot ? null :
            new(ListingRules.Read(original.Criteria, edit.Kind) == 1, edit.Value == 1, ListingRules.Id(edit.Kind));
        internal readonly ListingLootUi? LootUi = edit.Kind == ListingRuleKind.Loot ?
            new(ListingRules.Read(original.Criteria, edit.Kind)!.Value, edit.Value) : null;
        internal int CheckCalls, ClickCalls, OpenCalls, ItemClickCalls, SelectCalls;
        internal bool Completed;
    }

    public bool BeginRuleEdit(RefreshTarget target, ListingRuleEdit edit, out string error)
    {
        ruleSession = null; ruleCheck = "validating"; ruleControlRead = "not_started";
        error = "기존 모집 규칙을 확인하지 못했습니다.";
        if (!edit.MatchesOriginal(target) || Visible("LookingForGroupSelectRole")) return false;
        if (!ValidateEditor(target, out var addon, out var agent, out error)) return false;
        if (!ReadRuleControl(addon, agent, edit, out var state, out _, out error)) return false;
        if (!ListingSlotRoute.TryCreate(target.Slots, 1, out var route) ||
            !route.MatchesEditorShape(agent->StoredRecruitmentInfo.NumberOfGroups, agent->StoredRecruitmentInfo.NumberOfSlotsInMainParty) ||
            !agent->StoredRecruitmentInfo.MemberContentIds[..target.TotalSlots].SequenceEqual(target.Slots.Slots.Select(s => s.MemberContentId).ToArray()))
        { ruleCheck = "members_or_shape_changed"; error = "모집 수정창의 파티 구성이 달라 중단했습니다."; return false; }

        var session = ruleSession = new(target, edit, RuleInvariant(agent, edit.Kind), addon, agent, state);
        var loot = edit.Kind == ListingRuleKind.Loot;
        var ready = loot ? session.LootUi!.Next(state.Loot, Environment.TickCount64) == LootUiAction.Open :
            session.ToggleUi!.Next(state.Toggle, Environment.TickCount64) == CheckboxUiAction.Click;
        ruleCheck = session.ToggleUi?.Check ?? session.LootUi!.Check;
        if (!ready) { error = "모집 규칙의 기존 화면 상태와 저장값이 일치하지 않습니다."; return false; }

        session.CheckCalls++;
        var checkbox = (AtkComponentCheckBox*)(loot ? state.OpenButton : state.Control);
        checkbox->SetChecked(loot || edit.Value == 1);
        if (!ValidateRuleGuard(addon, agent, session, false, out var clickEvent, out error)) return false;
        state = session.State;
        if (loot ? (!state.Loot.Checked || state.Loot.Open || state.Loot.Pending ||
                    !state.Loot.Matches(ListingRules.Read(target.Criteria, edit.Kind)!.Value)) :
            state.Toggle.Checked != (edit.Value == 1) || state.Toggle.Stored != ListingRules.Read(target.Criteria, edit.Kind))
        { ruleCheck = "rule_preclick_state_mismatch"; error = "모집 규칙 버튼의 클릭 준비 상태를 확인하지 못했습니다."; return false; }

        clickEvent.NextEvent = null;
        var data = new AtkEventData();
        if (loot)
        {
            session.OpenCalls++;
            ((AtkEventListener*)state.Control)->ReceiveEvent(AtkEventType.ButtonClick, (int)clickEvent.Param, &clickEvent, &data);
            ruleCheck = "loot_open_requested";
        }
        else
        {
            // All three checkboxes use Param 25. The manual KR events carry
            // the clicked owner node (99/100/101), which selects the flag.
            // Populate that observed Node payload on the verified event route.
            // The game emits callback 18/19; PVKR does not construct it.
            session.ClickCalls++;
            clickEvent.Node = (AtkResNode*)state.Node;
            addon->ReceiveEvent(AtkEventType.ButtonClick, (int)clickEvent.Param, &clickEvent, &data);
            ruleCheck = "rule_click_requested";
        }
        error = "";
        return true;
    }

    public SlotEditProgress UpdateRuleEdit(out RefreshTarget expected, out string error)
    {
        expected = null!; error = "모집 규칙의 반영 상태를 확인하지 못했습니다.";
        var session = ruleSession;
        if (session == null || session.Completed) return SlotEditProgress.Failed;
        if (!ReferenceEquals(editingTarget, session.Original))
        { ruleCheck = "rule_binding_changed"; return SlotEditProgress.Failed; }
        // A native dropdown mouse click closes the list and selects its item
        // in the same update. Recheck identities/other conditions between them.
        for (var step = 0; step < 2; step++)
        {
            var addon = Plugin.GameGui.GetAddonByName<AddonLookingForGroupCondition>("LookingForGroupCondition", 1);
            var agent = AgentLookingForGroup.Instance();
            if (!ValidateRuleGuard(addon, agent, session, false, out _, out error)) return SlotEditProgress.Failed;
            if (session.ToggleUi is { } toggle)
            {
                var action = toggle.Next(session.State.Toggle, Environment.TickCount64);
                ruleCheck = toggle.Check;
                if (action == CheckboxUiAction.Wait) return SlotEditProgress.Waiting;
                if (action != CheckboxUiAction.Complete)
                { error = "체크 상태와 모집 규칙 저장값이 요청과 달라 중단했습니다. 진단 정보를 확인해 주세요."; return SlotEditProgress.Failed; }
            }
            else
            {
                var action = session.LootUi!.Next(session.State.Loot, Environment.TickCount64);
                ruleCheck = session.LootUi.Check;
                var list = (AtkComponentList*)session.State.List;
                switch (action)
                {
                    case LootUiAction.Wait: return SlotEditProgress.Waiting;
                    case LootUiAction.ClickItem:
                        if (!list->OwnerNode->IsVisible())
                        { ruleCheck = "loot_list_hidden"; error = "전리품 규칙 목록이 화면에 표시되지 않았습니다."; return SlotEditProgress.Failed; }
                        session.ItemClickCalls++;
                        list->DispatchItemEvent(session.Edit.Value, AtkEventType.ListItemClick);
                        continue;
                    case LootUiAction.SelectItem:
                        session.SelectCalls++;
                        // Highlight travels through the dropdown to editor Param 26.
                        list->SelectItem(session.Edit.Value, true);
                        error = ""; return SlotEditProgress.Waiting;
                    case LootUiAction.Complete: break;
                    default:
                        error = "전리품 규칙의 선택 상태와 저장값이 예상한 흐름과 달라 중단했습니다. 진단 정보를 확인해 주세요.";
                        return SlotEditProgress.Failed;
                }
            }
            if (!buttonEnabled) { error = "게임의 갱신 버튼이 준비되기를 기다리고 있습니다."; return SlotEditProgress.Waiting; }
            if (!ValidateRuleGuard(addon, agent, session, true, out _, out error)) return SlotEditProgress.Failed;
            editingTarget = session.Expected;
            if (!ValidateEditor(session.Expected, out _, out _, out error)) return SlotEditProgress.Failed;
            session.Completed = true; expected = session.Expected;
            return SlotEditProgress.Complete;
        }
        return SlotEditProgress.Waiting;
    }

    private bool ValidateRuleGuard(AddonLookingForGroupCondition* addon, AgentLookingForGroup* agent,
        RuleSession session, bool final, out AtkEvent clickEvent, out string error)
    {
        clickEvent = default; error = "모집 규칙 설정 중 창 또는 다른 모집 조건이 바뀌어 중단했습니다.";
        ReadEditorAction();
        if (SelfId != session.Original.SelfId || BlockReason != null || EditorClosedRevision != editingFromClosedRevision ||
            !buttonTextReadable || buttonMode != RecruitmentEditorMode.Refresh || Visible("LookingForGroupSelectRole") ||
            addon != Plugin.GameGui.GetAddonByName<AddonLookingForGroupCondition>("LookingForGroupCondition", 1))
        { ruleCheck = "rule_session_changed"; return false; }
        if ((nint)addon != session.Addon || (nint)agent != session.Agent ||
            !NativeReadGuard.Readable(addon, (nuint)sizeof(AddonLookingForGroupCondition)) || !addon->IsVisible || !addon->IsReady ||
            !NativeReadGuard.Readable(agent, (nuint)sizeof(AgentLookingForGroup)))
        { ruleCheck = "rule_window_changed"; return false; }
        if (!ReadRuleControl(addon, agent, session.Edit, out var state, out clickEvent, out error)) return false;
        session.State = state;
        if (!state.SameControls(session.Binding))
        { ruleCheck = "rule_component_changed"; error = "모집 규칙의 입력 창이 바뀌어 중단했습니다."; return false; }
        if (RuleInvariant(agent, session.Edit.Kind) != session.Invariant)
        { ruleCheck = "other_criteria_changed"; error = "다른 모집 조건도 함께 바뀌어 갱신을 중단했습니다. 게임 수정창을 확인해 주세요."; return false; }
        if (final && (session.Edit.Kind == ListingRuleKind.Loot ?
            !state.Loot.Matches(session.Edit.Value) || !state.Loot.SettledClosed : !state.Toggle.Matches(session.Edit.Value == 1)))
        { ruleCheck = "rule_final_ui_or_stored_mismatch"; error = "모집 규칙의 화면 상태와 저장값이 일치하지 않습니다."; return false; }
        error = "";
        return true;
    }

    private bool ReadRuleControl(AddonLookingForGroupCondition* addon, AgentLookingForGroup* agent, ListingRuleEdit edit,
        out RuleControlState state, out AtkEvent clickEvent, out string error)
    {
        state = default; clickEvent = default;
        error = $"현재 게임 수정창에서 ‘{ListingRules.Title(edit.Kind)}’ 항목을 선택할 수 없습니다.";
        var flags = (byte)agent->StoredRecruitmentInfo.DutyFinderSettingFlags;
        if (edit.Kind != ListingRuleKind.Loot)
        {
            var checkbox = edit.Kind switch {
                ListingRuleKind.Unrestricted => addon->UnrestrictedPartyCheckBox,
                ListingRuleKind.MinimumItemLevel => addon->MinimumItemLevelCheckBox,
                ListingRuleKind.SilenceEcho => addon->SilenceEchoCheckbox, _ => null
            };
            uint nodeId = edit.Kind switch {
                ListingRuleKind.Unrestricted => 99, ListingRuleKind.MinimumItemLevel => 100,
                ListingRuleKind.SilenceEcho => 101, _ => 0
            };
            ruleControlRead = $"kind={edit.Kind}; checkboxParam=25; expectedNode={nodeId}; dutyFlags={flags}";
            if (!NativeReadGuard.Readable(checkbox, (nuint)sizeof(AtkComponentCheckBox)) || !ComponentOwnsNode((AtkComponentBase*)checkbox, nodeId))
            { ruleCheck = "rule_checkbox_unavailable"; return false; }
            ruleControlRead += $"; checkboxNode={checkbox->OwnerNode->NodeId}; visible={checkbox->OwnerNode->IsVisible()}; enabled={checkbox->IsEnabled}";
            if (!checkbox->OwnerNode->IsVisible() || !checkbox->IsEnabled)
            { ruleCheck = "rule_checkbox_disabled_or_hidden"; return false; }
            if (!ReadRegisteredUiEvent((AtkResNode*)checkbox->OwnerNode, (AtkEventListener*)addon, AtkEventType.ButtonClick, 25, out clickEvent))
            { ruleCheck = "rule_checkbox_event_route_mismatch"; return false; }
            state = new((nint)checkbox, (nint)checkbox->OwnerNode, nodeId, 0, 0, 0, 0, flags,
                new(checkbox->IsChecked, (byte)((flags & ListingRules.Mask(edit.Kind)) != 0 ? 1 : 0)), default);
        }
        else
        {
            var dropdown = addon->LootRulesDropDown;
            ruleControlRead = "kind=Loot; dropdownParam=26";
            if (!NativeReadGuard.Readable(dropdown, (nuint)sizeof(AtkComponentDropDownList)) || !OwnsComponent((AtkComponentBase*)dropdown) ||
                !NativeReadGuard.Readable(dropdown->Checkbox, (nuint)sizeof(AtkComponentCheckBox)) || !OwnsComponent((AtkComponentBase*)dropdown->Checkbox) ||
                !NativeReadGuard.Readable(dropdown->List, (nuint)sizeof(AtkComponentList)) || !OwnsComponent((AtkComponentBase*)dropdown->List) ||
                !NativeReadGuard.Readable(dropdown->List->VirtualTable, (nuint)sizeof(AtkComponentList.AtkComponentListVirtualTable)))
            { ruleCheck = "loot_controls_unavailable"; return false; }
            ruleControlRead += $"; dropdownNode={dropdown->OwnerNode->NodeId}; openNode={dropdown->Checkbox->OwnerNode->NodeId}; listNode={dropdown->List->OwnerNode->NodeId}; " +
                $"visible={dropdown->OwnerNode->IsVisible()}; enabled={dropdown->Checkbox->IsEnabled}; itemCount={dropdown->List->ListLength}";
            if (!dropdown->OwnerNode->IsVisible() || !dropdown->Checkbox->IsEnabled || edit.Value is < 0 or > 2 ||
                dropdown->List->ListLength != 3 || dropdown->List->GetItemDisabledState(edit.Value))
            { ruleCheck = "loot_item_unavailable"; return false; }
            if (!ReadRegisteredUiEvent((AtkResNode*)dropdown->Checkbox->OwnerNode, (AtkEventListener*)dropdown, AtkEventType.ButtonClick, 2, out clickEvent) ||
                !ReadRegisteredUiEvent((AtkResNode*)dropdown->List->OwnerNode, (AtkEventListener*)dropdown, AtkEventType.ListItemClick, 1, out _) ||
                !ReadRegisteredUiEvent((AtkResNode*)dropdown->List->OwnerNode, (AtkEventListener*)dropdown, AtkEventType.ListItemHighlight, 1, out _) ||
                !ReadRegisteredUiEvent((AtkResNode*)dropdown->OwnerNode, (AtkEventListener*)addon, AtkEventType.ListItemHighlight, 26, out _))
            { ruleCheck = "loot_event_route_mismatch"; return false; }
            state = new((nint)dropdown, (nint)dropdown->OwnerNode, dropdown->OwnerNode->NodeId,
                (nint)dropdown->Checkbox, (nint)dropdown->Checkbox->OwnerNode, (nint)dropdown->List, (nint)dropdown->List->OwnerNode, flags, default,
                new(dropdown->GetSelectedItemIndex(), dropdown->List->SelectedItemIndex, (byte)agent->StoredRecruitmentInfo.LootRule,
                    dropdown->IsOpen, dropdown->OpenStateChangePending, dropdown->Checkbox->IsChecked, dropdown->List->ListLength));
        }
        error = "";
        return true;
    }

    private static string RuleInvariant(AgentLookingForGroup* agent, ListingRuleKind kind) =>
        DraftInvariant(agent, ignoredDutyFlags: ListingRules.Mask(kind), includeLootRule: kind != ListingRuleKind.Loot) + "/" +
        Convert.ToHexString(MemoryMarshal.AsBytes(agent->StoredRecruitmentInfo.SlotFlags));
}
