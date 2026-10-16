using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace PartyViewKR;

internal sealed unsafe partial class ListingRefreshHost
{
    private PrivateSession? privateSession;
    private string privateCheck = "not_started";
    private string privateControlRead = "not_started";
    private string PrivateEditDiagnostic => $"privateEditCheck={privateCheck}; privateEditPhase={privateSession?.Ui.Phase ?? "none"}; " +
        $"privateDirectCallbackCalls=0; privateCheckCalls={privateSession?.CheckCalls ?? 0}; privateClickCalls={privateSession?.ClickCalls ?? 0}; " +
        $"privatePasswordInputCalls={privateSession?.InputCalls ?? 0}; privateRequested={privateSession?.Edit.Enabled.ToString() ?? "unknown"}; " +
        $"privateUiChecked={privateSession?.State.Checked.ToString() ?? "unknown"}; privateStoredEnabled={privateSession?.State.StoredEnabled?.ToString() ?? "unknown"}; " +
        $"privatePasswordLength={(privateSession?.Edit.Enabled == true ? privateSession.Edit.Password.Length : 0)}; " +
        $"privatePasswordUiMatches={privateSession?.PasswordUiMatches.ToString() ?? "unknown"}; privatePasswordStoredMatches={privateSession?.PasswordStoredMatches.ToString() ?? "unknown"}; " +
        $"privateInputReady={privateSession?.State.InputReady.ToString() ?? "unknown"}; privateCheckboxNode={privateSession?.CheckboxNodeId.ToString() ?? "unknown"}; " +
        $"privatePasswordNode={privateSession?.InputNodeId.ToString() ?? "unknown"}; privateParam=29; privatePasswordServerCheck=unavailable\nprivateControlRead={privateControlRead}";

    private sealed class PrivateSession(RefreshTarget original, ListingPrivateEdit edit, string invariant,
        AddonLookingForGroupCondition* addon, AgentLookingForGroup* agent, AtkComponentCheckBox* checkbox, AtkComponentNumericInput* input)
    {
        internal readonly RefreshTarget Original = original, Expected = edit.Expected(original);
        internal readonly ListingPrivateEdit Edit = edit;
        internal readonly string Invariant = invariant;
        internal readonly nint Addon = (nint)addon, Agent = (nint)agent, Checkbox = (nint)checkbox, Input = (nint)input,
            CheckboxNode = (nint)checkbox->OwnerNode, InputNode = (nint)input->OwnerNode;
        internal readonly uint CheckboxNodeId = checkbox->OwnerNode->NodeId, InputNodeId = input->OwnerNode->NodeId;
        internal readonly ListingPrivateUi Ui = new(ListingPrivate.Read(original.ExtraCriteria)!.Value, edit.Enabled, edit.PasswordValue);
        internal PrivateUiState State;
        internal int CheckCalls, ClickCalls, InputCalls;
        internal bool Completed;
        internal bool PasswordUiMatches => Edit.Enabled && State.NumericValue == Edit.PasswordValue;
        internal bool PasswordStoredMatches => Edit.Enabled && State.StoredPassword == Edit.PasswordValue;
    }

    public bool BeginPrivateEdit(RefreshTarget target, ListingPrivateEdit edit, out string error)
    {
        privateSession = null; privateCheck = "validating"; privateControlRead = "not_read";
        error = "기존 비공개 설정을 확인하지 못했습니다.";
        if (!edit.MatchesOriginal(target) || Visible("LookingForGroupSelectRole")) return false;
        if (!ValidateEditor(target, out var addon, out var agent, out error)) return false;
        if (!ReadPrivateControls(addon, agent, out var checkbox, out var input, out var state, out _, out error)) return false;
        if (!ListingSlotRoute.TryCreate(target.Slots, 1, out var route) ||
            !route.MatchesEditorShape(agent->StoredRecruitmentInfo.NumberOfGroups, agent->StoredRecruitmentInfo.NumberOfSlotsInMainParty) ||
            !agent->StoredRecruitmentInfo.MemberContentIds[..target.TotalSlots].SequenceEqual(target.Slots.Slots.Select(s => s.MemberContentId).ToArray()))
        { privateCheck = "members_or_shape_changed"; error = "모집 수정창의 파티 구성이 달라 중단했습니다."; return false; }
        var session = privateSession = new(target, edit, PrivateInvariant(agent), addon, agent, checkbox, input) { State = state };
        return AdvancePrivateEdit(addon, agent, session, out _, out error);
    }

