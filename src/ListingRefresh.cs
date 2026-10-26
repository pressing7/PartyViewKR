using System.Text.RegularExpressions;

namespace PartyViewKR;

internal sealed record RefreshTarget(ulong SelfId, ulong ListingId, ushort DutyId,
    string Description, string Criteria, int? RemainingSeconds, int TotalSlots)
{
    internal ListingSlots Slots { get; init; } = ListingSlots.Unavailable("not_captured");
    internal string ExtraCriteria { get; init; } = "";
    internal bool JobEditingAllowed { get; init; }
}

internal enum RecruitmentEditorMode { Unknown, Refresh, Create }

internal interface IListingRefreshHost
{
    ulong SelfId { get; }
    string? BlockReason { get; }
    bool QueryBusy { get; }
    bool QuerySucceeded { get; }
    long DetailRevision { get; }
    bool EditorVisible { get; }
    bool EditorReady { get; }
    RecruitmentEditorMode EditorActionMode { get; }
    long EditorRevision { get; }
    long EditorClosedRevision { get; }
    void QueryOwn();
    void CancelQuery();
    bool TryGetOwn(out RefreshTarget target, out string error);
    bool OpenEditor(RefreshTarget target, out string error);
    bool BeginSlotEdit(RefreshTarget target, ListingJobEdit edit, out string error);
    SlotEditProgress UpdateSlotEdit(out RefreshTarget expected, out string error);
    bool BeginDescriptionEdit(RefreshTarget target, ListingDescriptionEdit edit, out string error);
    SlotEditProgress UpdateDescriptionEdit(out RefreshTarget expected, out string error);
    bool BeginObjectiveEdit(RefreshTarget target, ListingObjectiveEdit edit, out string error);
    SlotEditProgress UpdateObjectiveEdit(out RefreshTarget expected, out string error);
    bool BeginBeginnerEdit(RefreshTarget target, ListingBeginnerEdit edit, out string error);
    SlotEditProgress UpdateBeginnerEdit(out RefreshTarget expected, out string error);
    bool BeginUniqueJobsEdit(RefreshTarget target, ListingUniqueJobsEdit edit, out string error);
    SlotEditProgress UpdateUniqueJobsEdit(out RefreshTarget expected, out string error);
    bool BeginPrivateEdit(RefreshTarget target, ListingPrivateEdit edit, out string error);
    SlotEditProgress UpdatePrivateEdit(out RefreshTarget expected, out string error);
    bool BeginCompletionEdit(RefreshTarget target, ListingCompletionEdit edit, out string error);
    SlotEditProgress UpdateCompletionEdit(out RefreshTarget expected, out string error);
    bool BeginItemLevelEdit(RefreshTarget target, ListingItemLevelEdit edit, out string error);
    SlotEditProgress UpdateItemLevelEdit(out RefreshTarget expected, out string error);
    bool BeginRuleEdit(RefreshTarget target, ListingRuleEdit edit, out string error);
    SlotEditProgress UpdateRuleEdit(out RefreshTarget expected, out string error);
    // null keeps the existing button route; an empty string explicitly clears
    // the introduction and must still be carried in the final submission.
    bool Submit(RefreshTarget target, string? description, out string error);
}

internal enum RefreshStage { Idle, Queued, ReadingOwn, OpeningEditor, EditingSlot, EditingDescription, EditingObjective, EditingBeginner, EditingUniqueJobs, EditingPrivate, EditingCompletion, EditingItemLevel, EditingRule, EditingBatch, WaitingResult, Verifying, Completed, Unconfirmed, Stopped }

// No timers/tasks or automatic retries: every run comes from one button press.
// Advancing the stage BEFORE dispatch also prevents a thrown callback being retried.
internal sealed class ListingRefresh(IListingRefreshHost host)
{
    private RefreshStage singleStage;
    private ListingBatchRefresh? batch;
    internal RefreshStage Stage { get => batch?.Stage ?? singleStage; private set => singleStage = value; }
    internal IReadOnlyList<ListingChangeResult> BatchResults => batch?.Results ?? [];
    internal bool IsBatch => batch != null;
    internal long RequestRevision { get; private set; }
    internal bool EditorStarted => batch?.EditorStarted ?? editCalls > 0;
    internal bool Submitted => batch?.Submitted ?? submitCalls > 0;
    private RefreshTarget? boundSource;
    internal bool Busy => batch?.Busy ?? (Stage is RefreshStage.Queued or RefreshStage.ReadingOwn or
        RefreshStage.OpeningEditor or RefreshStage.EditingSlot or RefreshStage.EditingDescription or RefreshStage.EditingObjective or RefreshStage.EditingBeginner or RefreshStage.EditingUniqueJobs or RefreshStage.EditingPrivate or RefreshStage.EditingCompletion or RefreshStage.EditingItemLevel or RefreshStage.EditingRule or RefreshStage.WaitingResult or RefreshStage.Verifying);
    internal ListingJobEdit? JobEdit { get; private set; }
    internal ListingDescriptionEdit? DescriptionEdit { get; private set; }
    internal ListingObjectiveEdit? ObjectiveEdit { get; private set; }
    internal ListingBeginnerEdit? BeginnerEdit { get; private set; }
    internal ListingUniqueJobsEdit? UniqueJobsEdit { get; private set; }
    internal ListingPrivateEdit? PrivateEdit { get; private set; }
    internal ListingCompletionEdit? CompletionEdit { get; private set; }
    internal ListingItemLevelEdit? ItemLevelEdit { get; private set; }
    internal ListingRuleEdit? RuleEdit { get; private set; }
    private string singleStatus = "버튼을 누르면 현재 모집 조건으로 한 번 갱신합니다.";
    internal string Status { get => batch?.Status ?? singleStatus; private set => singleStatus = value; }
    internal bool CoolingDown(long now) => now < Math.Max(cooldownUntil, batch?.CooldownUntil ?? 0);
    internal string Diagnostic => batch?.Diagnostic ?? $"refreshStage={Stage}; refreshEditCalls={editCalls}; refreshSubmitCalls={submitCalls}; " +
        $"refreshBeforeSeconds={target?.RemainingSeconds?.ToString() ?? "unknown"}; refreshAfterSeconds={afterSeconds?.ToString() ?? "unknown"}; " +
        $"refreshOwnWaitChecks={ownWaitChecks}; refreshEditorMode={editorMode}; refreshJobSlot={JobEdit?.Position ?? 0}; " +
        $"refreshDescriptionEdit={DescriptionEdit != null}; refreshObjectiveEdit={ObjectiveEdit != null}; refreshBeginnerEdit={BeginnerEdit != null}; refreshUniqueJobsEdit={UniqueJobsEdit != null}; refreshPrivateEdit={PrivateEdit != null}; refreshCompletionEdit={CompletionEdit != null}; refreshItemLevelEdit={ItemLevelEdit != null}; refreshRuleEdit={RuleEdit?.Kind.ToString() ?? "none"}\nrefreshResult={result}\n{descriptionVerification}\n{objectiveVerification}\n{beginnerVerification}\n{uniqueJobsVerification}\n{privateVerification}\n{completionVerification}\n{itemLevelVerification}\n{ruleVerification}";

