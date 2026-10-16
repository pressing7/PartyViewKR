using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace PartyViewKR;

internal sealed unsafe partial class ListingRefreshHost : IListingBatchHost
{
    private bool batchCleanupSent, batchSubmitSent;

    public bool PrepareBatchStep(RefreshTarget identity, out RefreshTarget current, out string error)
    {
        if (!ReadBatchDraft(identity, out current, out error)) return false;
        if (Visible("LookingForGroupSelectRole"))
        { error = "직업 선택창이 아직 열려 있습니다."; return false; }
        var addon = Plugin.GameGui.GetAddonByName<AddonLookingForGroupCondition>("LookingForGroupCondition", 1);
        var priorDropdown = BatchDropdown(addon);
        if (priorDropdown != null && NativeReadGuard.Readable(priorDropdown, (nuint)sizeof(AtkComponentDropDownList)) &&
            (priorDropdown->IsOpen || priorDropdown->OpenStateChangePending))
        { error = "이전 항목의 선택 목록이 아직 열려 있습니다."; return false; }
        batchCleanupSent = false;
        // Completed single-item guards intentionally retain the previous item.
        // Start each item against the newly read, cumulative game draft instead.
        slotEdit = null; descriptionSession = null; objectiveSession = null;
        beginnerSession = null; uniqueJobsSession = null; privateSession = null;
        completionSession = null; itemLevelSession = null; ruleSession = null;
        editingTarget = current;
        return true;
    }

    public SlotEditProgress FinishBatchStep(RefreshTarget identity, out RefreshTarget current, out string error)
    {
        current = identity;
        if (!ReadBatchWindow(identity, out var addon, out _, out error)) return SlotEditProgress.Failed;
        if (!batchCleanupSent)
        {
            var role = Plugin.GameGui.GetAddonByName<AtkUnitBase>("LookingForGroupSelectRole", 1);
            if (Visible("LookingForGroupSelectRole"))
            {
                if (slotEdit == null || Interlocked.Read(ref roleOpenedRevision) != slotEdit.OpenedRevision + 1 ||
                    !NativeReadGuard.Readable(role, (nuint)sizeof(AtkUnitBase)) || !role->IsReady ||
                    !NativeReadGuard.Readable(role->VirtualTable, (nuint)sizeof(AtkUnitBase.AtkUnitBaseVirtualTable)))
                { error = "직업 선택창을 닫을 수 없어 해당 항목을 마치지 못했습니다."; return SlotEditProgress.Failed; }
                // Close only the child opened by this item. Closing sends the
                // game's close callback; it does not click an unverified Confirm.
                batchCleanupSent = true; // Never repeat Close if native dispatch throws.
                role->Close(true);
                return SlotEditProgress.Waiting;
            }
            var dropdown = BatchDropdown(addon);
            if (dropdown != null && NativeReadGuard.Readable(dropdown, (nuint)sizeof(AtkComponentDropDownList)) &&
                OwnsComponent((AtkComponentBase*)dropdown))
            {
                if (dropdown->OpenStateChangePending) { error = "선택 목록 상태 대기"; return SlotEditProgress.Waiting; }
                if (dropdown->IsOpen)
                {
                    if (!NativeReadGuard.Readable(dropdown->Checkbox, (nuint)sizeof(AtkComponentCheckBox)) ||
                        !OwnsComponent((AtkComponentBase*)dropdown->Checkbox) ||
                        !ReadRegisteredUiEvent((AtkResNode*)dropdown->Checkbox->OwnerNode, (AtkEventListener*)dropdown,
                            AtkEventType.ButtonClick, 2, out var evt))
                    { error = "선택 목록을 닫는 이벤트를 확인하지 못했습니다."; return SlotEditProgress.Failed; }
                    batchCleanupSent = true;
                    dropdown->Checkbox->SetChecked(false);
                    evt.NextEvent = null;
                    var data = new AtkEventData();
                    dropdown->ReceiveEvent(AtkEventType.ButtonClick, (int)evt.Param, &evt, &data);
                    return SlotEditProgress.Waiting;
                }
            }
        }
        if (Visible("LookingForGroupSelectRole")) { error = "직업 선택창 닫힘 대기"; return SlotEditProgress.Waiting; }
        var remaining = BatchDropdown(addon);
        if (remaining != null && NativeReadGuard.Readable(remaining, (nuint)sizeof(AtkComponentDropDownList)) &&
            (remaining->IsOpen || remaining->OpenStateChangePending))
        { error = "선택 목록 닫힘 대기"; return SlotEditProgress.Waiting; }
        if (!ReadBatchDraft(identity, out current, out error)) return SlotEditProgress.Failed;
        var descriptionRecovery = RecoverBatchDescription(addon, current, out error);
        if (descriptionRecovery != SlotEditProgress.Complete) return descriptionRecovery;
        editingTarget = current;
        return SlotEditProgress.Complete;
    }