    public SlotEditProgress UpdatePrivateEdit(out RefreshTarget expected, out string error)
    {
        expected = null!; error = "비공개 설정을 확인하지 못했습니다.";
        var session = privateSession;
        if (session == null || session.Completed) return SlotEditProgress.Failed;
        if (!ReferenceEquals(editingTarget, session.Original))
        { privateCheck = "private_binding_changed"; return SlotEditProgress.Failed; }
        var addon = Plugin.GameGui.GetAddonByName<AddonLookingForGroupCondition>("LookingForGroupCondition", 1);
        var agent = AgentLookingForGroup.Instance();
        if (!ValidatePrivateGuard(addon, agent, session, false, out _, out error) ||
            !AdvancePrivateEdit(addon, agent, session, out var complete, out error)) return SlotEditProgress.Failed;
        if (!complete || !buttonEnabled) return SlotEditProgress.Waiting;
        if (!ValidatePrivateGuard(addon, agent, session, true, out _, out error)) return SlotEditProgress.Failed;
        editingTarget = session.Expected;
        if (!ValidateEditor(session.Expected, out _, out _, out error)) return SlotEditProgress.Failed;
        session.Completed = true; expected = session.Expected;
        return SlotEditProgress.Complete;
    }

    private bool AdvancePrivateEdit(AddonLookingForGroupCondition* addon, AgentLookingForGroup* agent,
        PrivateSession session, out bool complete, out string error)
    {
        complete = false; error = "비공개 체크 상태 또는 비밀번호 저장값이 요청과 달라 중단했습니다.";
        var action = session.Ui.Next(session.State, Environment.TickCount64);
        privateCheck = session.Ui.Check;
        if (action == PrivateUiAction.Failed) return false;
        if (action == PrivateUiAction.Complete) { complete = true; error = ""; return true; }
        if (action == PrivateUiAction.Wait) { error = ""; return true; }
        if (!ValidatePrivateGuard(addon, agent, session, false, out var clickEvent, out error)) return false;
        if (action == PrivateUiAction.Toggle)
        {
            var storedBefore = session.State.StoredPassword;
            session.CheckCalls++;
            addon->FormPrivatePartyCheckbox->SetChecked(session.Edit.Enabled);
            if (!ValidatePrivateGuard(addon, agent, session, false, out clickEvent, out error)) return false;
            if (session.State.Checked != session.Edit.Enabled || session.State.StoredPassword != storedBefore)
            { privateCheck = "private_preclick_state_mismatch"; error = "비공개 체크박스의 클릭 준비 상태가 예상과 달라 중단했습니다."; return false; }
            // KR manual trace: ButtonClick(25), Param 29 -> callback 21, UInt 1/0.
            // Use the registered event, preserving its optional Node argument.
            session.ClickCalls++;
            clickEvent.NextEvent = null;
            var data = new AtkEventData();
            addon->ReceiveEvent(AtkEventType.ButtonClick, (int)clickEvent.Param, &clickEvent, &data);
        }
        else if (action == PrivateUiAction.Input)
        {
            var input = addon->PasswordNumericInput;
            var value = session.Edit.PasswordValue;
            if (!session.State.Checked || session.State.StoredEnabled != true || !session.State.InputReady ||
                input->Data.Min > value || input->Data.Max < value)
            { privateCheck = "private_password_input_or_range_unavailable"; error = "게임 비밀번호 입력창이 준비되지 않았거나 입력 범위를 벗어났습니다."; return false; }
            // InnerSetValue's named triggerCallback argument uses the native
            // numeric control to construct its ValueUpdate data and notify the
            // registered editor route. Do not synthesize callback 16 or write
            // StoredRecruitmentInfo.Password. KR trace confirms ValueUpdate(27),
            // Param 29 and numeric transport (e.g. leading zeroes are formatting).
            session.InputCalls++;
            input->InnerSetValue(value, triggerCallback: true, playSoundEffect: false);
        }
        error = "";
        return true;
    }