    private RefreshTarget? target;
    private ulong self;
    private long stageTick, queryRevision, editorRevision, editorClosedRevision, cooldownUntil;
    private int editCalls, submitCalls, ownWaitChecks;
    private int? afterSeconds;
    private string result = "not_started";
    private string ownWaitReason = "";
    private string descriptionVerification = "descriptionResultCheck=not_started";
    private string objectiveVerification = "objectiveResultCheck=not_started";
    private string beginnerVerification = "beginnerResultCheck=not_started";
    private string uniqueJobsVerification = "uniqueJobsResultCheck=not_started";
    private string privateVerification = "privateResultCheck=not_started";
    private string completionVerification = "completionResultCheck=not_started";
    private string itemLevelVerification = "itemLevelResultCheck=not_started";
    private string ruleVerification = "ruleResultCheck=not_started";
    private RecruitmentEditorMode editorMode;

    internal void RequestBatch(long now, ListingPendingChanges pending)
    {
        var changes = pending.Items.ToArray();
        RequestBatch(now, changes, () => pending.Consume(changes));
    }

    internal void RequestBatch(long now, ListingChange[] changes) => RequestBatch(now, changes, null);

    private void RequestBatch(long now, ListingChange[] changes, Action? editorStarting)
    {
        if (Busy || CoolingDown(now) || host is not IListingBatchHost batchHost) return;
        RequestRevision++; boundSource = null;
        JobEdit = null; DescriptionEdit = null; ObjectiveEdit = null; BeginnerEdit = null;
        UniqueJobsEdit = null; PrivateEdit = null; CompletionEdit = null; ItemLevelEdit = null; RuleEdit = null;
        batch = new(batchHost, changes, now, editorStarting);
    }

    internal void Request(long now, ListingJobEdit? edit = null, ListingDescriptionEdit? descriptionEdit = null,
        ListingObjectiveEdit? objectiveEdit = null, ListingBeginnerEdit? beginnerEdit = null, ListingUniqueJobsEdit? uniqueJobsEdit = null, ListingPrivateEdit? privateEdit = null, ListingCompletionEdit? completionEdit = null, ListingItemLevelEdit? itemLevelEdit = null, ListingRuleEdit? ruleEdit = null, RefreshTarget? source = null)
    {
        if (Busy || CoolingDown(now)) return;
        RequestRevision++; boundSource = source;
        batch = null;
        JobEdit = edit;
        DescriptionEdit = descriptionEdit;
        ObjectiveEdit = objectiveEdit;
        BeginnerEdit = beginnerEdit;
        UniqueJobsEdit = uniqueJobsEdit;
        PrivateEdit = privateEdit;
        CompletionEdit = completionEdit;
        ItemLevelEdit = itemLevelEdit;
        RuleEdit = ruleEdit;
        target = null;
        editCalls = submitCalls = ownWaitChecks = 0;
        ownWaitReason = "";
        descriptionVerification = "descriptionResultCheck=not_started";
        objectiveVerification = "objectiveResultCheck=not_started";
        beginnerVerification = "beginnerResultCheck=not_started";
        uniqueJobsVerification = "uniqueJobsResultCheck=not_started";
        privateVerification = "privateResultCheck=not_started";
        completionVerification = "completionResultCheck=not_started";
        itemLevelVerification = "itemLevelResultCheck=not_started";
        ruleVerification = "ruleResultCheck=not_started";
        editorMode = RecruitmentEditorMode.Unknown;
        afterSeconds = null;
        if (ruleEdit != null && (edit != null || descriptionEdit != null || objectiveEdit != null || beginnerEdit != null || uniqueJobsEdit != null || privateEdit != null || completionEdit != null || itemLevelEdit != null || !ruleEdit.Valid))
        { Stop("변경할 모집 규칙을 다시 확인해 주세요.", "invalid_rule_edit"); return; }
        if (itemLevelEdit != null && (edit != null || descriptionEdit != null || objectiveEdit != null || beginnerEdit != null || uniqueJobsEdit != null || privateEdit != null || completionEdit != null || !itemLevelEdit.Valid))
        { Stop("평균 아이템 레벨 설정과 1~999 범위의 숫자를 다시 확인해 주세요.", "invalid_item_level_edit"); return; }
        if (completionEdit != null && (edit != null || descriptionEdit != null || objectiveEdit != null || beginnerEdit != null || uniqueJobsEdit != null || privateEdit != null || !completionEdit.Valid))
        { Stop("변경할 완료조건을 다시 확인해 주세요.", "invalid_completion_edit"); return; }
        if (privateEdit != null && (edit != null || descriptionEdit != null || objectiveEdit != null || beginnerEdit != null || uniqueJobsEdit != null || !privateEdit.Valid))
        { Stop("비공개 설정과 네 자리 비밀번호를 다시 확인해 주세요.", "invalid_private_edit"); return; }
        if (uniqueJobsEdit != null && (edit != null || descriptionEdit != null || objectiveEdit != null || beginnerEdit != null || !uniqueJobsEdit.Valid))
        { Stop("변경할 잡 중복 없음 설정을 다시 확인해 주세요.", "invalid_unique_jobs_edit"); return; }
        if (beginnerEdit != null && (edit != null || descriptionEdit != null || objectiveEdit != null || !beginnerEdit.Valid))
        { Stop("변경할 초행 및 초보자 환영 설정을 다시 확인해 주세요.", "invalid_beginner_edit"); return; }
        if (objectiveEdit != null && (edit != null || descriptionEdit != null || !objectiveEdit.Valid))
        { Stop("변경할 모집 목적을 다시 확인해 주세요.", "invalid_objective_edit"); return; }
        if (descriptionEdit != null && (edit != null || !descriptionEdit.Valid))
        { Stop("모집 소개말을 다시 확인해 주세요.", "invalid_description_edit"); return; }
        if (edit != null && !edit.Valid) { Stop("허용 직업과 빈자리를 다시 확인해 주세요.", "invalid_job_edit"); return; }
        result = "pending";
        SetStage(RefreshStage.Queued, now, "갱신 준비 중");
    }