    private SlotEditProgress RecoverBatchDescription(AddonLookingForGroupCondition* addon, RefreshTarget current, out string error)
    {
        error = "";
        if (descriptionSession is not { InputCalls: > 0, Completed: false } session) return SlotEditProgress.Complete;
        var input = addon->CommentTextInput;
        if ((nint)input != session.Input || !NativeReadGuard.Readable(input, (nuint)sizeof(AtkComponentTextInput)) ||
            !OwnsComponent((AtkComponentBase*)input) || !input->Enabled ||
            !NativeReadGuard.Text(&input->EvaluatedString, out var visible))
        { error = "소개말 입력칸을 다시 확인하지 못했습니다."; return SlotEditProgress.Failed; }
        // ReadBatchDraft has already verified the owned editor and read the
        // actual stored comment. Restore the UI only, never the requested text
        // or other draft fields, so later items can keep their normal guards.
        var action = session.Recovery.Next(visible, current.Description, session.Edit.Value, Environment.TickCount64, session.InputText);
        if (action == DescriptionRecoveryAction.RestoreInput)
        {
            try { input->SetText(current.Description); }
            catch (Exception)
            {
                // SetText may have taken effect before throwing. Observe once
                // more on the next update without dispatching another input.
                error = "소개말 입력칸 정리 결과 확인 중";
                return SlotEditProgress.Waiting;
            }
        }
        if (action is DescriptionRecoveryAction.RestoreInput or DescriptionRecoveryAction.Wait)
        { error = "소개말 입력칸 정리 결과 확인 중"; return SlotEditProgress.Waiting; }
        if (action == DescriptionRecoveryAction.Failed)
        { error = "소개말 입력칸과 저장된 내용이 일치하지 않아 정리를 확인하지 못했습니다."; return SlotEditProgress.Failed; }
        return SlotEditProgress.Complete;
    }

    private AtkComponentDropDownList* BatchDropdown(AddonLookingForGroupCondition* addon) =>
        objectiveSession != null ? addon->ObjectiveDropDown : completionSession != null ? addon->CompletionStatusDropDown :
        ruleSession?.Edit.Kind == ListingRuleKind.Loot ? addon->LootRulesDropDown : null;

    public bool SubmitBatch(RefreshTarget identity, out string error)
    {
        if (batchSubmitSent) { error = "이미 갱신 요청을 보냈습니다."; return false; }
        // Item verification may have failed. The user's policy is to submit the
        // actual game draft anyway, then report the fresh listing per item.
        // Keep only the identity/lifecycle/native-read and real Refresh gates.
        if (!ReadBatchWindow(identity, out var addon, out var agent, out error)) return false;
        if (Visible("LookingForGroupSelectRole")) { error = "직업 선택창이 닫히지 않아 갱신하지 못했습니다."; return false; }
        if (!ReadEditorAction() || buttonMode != RecruitmentEditorMode.Refresh)
        { error = "게임의 ‘모집 내용 갱신’ 버튼을 누를 수 없습니다. 입력 조건을 확인해 주세요."; return false; }
        if (!NativeReadGuard.FixedText(agent->StoredRecruitmentInfo.Comment, out var comment))
        { error = "갱신에 전달할 현재 소개말을 읽지 못했습니다."; return false; }
        batchSubmitSent = true;
        check = "submit_batch_actual_draft_once";
        // The KR refresh command must carry the current comment again. Even
        // partial edits use the stored text, never the failed requested text.
        FireDescriptionCallback((AtkUnitBase*)addon, 0, comment);
        editingTarget = null;
        error = ""; return true;
    }