    private bool ValidatePrivateGuard(AddonLookingForGroupCondition* addon, AgentLookingForGroup* agent,
        PrivateSession session, bool final, out AtkEvent clickEvent, out string error)
    {
        clickEvent = default; error = "비공개 설정 중 창 또는 다른 모집 조건이 바뀌어 중단했습니다.";
        ReadEditorAction();
        if (SelfId != session.Original.SelfId || BlockReason != null || EditorClosedRevision != editingFromClosedRevision ||
            !buttonTextReadable || buttonMode != RecruitmentEditorMode.Refresh || Visible("LookingForGroupSelectRole") ||
            addon != Plugin.GameGui.GetAddonByName<AddonLookingForGroupCondition>("LookingForGroupCondition", 1))
        { privateCheck = "private_session_changed"; return false; }
        if ((nint)addon != session.Addon || (nint)agent != session.Agent ||
            !NativeReadGuard.Readable(addon, (nuint)sizeof(AddonLookingForGroupCondition)) || !addon->IsVisible || !addon->IsReady ||
            !NativeReadGuard.Readable(agent, (nuint)sizeof(AgentLookingForGroup)))
        { privateCheck = "private_window_changed"; return false; }
        if ((nint)addon->FormPrivatePartyCheckbox != session.Checkbox || (nint)addon->PasswordNumericInput != session.Input)
        { privateCheck = "private_component_changed"; return false; }
        if (PrivateInvariant(agent) != session.Invariant)
        { privateCheck = "other_criteria_changed"; return false; }
        if (!ReadPrivateControls(addon, agent, out var checkbox, out var input, out var state, out clickEvent, out error)) return false;
        session.State = state;
        if ((nint)checkbox->OwnerNode != session.CheckboxNode || checkbox->OwnerNode->NodeId != session.CheckboxNodeId ||
            (nint)input->OwnerNode != session.InputNode || input->OwnerNode->NodeId != session.InputNodeId)
        { privateCheck = "private_node_changed"; return false; }
        if (final && !state.Matches(session.Edit.Enabled, session.Edit.PasswordValue))
        { privateCheck = "private_final_ui_or_stored_mismatch"; error = "비공개 설정과 비밀번호 반영을 확인하지 못해 갱신하지 않았습니다."; return false; }
        error = ""; return true;
    }

    private bool ReadPrivateControls(AddonLookingForGroupCondition* addon, AgentLookingForGroup* agent,
        out AtkComponentCheckBox* checkbox, out AtkComponentNumericInput* input, out PrivateUiState state,
        out AtkEvent clickEvent, out string error)
    {
        checkbox = addon->FormPrivatePartyCheckbox; input = addon->PasswordNumericInput;
        state = default; clickEvent = default; error = "현재 게임 수정창의 비공개 설정과 비밀번호 입력창을 확인하지 못했습니다.";
        if (!NativeReadGuard.Readable(checkbox, (nuint)sizeof(AtkComponentCheckBox)) ||
            !NativeReadGuard.Readable(checkbox->OwnerNode, (nuint)sizeof(AtkComponentNode)) ||
            checkbox->OwnerNode->Component != (AtkComponentBase*)checkbox || !checkbox->OwnerNode->IsVisible() || !checkbox->IsEnabled ||
            !NativeReadGuard.Readable(input, (nuint)sizeof(AtkComponentNumericInput)) ||
            !NativeReadGuard.Readable(input->OwnerNode, (nuint)sizeof(AtkComponentNode)) || input->OwnerNode->Component != (AtkComponentBase*)input)
        { privateCheck = "private_control_unavailable"; return false; }
        state = new(checkbox->IsChecked, agent->StoredRecruitmentInfo.Password, input->Value,
            input->OwnerNode->IsVisible() && (input->OwnerNode->NodeFlags & NodeFlags.Enabled) != 0 && input->EnableTextInput);
        privateControlRead = $"read; checkboxNode={checkbox->OwnerNode->NodeId}; inputNode={input->OwnerNode->NodeId}; " +
            $"inputVisible={input->OwnerNode->IsVisible()}; inputNodeEnabled={(input->OwnerNode->NodeFlags & NodeFlags.Enabled) != 0}; " +
            $"textInputEnabled={input->EnableTextInput}; digits={input->Digits}; zeroPadding={input->AddZeroPadding}; " +
            $"minimum={input->Data.Min}; maximum={input->Data.Max}";
        if (!state.StoredEnabled.HasValue) { privateCheck = "private_stored_unknown"; return false; }
        if (!ReadRegisteredUiEvent((AtkResNode*)checkbox->OwnerNode, (AtkEventListener*)addon, AtkEventType.ButtonClick, 29, out clickEvent))
        { privateCheck = "private_checkbox_route_mismatch"; return false; }
        if (!ReadRegisteredUiEvent((AtkResNode*)input->OwnerNode, (AtkEventListener*)addon, AtkEventType.ValueUpdate, 29, out _))
        { privateCheck = "private_password_route_mismatch"; return false; }
        error = ""; return true;
    }

    private static string PrivateInvariant(AgentLookingForGroup* agent) => DraftInvariant(agent, includePassword: false) + "/" +
        Convert.ToHexString(MemoryMarshal.AsBytes(agent->StoredRecruitmentInfo.SlotFlags));
}