    internal void Update(long now)
    {
        if (batch != null) { batch.Update(now); return; }
        if (!Busy) return;
        if (host.BlockReason is { } blocked) { Stop(blocked, "blocked"); return; }
        if (Stage != RefreshStage.Queued && self != host.SelfId)
        { Stop("캐릭터가 변경되어 중단했습니다.", "character_changed"); return; }

        switch (Stage)
        {
            case RefreshStage.Queued:
                if (host.QueryBusy) { Stop("진행 중인 조회가 끝난 뒤 다시 눌러 주세요.", "query_busy"); return; }
                if (host.EditorVisible) { Stop("열려 있는 모집 조건 창을 닫고 다시 눌러 주세요.", "editor_already_open"); return; }
                self = host.SelfId;
                BeginQuery(now, false);
                break;

            case RefreshStage.ReadingOwn:
                if (now - stageTick > 15000)
                { Stop(ownWaitReason.Length > 0 ? ownWaitReason : "내 모집글 조회 응답을 확인하지 못했습니다.", "initial_own_ready_timeout"); return; }
                if (host.QueryBusy) return;
                if (!host.QuerySucceeded || host.DetailRevision <= queryRevision)
                { Stop("새 모집글 조회 응답을 확인하지 못했습니다. 내 모집 상태를 확인해 주세요.", "initial_query_failed"); return; }
                // PostSetup/PostRefresh can arrive before the visible text, native
                // detail and owner classification agree. Keep reading that same
                // response until it is verified; do not reopen or requery it.
                if (!host.TryGetOwn(out var own, out var ownError))
                { WaitForOwn(ownError, false); return; }
                if (own.SelfId != self) { Stop("모집자가 변경되어 중단했습니다.", "owner_changed"); return; }
                if (boundSource != null && !ListingPendingChanges.SameSource(boundSource, own))
                { Stop("브라우저에서 확인한 뒤 모집글 또는 참가자가 바뀌었습니다. 새 모집글에서 다시 확인해 주세요.", "web_source_changed"); return; }
                if (JobEdit != null && !JobEdit.MatchesOriginal(own))
                { Stop("편집 중 참가자 또는 모집 조건이 바뀌었습니다. 새 모집글에서 다시 선택해 주세요.", "job_edit_stale"); return; }
                if (DescriptionEdit != null && !DescriptionEdit.MatchesOriginal(own))
                { Stop("편집 중 모집글 또는 참가자가 바뀌었습니다. 다시 조회한 뒤 소개말을 수정해 주세요.", "description_edit_stale"); return; }
                if (ObjectiveEdit != null && !ObjectiveEdit.MatchesOriginal(own))
                { Stop("편집 중 모집글 또는 참가자가 바뀌었습니다. 다시 조회한 뒤 목적을 수정해 주세요.", "objective_edit_stale"); return; }
                if (CompletionEdit != null && !CompletionEdit.MatchesOriginal(own))
                { Stop("편집 중 모집글 또는 참가자가 바뀌었습니다. 다시 조회한 뒤 완료조건을 수정해 주세요.", "completion_edit_stale"); return; }
                if (RuleEdit != null && !RuleEdit.MatchesOriginal(own))
                { Stop("편집 중 모집글 또는 참가자가 바뀌었습니다. 다시 조회한 뒤 규칙을 수정해 주세요.", "rule_edit_stale"); return; }
                if (ItemLevelEdit != null && !ItemLevelEdit.MatchesOriginal(own))
                { Stop("편집 중 모집글 또는 참가자가 바뀌었습니다. 다시 조회한 뒤 평균 아이템 레벨을 수정해 주세요.", "item_level_edit_stale"); return; }
                if (BeginnerEdit != null && !BeginnerEdit.MatchesOriginal(own))
                { Stop("편집 중 모집글 또는 참가자가 바뀌었습니다. 다시 조회한 뒤 초보자 환영 설정을 수정해 주세요.", "beginner_edit_stale"); return; }
                if (UniqueJobsEdit != null && !UniqueJobsEdit.MatchesOriginal(own))
                { Stop("편집 중 모집글 또는 참가자가 바뀌었습니다. 다시 조회한 뒤 잡 중복 없음 설정을 수정해 주세요.", "unique_jobs_edit_stale"); return; }
                if (PrivateEdit != null && !PrivateEdit.MatchesOriginal(own))
                { Stop("편집 중 모집글 또는 참가자가 바뀌었습니다. 다시 조회한 뒤 비공개 설정을 수정해 주세요.", "private_edit_stale"); return; }
                if (host.EditorVisible) { Stop("모집 조건 창이 따로 열려 있어 중단했습니다.", "editor_opened_elsewhere"); return; }
                target = own;
                ownWaitReason = "";
                editorRevision = host.EditorRevision;
                editorClosedRevision = host.EditorClosedRevision;
                SetStage(RefreshStage.OpeningEditor, now, "기존 모집 조건 여는 중");
                editCalls++;
                if (!host.OpenEditor(own, out var openError)) Stop(openError, "open_editor_failed");
                break;

            case RefreshStage.OpeningEditor:
                if (host.EditorClosedRevision > editorClosedRevision)
                { Stop("모집 조건 창이 닫혀 중단했습니다.", "editor_closed_before_submit"); return; }
                var editorReady = host.EditorReady;
                editorMode = host.EditorActionMode;
                if (now - stageTick > 7000)
                {
                    Stop(editorMode == RecruitmentEditorMode.Create ?
                        "‘파티 만들기’ 화면이어서 갱신을 실행하지 않았습니다." :
                        "‘모집 내용 갱신’ 버튼이 준비되지 않았습니다.", "editor_refresh_mode_timeout");
                    return;
                }
                if (now - stageTick < 500 || !editorReady || host.EditorRevision <= editorRevision) return;
                if (editorMode != RecruitmentEditorMode.Refresh)
                { result = "waiting_refresh_mode"; Status = "‘모집 내용 갱신’ 버튼 확인 중"; return; }
                if (JobEdit != null)
                {
                    SetStage(RefreshStage.EditingSlot, now, "허용 직업 변경 중");
                    if (!host.BeginSlotEdit(target!, JobEdit, out var editError)) Stop(editError, "job_edit_open_failed");
                }
                else if (DescriptionEdit != null)
                {
                    SetStage(RefreshStage.EditingDescription, now, "모집 소개말 반영 중");
                    if (!host.BeginDescriptionEdit(target!, DescriptionEdit, out var descriptionError))
                        Stop(descriptionError, "description_edit_start_failed");
                }
                else if (ObjectiveEdit != null)
                {
                    SetStage(RefreshStage.EditingObjective, now, "모집 목적 반영 중");
                    if (!host.BeginObjectiveEdit(target!, ObjectiveEdit, out var objectiveError))
                        Stop(objectiveError, "objective_edit_start_failed");
                }
                else if (CompletionEdit != null)
                {
                    SetStage(RefreshStage.EditingCompletion, now, "모집 완료조건 반영 중");
                    if (!host.BeginCompletionEdit(target!, CompletionEdit, out var completionError))
                        Stop(completionError, "completion_edit_start_failed");
                }
                else if (RuleEdit != null)
                {
                    SetStage(RefreshStage.EditingRule, now, $"{ListingRules.Title(RuleEdit.Kind)} 반영 중");
                    if (!host.BeginRuleEdit(target!, RuleEdit, out var ruleError)) Stop(ruleError, "rule_edit_start_failed");
                }
                else if (ItemLevelEdit != null)
                {
                    SetStage(RefreshStage.EditingItemLevel, now, "모집 평균 아이템 레벨 반영 중");
                    if (!host.BeginItemLevelEdit(target!, ItemLevelEdit, out var itemLevelError))
                        Stop(itemLevelError, "item_level_edit_start_failed");
                }
                else if (BeginnerEdit != null)
                {
                    SetStage(RefreshStage.EditingBeginner, now, "초행 및 초보자 환영 설정 중");
                    if (!host.BeginBeginnerEdit(target!, BeginnerEdit, out var beginnerError))
                        Stop(beginnerError, "beginner_edit_start_failed");
                }
                else if (UniqueJobsEdit != null)
                {
                    SetStage(RefreshStage.EditingUniqueJobs, now, "잡 중복 없음 설정 중");
                    if (!host.BeginUniqueJobsEdit(target!, UniqueJobsEdit, out var uniqueJobsError))
                        Stop(uniqueJobsError, "unique_jobs_edit_start_failed");
                }
                else if (PrivateEdit != null)
                {
                    SetStage(RefreshStage.EditingPrivate, now, "비공개 및 비밀번호 설정 중");
                    if (!host.BeginPrivateEdit(target!, PrivateEdit, out var privateError))
                        Stop(privateError, "private_edit_start_failed");
                }
                else Submit(now);
                break;

            case RefreshStage.EditingSlot:
                if (host.EditorClosedRevision != editorClosedRevision || !host.EditorVisible)
                { Stop("모집 수정 창이 닫혀 직업 수정을 중단했습니다.", "job_editor_closed"); return; }
                if (now - stageTick > 45000)
                { Stop("직업 변경 결과를 확인하지 못했습니다.", "job_edit_timeout"); return; }
                var progress = host.UpdateSlotEdit(out var expected, out var jobError);
                if (progress == SlotEditProgress.Failed) { Stop(jobError, "job_edit_failed"); return; }
                if (progress == SlotEditProgress.Waiting) return;
                if (!JobEdit!.MatchesResult(target!, expected))
                { Stop("요청한 자리의 직업 변경 결과가 다릅니다.", "job_edit_result_mismatch"); return; }
                target = expected;
                Submit(now);
                break;

            case RefreshStage.EditingDescription:
                if (host.EditorClosedRevision != editorClosedRevision || !host.EditorVisible)
                { Stop("모집 수정 창이 닫혀 소개말 수정을 중단했습니다.", "description_editor_closed"); return; }
                if (now - stageTick > 8000)
                { Stop("소개말이 게임 수정창에 반영됐는지 확인하지 못했습니다.", "description_edit_timeout"); return; }
                var descriptionProgress = host.UpdateDescriptionEdit(out var descriptionExpected, out var descriptionReadError);
                if (descriptionProgress == SlotEditProgress.Failed) { Stop(descriptionReadError, "description_edit_failed"); return; }
                if (descriptionProgress == SlotEditProgress.Waiting) return;
                if (!DescriptionEdit!.MatchesResult(target!, descriptionExpected))
                { Stop("소개말 또는 다른 모집 조건이 요청한 내용과 다릅니다.", "description_edit_result_mismatch"); return; }
                target = descriptionExpected;
                Submit(now);
                break;

            case RefreshStage.EditingObjective:
                if (host.EditorClosedRevision != editorClosedRevision || !host.EditorVisible)
                { Stop("모집 수정 창이 닫혀 목적 수정을 중단했습니다.", "objective_editor_closed"); return; }
                if (now - stageTick > 8000)
                { Stop("목적이 게임 수정창에 반영됐는지 확인하지 못했습니다.", "objective_edit_timeout"); return; }
                var objectiveProgress = host.UpdateObjectiveEdit(out var objectiveExpected, out var objectiveReadError);
                if (objectiveProgress == SlotEditProgress.Failed) { Stop(objectiveReadError, "objective_edit_failed"); return; }
                if (objectiveProgress == SlotEditProgress.Waiting) return;
                if (!ObjectiveEdit!.MatchesResult(target!, objectiveExpected))
                { Stop("목적 또는 다른 모집 조건이 요청한 내용과 다릅니다.", "objective_edit_result_mismatch"); return; }
                target = objectiveExpected;
                Submit(now);
                break;

            case RefreshStage.EditingCompletion:
                if (host.EditorClosedRevision != editorClosedRevision || !host.EditorVisible)
                { Stop("모집 수정 창이 닫혀 완료조건 수정을 중단했습니다.", "completion_editor_closed"); return; }
                if (now - stageTick > 8000)
                { Stop("완료조건이 게임 수정창에 반영됐는지 확인하지 못했습니다.", "completion_edit_timeout"); return; }
                var completionProgress = host.UpdateCompletionEdit(out var completionExpected, out var completionReadError);
                if (completionProgress == SlotEditProgress.Failed) { Stop(completionReadError, "completion_edit_failed"); return; }
                if (completionProgress == SlotEditProgress.Waiting) return;
                if (!CompletionEdit!.MatchesResult(target!, completionExpected))
                { Stop("완료조건 또는 다른 모집 조건이 요청한 내용과 다릅니다.", "completion_edit_result_mismatch"); return; }
                target = completionExpected;
                Submit(now);
                break;

            case RefreshStage.EditingRule:
                if (host.EditorClosedRevision != editorClosedRevision || !host.EditorVisible)
                { Stop("모집 수정 창이 닫혀 규칙 수정을 중단했습니다.", "rule_editor_closed"); return; }
                if (now - stageTick > 8000)
                { Stop("모집 규칙이 게임 수정창에 반영됐는지 확인하지 못했습니다.", "rule_edit_timeout"); return; }
                var ruleProgress = host.UpdateRuleEdit(out var ruleExpected, out var ruleReadError);
                if (ruleProgress == SlotEditProgress.Failed) { Stop(ruleReadError, "rule_edit_failed"); return; }
                if (ruleProgress == SlotEditProgress.Waiting) return;
                if (!RuleEdit!.MatchesResult(target!, ruleExpected))
                { Stop("모집 규칙 또는 다른 모집 조건이 요청한 내용과 다릅니다.", "rule_edit_result_mismatch"); return; }
                target = ruleExpected;
                Submit(now);
                break;

            case RefreshStage.EditingItemLevel:
                if (host.EditorClosedRevision != editorClosedRevision || !host.EditorVisible)
                { Stop("모집 수정 창이 닫혀 평균 아이템 레벨 수정을 중단했습니다.", "item_level_editor_closed"); return; }
                if (now - stageTick > 8000)
                { Stop("평균 아이템 레벨이 게임 수정창에 반영됐는지 확인하지 못했습니다.", "item_level_edit_timeout"); return; }
                var itemLevelProgress = host.UpdateItemLevelEdit(out var itemLevelExpected, out var itemLevelReadError);
                if (itemLevelProgress == SlotEditProgress.Failed) { Stop(itemLevelReadError, "item_level_edit_failed"); return; }
                if (itemLevelProgress == SlotEditProgress.Waiting) return;
                if (!ItemLevelEdit!.MatchesResult(target!, itemLevelExpected))
                { Stop("평균 아이템 레벨 또는 다른 모집 조건이 요청한 내용과 다릅니다.", "item_level_edit_result_mismatch"); return; }
                target = itemLevelExpected;
                Submit(now);
                break;

            case RefreshStage.EditingBeginner:
                if (host.EditorClosedRevision != editorClosedRevision || !host.EditorVisible)
                { Stop("모집 수정 창이 닫혀 초보자 환영 수정을 중단했습니다.", "beginner_editor_closed"); return; }
                if (now - stageTick > 8000)
                { Stop("초보자 환영 설정이 게임 수정창에 반영됐는지 확인하지 못했습니다.", "beginner_edit_timeout"); return; }
                var beginnerProgress = host.UpdateBeginnerEdit(out var beginnerExpected, out var beginnerReadError);
                if (beginnerProgress == SlotEditProgress.Failed) { Stop(beginnerReadError, "beginner_edit_failed"); return; }
                if (beginnerProgress == SlotEditProgress.Waiting) return;
                if (!BeginnerEdit!.MatchesResult(target!, beginnerExpected))
                { Stop("초보자 환영 또는 다른 모집 조건이 요청한 내용과 다릅니다.", "beginner_edit_result_mismatch"); return; }
                target = beginnerExpected;
                Submit(now);
                break;

            case RefreshStage.EditingUniqueJobs:
                if (host.EditorClosedRevision != editorClosedRevision || !host.EditorVisible)
                { Stop("모집 수정 창이 닫혀 잡 중복 없음 수정을 중단했습니다.", "unique_jobs_editor_closed"); return; }
                if (now - stageTick > 8000)
                { Stop("잡 중복 없음 설정이 게임 수정창에 반영됐는지 확인하지 못했습니다.", "unique_jobs_edit_timeout"); return; }
                var uniqueJobsProgress = host.UpdateUniqueJobsEdit(out var uniqueJobsExpected, out var uniqueJobsReadError);
                if (uniqueJobsProgress == SlotEditProgress.Failed) { Stop(uniqueJobsReadError, "unique_jobs_edit_failed"); return; }
                if (uniqueJobsProgress == SlotEditProgress.Waiting) return;
                if (!UniqueJobsEdit!.MatchesResult(target!, uniqueJobsExpected))
                { Stop("잡 중복 없음 또는 다른 모집 조건이 요청한 내용과 다릅니다.", "unique_jobs_edit_result_mismatch"); return; }
                target = uniqueJobsExpected;
                Submit(now);
                break;

            case RefreshStage.EditingPrivate:
                if (host.EditorClosedRevision != editorClosedRevision || !host.EditorVisible)
                { Stop("모집 수정 창이 닫혀 비공개 및 비밀번호 수정을 중단했습니다.", "private_editor_closed"); return; }
                if (now - stageTick > 8000)
                { Stop("비공개 및 비밀번호 설정이 게임 수정창에 반영됐는지 확인하지 못했습니다.", "private_edit_timeout"); return; }
                var privateProgress = host.UpdatePrivateEdit(out var privateExpected, out var privateReadError);
                if (privateProgress == SlotEditProgress.Failed) { Stop(privateReadError, "private_edit_failed"); return; }
                if (privateProgress == SlotEditProgress.Waiting) return;
                if (!PrivateEdit!.MatchesResult(target!, privateExpected))
                { Stop("비공개 및 비밀번호 또는 다른 모집 조건이 요청한 내용과 다릅니다.", "private_edit_result_mismatch"); return; }
                target = privateExpected;
                Submit(now);
                break;

            case RefreshStage.WaitingResult:
                if (now - stageTick > 10000)
                { Stop("등록 화면이 닫히지 않았습니다. 게임에 표시된 메시지를 확인해 주세요.", "submit_response_timeout"); return; }
                if (host.EditorVisible || now - stageTick < 1500) return;
                BeginQuery(now, true);
                break;

            case RefreshStage.Verifying:
                if (now - stageTick > 15000)
                { Stop(ownWaitReason.Length > 0 ? ownWaitReason : "재등록 후 조회 응답을 확인하지 못했습니다.", "verify_own_ready_timeout"); return; }
                if (host.QueryBusy) return;
                if (!host.QuerySucceeded || host.DetailRevision <= queryRevision)
                { Stop("재등록 후 새 조회 응답을 확인하지 못했습니다.", "verify_query_failed"); return; }
                if (!host.TryGetOwn(out var after, out var verifyError))
                { WaitForOwn(verifyError, true); return; }
                ownWaitReason = "";
                afterSeconds = after.RemainingSeconds;
                if (DescriptionEdit != null)
                    descriptionVerification = $"descriptionResultCheck=read; " +
                        $"descriptionResultTextAgrees={ListingIdentity.SameText(target!.Description, after.Description)}; " +
                        $"descriptionResultExpectedChars={target.Description.Length}; descriptionResultActualChars={after.Description.Length}; " +
                        $"descriptionResultCriteriaAgree={RefreshEvidence.SameRecruitment(target with { Description = after.Description }, after)}; " +
                        $"descriptionResultSlotsAgree={ListingJobEdit.SameSlots(target.Slots, after.Slots)}";
                if (ObjectiveEdit != null)
                    objectiveVerification = $"objectiveResultCheck=read; " +
                        $"objectiveResultExpected={ObjectiveEdit.Value}; objectiveResultActual={ListingObjectives.Read(after.Criteria)?.ToString() ?? "unknown"}; " +
                        $"objectiveResultSlotsAgree={ListingJobEdit.SameSlots(target!.Slots, after.Slots)}; " +
                        $"objectiveResultContentsAgree={ObjectiveEdit.MatchesResult(target, after)}";
                if (CompletionEdit != null)
                    completionVerification = $"completionResultCheck=read; " +
                        $"completionResultExpected={CompletionEdit.Value}; completionResultActual={ListingCompletions.Read(after.Criteria)?.ToString() ?? "unknown"}; " +
                        $"completionResultSlotsAgree={ListingJobEdit.SameSlots(target!.Slots, after.Slots)}; " +
                        $"completionResultContentsAgree={CompletionEdit.MatchesResult(target, after)}";
                if (RuleEdit != null)
                    ruleVerification = $"ruleResultCheck=read; ruleResultKind={RuleEdit.Kind}; " +
                        $"ruleResultExpected={RuleEdit.Value}; ruleResultActual={ListingRules.Read(after.Criteria, RuleEdit.Kind)?.ToString() ?? "unknown"}; " +
                        $"ruleResultSlotsAgree={ListingJobEdit.SameSlots(target!.Slots, after.Slots)}; " +
                        $"ruleResultContentsAgree={RuleEdit.MatchesResult(target, after)}";
                if (ItemLevelEdit != null)
                    itemLevelVerification = $"itemLevelResultCheck=read; " +
                        $"itemLevelResultExpected={ItemLevelEdit.Value}; itemLevelResultActual={ListingItemLevels.Read(after.ExtraCriteria)?.ToString() ?? "unknown"}; " +
                        $"itemLevelResultSlotsAgree={ListingJobEdit.SameSlots(target!.Slots, after.Slots)}; " +
                        $"itemLevelResultContentsAgree={ItemLevelEdit.MatchesResult(target, after)}";
                if (BeginnerEdit != null)
                    beginnerVerification = $"beginnerResultCheck=read; " +
                        $"beginnerResultExpected={BeginnerEdit.Enabled}; beginnerResultActual={ListingBeginners.Read(after.Criteria)?.ToString() ?? "unknown"}; " +
                        $"beginnerResultSlotsAgree={ListingJobEdit.SameSlots(target!.Slots, after.Slots)}; " +
                        $"beginnerResultContentsAgree={BeginnerEdit.MatchesResult(target, after)}";
                if (UniqueJobsEdit != null)
                    uniqueJobsVerification = $"uniqueJobsResultCheck=read; " +
                        $"uniqueJobsResultExpected={UniqueJobsEdit.Enabled}; uniqueJobsResultActual={ListingUniqueJobs.Read(after.ExtraCriteria)?.ToString() ?? "unknown"}; " +
                        $"uniqueJobsResultSlotsAgree={ListingJobEdit.SameSlots(target!.Slots, after.Slots)}; " +
                        $"uniqueJobsResultContentsAgree={UniqueJobsEdit.MatchesResult(target, after)}";
                if (PrivateEdit != null)
                    privateVerification = $"privateResultCheck=read; privatePasswordServerCheck=unavailable; " +
                        $"privateResultExpected={PrivateEdit.Enabled}; privateResultActual={ListingPrivate.Read(after.ExtraCriteria)?.ToString() ?? "unknown"}; " +
                        $"privateResultSlotsAgree={ListingJobEdit.SameSlots(target!.Slots, after.Slots)}; " +
                        $"privateResultContentsAgree={PrivateEdit.MatchesResult(target, after)}";
                var contentsAgree = JobEdit != null ? JobEdit.MatchesResult(target!, after) :
                    DescriptionEdit != null ? DescriptionEdit.MatchesResult(target!, after) :
                    ObjectiveEdit != null ? ObjectiveEdit.MatchesResult(target!, after) :
                    CompletionEdit != null ? CompletionEdit.MatchesResult(target!, after) :
                    RuleEdit != null ? RuleEdit.MatchesResult(target!, after) :
                    ItemLevelEdit != null ? ItemLevelEdit.MatchesResult(target!, after) :
                    BeginnerEdit != null ? BeginnerEdit.MatchesResult(target!, after) :
                    UniqueJobsEdit != null ? UniqueJobsEdit.MatchesResult(target!, after) :
                    PrivateEdit != null ? PrivateEdit.MatchesResult(target!, after) : RefreshEvidence.SameRecruitment(target!, after);
                if (!contentsAgree)
                { Stop("등록 후 모집 내용 또는 조건이 다르게 보입니다. 게임에서 확인해 주세요.", "verify_contents_changed"); return; }
                var confirmed = after.ListingId != target!.ListingId ||
                    after.RemainingSeconds is { } seconds && target.RemainingSeconds is { } before && seconds > before;
                result = confirmed ? "confirmed_by_new_listing_or_timer" : "own_listing_seen_timer_unconfirmed";
                SetStage(confirmed ? RefreshStage.Completed : RefreshStage.Unconfirmed, now,
                    confirmed ? (JobEdit != null ? "직업 변경 및 갱신 확인 — 내 모집글을 다시 조회했습니다." :
                        PrivateEdit != null ? "비공개 설정 및 갱신 확인 — 내 모집글을 다시 조회했습니다." :
                        UniqueJobsEdit != null ? "잡 중복 없음 변경 및 갱신 확인 — 내 모집글을 다시 조회했습니다." :
                        BeginnerEdit != null ? "초보자 환영 변경 및 갱신 확인 — 내 모집글을 다시 조회했습니다." :
                        CompletionEdit != null ? "완료조건 변경 및 갱신 확인 — 내 모집글을 다시 조회했습니다." :
                        RuleEdit != null ? $"{ListingRules.Title(RuleEdit.Kind)} 변경 및 갱신 확인 — 내 모집글을 다시 조회했습니다." :
                        ItemLevelEdit != null ? "평균 아이템 레벨 변경 및 갱신 확인 — 내 모집글을 다시 조회했습니다." :
                        ObjectiveEdit != null ? "목적 변경 및 갱신 확인 — 내 모집글을 다시 조회했습니다." :
                        DescriptionEdit != null ? "소개말 변경 및 갱신 확인 — 내 모집글을 다시 조회했습니다." : "갱신 확인 — 재등록 후 내 모집글을 다시 조회했습니다.") :
                    PrivateEdit != null ? "비공개 설정이 반영된 것을 확인했습니다. 남은 시간 초기화 여부는 게임에서 확인해 주세요." :
                    UniqueJobsEdit != null ? "잡 중복 없음 설정이 반영된 것을 확인했습니다. 남은 시간 초기화 여부는 게임에서 확인해 주세요." :
                    BeginnerEdit != null ? "초보자 환영 설정이 반영된 것을 확인했습니다. 남은 시간 초기화 여부는 게임에서 확인해 주세요." :
                    CompletionEdit != null ? "수정한 완료조건이 등록된 것을 확인했습니다. 남은 시간 초기화 여부는 게임에서 확인해 주세요." :
                    RuleEdit != null ? $"{ListingRules.Title(RuleEdit.Kind)} 설정이 반영된 것을 확인했습니다. 남은 시간 초기화 여부는 게임에서 확인해 주세요." :
                    ItemLevelEdit != null ? "수정한 평균 아이템 레벨이 등록된 것을 확인했습니다. 남은 시간 초기화 여부는 게임에서 확인해 주세요." :
                    ObjectiveEdit != null ? "수정한 목적이 등록된 것을 확인했습니다. 남은 시간 초기화 여부는 게임에서 확인해 주세요." :
                    DescriptionEdit != null ? "수정한 소개말이 등록된 것을 확인했습니다. 남은 시간 초기화 여부는 게임에서 확인해 주세요." :
                    "재등록 요청 후 내 모집글을 확인했습니다. 남은 시간 초기화 여부는 게임에서 확인해 주세요.");
                break;
        }
    }