    private bool ReadBatchWindow(RefreshTarget identity, out AddonLookingForGroupCondition* addon,
        out AgentLookingForGroup* agent, out string error)
    {
        addon = null; agent = null;
        error = "본인 모집 수정창을 확인하지 못했습니다.";
        if (SelfId != identity.SelfId || BlockReason != null || editingTarget == null ||
            editingTarget.SelfId != identity.SelfId || editingTarget.ListingId != identity.ListingId ||
            EditorRevision <= editingFromRevision || EditorClosedRevision != editingFromClosedRevision) return false;
        ReadEditorAction();
        if (!buttonTextReadable || buttonMode != RecruitmentEditorMode.Refresh) return false;
        addon = Plugin.GameGui.GetAddonByName<AddonLookingForGroupCondition>("LookingForGroupCondition", 1);
        agent = AgentLookingForGroup.Instance();
        return NativeReadGuard.Readable(addon, (nuint)sizeof(AddonLookingForGroupCondition)) && addon->IsReady && addon->IsVisible &&
            NativeReadGuard.Readable(addon->VirtualTable, (nuint)sizeof(AtkUnitBase.AtkUnitBaseVirtualTable)) &&
            NativeReadGuard.Readable(agent, (nuint)sizeof(AgentLookingForGroup));
    }

    private bool ReadBatchDraft(RefreshTarget identity, out RefreshTarget current, out string error)
    {
        current = identity;
        if (!ReadBatchWindow(identity, out _, out var agent, out error)) return false;
        ref var draft = ref agent->StoredRecruitmentInfo;
        if (!ListingSlotRoute.TryCreate(identity.Slots, 1, out var route) ||
            !route.MatchesEditorShape(draft.NumberOfGroups, draft.NumberOfSlotsInMainParty) ||
            !identity.Slots.MatchesMembers(draft.MemberContentIds) || draft.SelectedDutyId != identity.DutyId)
        { error = "수정창의 참가자 또는 모집 대상이 바뀌어 해당 항목을 건너뜁니다."; return false; }
        if (!NativeReadGuard.FixedText(draft.Comment, out var comment))
        { error = "수정창의 소개말을 읽지 못했습니다."; return false; }
        if (!ListingUniqueJobs.TryFlags(identity.ExtraCriteria, out var join) ||
            draft.OnePlayerPerJob > 1 || agent->AvgItemLvEnabled > 1 ||
            ListingPrivate.StoredEnabled(draft.Password) is not { } isPrivate)
        { error = "수정창의 추가 조건을 읽지 못했습니다."; return false; }
        var extra = identity.ExtraCriteria.Split('/');
        extra[0] = (agent->AvgItemLvEnabled != 0 ? agent->AvgItemLv : 0).ToString();
        join = (byte)(draft.OnePlayerPerJob != 0 ? join | ListingUniqueJobs.Flag : join & ~ListingUniqueJobs.Flag);
        join = (byte)(isPrivate ? join | ListingPrivate.Flag : join & ~ListingPrivate.Flag);
        extra[1] = join.ToString();
        return ListingBatchSnapshot.TryRead(identity, DraftCriteria(ref draft, identity.Slots.NativeSlotCount),
            comment, string.Join('/', extra), out current, out error);
    }
}