    internal void Cancel()
    {
        if (batch != null) { batch.Cancel(); return; }
        if (Busy) Stop("사용자가 진행을 중단했습니다.", "cancelled");
    }

    internal void Fail(string message)
    { if (batch != null) batch.Fail(message); else Stop("갱신 오류: " + message, "exception"); }

    private void Submit(long now)
    {
        SetStage(RefreshStage.WaitingResult, now, "재등록 응답 기다리는 중");
        // Advance before native dispatch; failures never cause an automatic retry.
        submitCalls++;
        cooldownUntil = now + 5000;
        if (!host.Submit(target!, DescriptionEdit?.Value, out var error)) Stop(error, "submit_not_completed");
    }

    private void BeginQuery(long now, bool verify)
    {
        ownWaitReason = "";
        queryRevision = host.DetailRevision;
        SetStage(verify ? RefreshStage.Verifying : RefreshStage.ReadingOwn, now,
            verify ? "재등록한 모집글 확인 중" : "내 모집글과 작성자 확인 중");
        host.QueryOwn();
    }

    private void WaitForOwn(string reason, bool verify)
    {
        ownWaitChecks++;
        ownWaitReason = reason;
        result = verify ? "waiting_verified_result" : "waiting_own_details";
        Status = verify ? "갱신 결과의 내용과 작성자가 확인될 때까지 기다리는 중" :
            "내 모집글의 내용과 작성자가 확인될 때까지 기다리는 중";
    }

    private void Stop(string message, string code)
    {
        host.CancelQuery();
        result = code;
        Stage = submitCalls > 0 ? RefreshStage.Unconfirmed : RefreshStage.Stopped;
        Status = message + (editCalls > 0 ?
            " 자동 진행은 중단했습니다. 편집 창이 열려 있다면 모집은 중지된 상태이므로, 게임에서 ‘모집 내용 갱신’을 눌러 재개해 주세요." : "");
    }

    private void SetStage(RefreshStage stage, long now, string message)
    { Stage = stage; stageTick = now; Status = message; }
}

internal static class RefreshEvidence
{
    internal static bool SameRecruitment(RefreshTarget a, RefreshTarget b) =>
        a.SelfId == b.SelfId && a.DutyId == b.DutyId && a.TotalSlots == b.TotalSlots &&
        ListingRoleNormalization.SameCriteria(a, b) && a.ExtraCriteria == b.ExtraCriteria &&
        ListingIdentity.SameText(a.Description, b.Description);

    internal static RecruitmentEditorMode ClassifyEditorAction(string text) =>
        string.Concat(text.Where(c => !char.IsWhiteSpace(c))) switch {
            "모집내용갱신" => RecruitmentEditorMode.Refresh,
            "파티만들기" => RecruitmentEditorMode.Create,
            _ => RecruitmentEditorMode.Unknown
        };

    // Read only the game's countdown text. An unreadable timer is never replaced
    // with a fabricated 60-minute timer or used to claim a successful refresh.
    internal static int? ParseSeconds(string text)
    {
        var match = Regex.Match(text, @"(?<!\d)(\d{1,3}):([0-5]\d)(?::([0-5]\d))?(?![\d:])");
        if (match.Success)
        {
            var first = int.Parse(match.Groups[1].Value);
            var second = int.Parse(match.Groups[2].Value);
            var seconds = match.Groups[3].Success ? first * 3600 + second * 60 + int.Parse(match.Groups[3].Value) : first * 60 + second;
            return seconds <= 3600 ? seconds : null;
        }
        if (text.Contains("시간")) return null;
        match = Regex.Match(text, @"(?<!\d)(\d{1,2})\s*분(?:\s*([0-5]?\d)\s*초)?");
        if (!match.Success) return null;
        var total = int.Parse(match.Groups[1].Value) * 60 + (match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0);
        return total <= 3600 ? total : null;
    }
}
