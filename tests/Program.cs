using PartyViewKR;

var passed = 0;
void Check(bool value, string label) { if (!value) throw new Exception(label); }
void Test(string name, Action body) { body(); passed++; Console.WriteLine("PASS " + name); }
(FakeHost h, ListingRefresh flow) StartToEditor()
{
    var h = new FakeHost(); var flow = new ListingRefresh(h);
    flow.Request(1000); flow.Update(1000); h.FinishQuery(); flow.Update(1500);
    Check(flow.Stage == RefreshStage.OpeningEditor && h.EditCalls == 1, "editor entry");
    return (h, flow);
}
(FakeHost h, ListingRefresh flow) StartToVerify()
{
    var (h, flow) = StartToEditor(); flow.Update(2000); flow.Update(3500);
    Check(flow.Stage == RefreshStage.Verifying && h.SubmitCalls == 1, "verification entry");
    return (h, flow);
}

Test("detail event before visible own data waits instead of stopping", () => {
    var h = new FakeHost { OwnVerified = false }; var f = new ListingRefresh(h);
    f.Request(1000); f.Update(1000); h.FinishQuery(); f.Update(1500);
    Check(f.Stage == RefreshStage.ReadingOwn && h.EditCalls == 0, "stopped before own details became ready");
    f.Update(2000); Check(f.Busy && h.QueryCalls == 1, "waiting reissued query or stopped");
    h.OwnVerified = true; f.Update(2500);
    Check(f.Stage == RefreshStage.OpeningEditor && h.EditCalls == 1, "did not continue after own data became ready");
});
Test("idle never acts", () => {
    var h = new FakeHost(); var f = new ListingRefresh(h); f.Update(10000);
    Check(h.QueryCalls + h.EditCalls + h.SubmitCalls == 0, "unexpected action");
});
Test("cancel while waiting for own data never continues later", () => {
    var h = new FakeHost { OwnVerified = false }; var f = new ListingRefresh(h);
    f.Request(1000); f.Update(1000); h.FinishQuery(); f.Update(1500);
    f.Cancel(); h.OwnVerified = true; f.Update(2500);
    Check(!f.Busy && h.EditCalls == 0 && h.SubmitCalls == 0, "continued after cancellation");
});
Test("new-party editor never submits", () => {
    var (h, f) = StartToEditor(); h.EditorActionMode = RecruitmentEditorMode.Create;
    f.Update(2000); Check(f.Busy && h.SubmitCalls == 0, "submitted create action");
    f.Update(9000);
    Check(!f.Busy && h.SubmitCalls == 0 && f.Status.Contains("파티 만들기"), "create mode did not stop");
});
Test("editor label can finish loading before one refresh", () => {
    var (h, f) = StartToEditor(); h.EditorActionMode = RecruitmentEditorMode.Unknown;
    f.Update(2000); Check(f.Busy && h.SubmitCalls == 0, "unknown action was submitted");
    h.EditorActionMode = RecruitmentEditorMode.Refresh; f.Update(2500); f.Update(3000);
    Check(h.SubmitCalls == 1 && h.EditCalls == 1, "refresh did not resume once label loaded");
});
Test("editor pause does not require an active own listing", () => {
    var (h, f) = StartToEditor(); h.OwnVerified = false;
    f.Update(2000);
    Check(f.Stage == RefreshStage.WaitingResult && h.SubmitCalls == 1, "expected recruitment pause blocked refresh");
});
Test("stopping during edit keeps the window and explains how to resume", () => {
    var (h, f) = StartToEditor(); f.Cancel(); f.Update(3000);
    Check(h.EditorVisible && h.SubmitCalls == 0, "cancel changed or submitted editor");
    Check(f.Status.Contains("모집은 중지된 상태") && f.Status.Contains("모집 내용 갱신"), "missing manual resume guidance");
});
Test("post-refresh owner data may become ready after its event", () => {
    var (h, f) = StartToVerify(); h.OwnVerified = false; h.FinishQuery(); f.Update(4000);
    Check(f.Stage == RefreshStage.Verifying && h.QueryCalls == 2, "verification stopped too early");
    h.OwnVerified = true; h.Target = h.Target with { RemainingSeconds = 3598 }; f.Update(4500);
    Check(f.Stage == RefreshStage.Completed && h.SubmitCalls == 1, "verification did not finish without resubmitting");
});
Test("refresh action label is distinct from create and unknown actions", () => {
    Check(RefreshEvidence.ClassifyEditorAction("모집 내용 갱신") == RecruitmentEditorMode.Refresh, "refresh label");
    Check(RefreshEvidence.ClassifyEditorAction(" 모집\n내용  갱신 ") == RecruitmentEditorMode.Refresh, "label whitespace");
    Check(RefreshEvidence.ClassifyEditorAction("파티 만들기") == RecruitmentEditorMode.Create, "create label");
    Check(RefreshEvidence.ClassifyEditorAction("") == RecruitmentEditorMode.Unknown, "empty label");
    Check(RefreshEvidence.ClassifyEditorAction("모집 내용 갱신 취소") == RecruitmentEditorMode.Unknown, "partial label match");
});
Test("one click, one submit, fresh timer confirms", () => {
    var (h, f) = StartToEditor();
    f.Request(1600); f.Update(1700); Check(h.SubmitCalls == 0, "did not wait for editor");
    f.Update(2000); f.Request(2100); f.Update(2200); f.Update(3500);
    h.Target = h.Target with { RemainingSeconds = 3598 }; h.FinishQuery(); f.Update(4000); f.Update(9000);
    Check(f.Stage == RefreshStage.Completed, "not confirmed");
    Check(h.QueryCalls == 2 && h.EditCalls == 1 && h.SubmitCalls == 1, "duplicate dispatch");
    Check(h.SubmittedDescriptionArgument == null, "ordinary refresh selected the description callback");
});
Test("old response without a new revision cannot open editor", () => {
    var h = new FakeHost(); var f = new ListingRefresh(h); f.Request(1000); f.Update(1000);
    h.QueryBusy = false; h.QuerySucceeded = true; f.Update(1500);
    Check(!f.Busy && h.EditCalls == 0, "stale response accepted");
});
Test("other or unverified owner cannot open editor", () => {
    var h = new FakeHost { OwnVerified = false }; var f = new ListingRefresh(h);
    f.Request(1000); f.Update(1000); h.FinishQuery(); f.Update(1500);
    Check(f.Busy && h.EditCalls == 0, "unverified owner accepted");
    f.Update(16001);
    Check(!f.Busy && h.EditCalls == 0, "unverified owner did not time out");
});
Test("preexisting editor is never submitted", () => {
    var h = new FakeHost { EditorVisible = true }; var f = new ListingRefresh(h); f.Request(1000); f.Update(1000);
    Check(!f.Busy && h.QueryCalls == 0 && h.SubmitCalls == 0, "preexisting draft touched");
});
Test("character switch aborts", () => {
    var (h, f) = StartToEditor(); h.SelfId = 99; f.Update(2000);
    Check(!f.Busy && h.SubmitCalls == 0, "character changed but submitted");
});
Test("cancel prevents subsequent registration", () => {
    var (h, f) = StartToEditor(); f.Cancel(); f.Update(2000); f.Update(10000);
    Check(!f.Busy && h.SubmitCalls == 0, "cancel still submitted");
});
Test("closing and reopening editor aborts", () => {
    var (h, f) = StartToEditor(); h.EditorClosedRevision++; h.EditorRevision++; f.Update(2000);
    Check(!f.Busy && h.SubmitCalls == 0, "closed editor accepted");
});
Test("editor must have a fresh lifecycle event", () => {
    var (h, f) = StartToEditor(); h.EditorRevision = 0; f.Update(2000); f.Update(9000);
    Check(!f.Busy && h.SubmitCalls == 0, "stale editor submitted");
});
Test("zone transition blocks remaining actions", () => {
    var (h, f) = StartToEditor(); h.BlockReason = "지역 이동"; f.Update(2000);
    Check(!f.Busy && h.SubmitCalls == 0, "transition submitted");
});
Test("validation failure never retries", () => {
    var (h, f) = StartToEditor(); h.SubmitAllowed = false; f.Update(2000); f.Update(2500); f.Update(20000);
    Check(!f.Busy && h.SubmitCalls == 1, "failed submit retried");
});
Test("throwing native call never retries", () => {
    var (h, f) = StartToEditor(); h.ThrowOnSubmit = true;
    try { f.Update(2000); } catch (InvalidOperationException ex) { f.Fail(ex.Message); }
    f.Update(3000); f.Update(30000);
    Check(!f.Busy && h.SubmitCalls == 1, "exception retried");
});
Test("server timeout is unconfirmed without retry", () => {
    var (h, f) = StartToEditor(); h.KeepEditorOpen = true; f.Update(2000); f.Update(13000); f.Update(20000);
    Check(f.Stage == RefreshStage.Unconfirmed && h.SubmitCalls == 1, "timeout resubmitted");
});
Test("unchanged timer is not success", () => {
    var (h, f) = StartToVerify(); h.FinishQuery(); f.Update(4000);
    Check(f.Stage == RefreshStage.Unconfirmed, "unchanged timer claimed success");
});
Test("new listing identity confirms after fresh response", () => {
    var (h, f) = StartToVerify(); h.Target = h.Target with { ListingId = 2 }; h.FinishQuery(); f.Update(4000);
    Check(f.Stage == RefreshStage.Completed, "new identity not confirmed");
});
Test("post-submit query failure is not success", () => {
    var (h, f) = StartToVerify(); h.QueryBusy = false; h.QuerySucceeded = false; f.Update(4000);
    Check(f.Stage == RefreshStage.Unconfirmed && h.SubmitCalls == 1, "verification failure accepted");
});
Test("changed recruitment settings are not success", () => {
    var (h, f) = StartToVerify(); h.Target = h.Target with { Criteria = "different", RemainingSeconds = 3599 };
    h.FinishQuery(); f.Update(4000);
    Check(f.Stage == RefreshStage.Unconfirmed, "changed settings accepted");
});
Test("post-submit cooldown prevents immediate rerun", () => {
    var (h, f) = StartToVerify(); h.FinishQuery(); f.Update(4000); f.Request(4500); f.Update(4500);
    Check(!f.Busy && h.QueryCalls == 2, "cooldown ignored");
    f.Request(7000); f.Update(7000); Check(h.QueryCalls == 3, "new manual run prevented");
});
Test("countdown parsing and unknown formats", () => {
    Check(RefreshEvidence.ParseSeconds("남은 시간: 59:42") == 3582, "minutes:seconds");
    Check(RefreshEvidence.ParseSeconds("01:00:00") == 3600, "hours:minutes:seconds");
    Check(RefreshEvidence.ParseSeconds("59분 42초") == 3582, "Korean countdown");
    Check(RefreshEvidence.ParseSeconds("모집 종료") == null, "unknown accepted");
    Check(RefreshEvidence.ParseSeconds("99:00") == null, "out of range accepted");
    Check(RefreshEvidence.ParseSeconds("1시간 5분") == null, "partial hour accepted");
});
Test("ownership remains conservative", () => {
    var party = new (ulong Id, string Name, bool IsLeader)[] { (10, "나", true), (20, "다른 사람", false) };
    Check(ListingIdentity.Resolve(10, "나", true, 10, "나", party) == ListingOwner.Self, "self");
    Check(ListingIdentity.Resolve(20, "다른 사람", true, 10, "나", party) == ListingOwner.Other, "other");
    Check(ListingIdentity.Resolve(10, "나", false, 10, "나", party) == ListingOwner.Unknown, "mismatch");
});
Test("reported Korean placeholder in both UI sources resolves to an empty comment", () => {
    var description = ListingDescription.Resolve(true, "소개말 없음", true, "소개말 없음", true, "");
    Check(description.Readable && description.NativeAgrees && description.EmptyPlaceholder, "reported empty comment rejected");
    Check(description.Text == "", "Korean placeholder leaked into canonical comment");
    Check(ListingIdentity.Resolve(10, "나", description.NativeAgrees, 10, "나", []) == ListingOwner.Self, "self listing remains unverified");
});
Test("literal Korean placeholder text remains a nonempty authored comment", () => {
    var description = ListingDescription.Resolve(true, "소개말 없음", true, "소개말 없음", true, "소개말 없음");
    Check(description.NativeAgrees && description.Text == "소개말 없음" && !description.EmptyPlaceholder, "authored comment discarded");
    Check(!ListingDescription.Resolve(true, "소개말 없음", true, "소개말 없음", true, "실제 내용").NativeAgrees, "nonempty native mismatch ignored");
});
Test("Korean empty placeholder recognition requires exact agreeing UI text", () => {
    foreach (var text in new[] { "다른말 없음", "소개말 없음!", "소개말없음" })
        Check(!ListingDescription.Resolve(true, text, true, text, true, "").NativeAgrees, "arbitrary or partial placeholder accepted");
    Check(!ListingDescription.Resolve(true, "소개말 없음", true, "다른 내용", true, "").NativeAgrees, "different buffer ignored");
    Check(!ListingDescription.Resolve(true, "다른 내용", true, "소개말 없음", true, "").NativeAgrees, "different node ignored");
});
Test("an empty addon buffer still confirms an empty native comment", () => {
    var description = ListingDescription.Resolve(true, "게임의 빈 문구 안내", true, "", true, "");
    Check(description.Readable && description.NativeAgrees && description.EmptyPlaceholder, "empty comment rejected");
    Check(description.Text == "", "placeholder leaked into canonical comment");
});
Test("empty comment does not grant ownership to another character", () => {
    var description = ListingDescription.Resolve(true, "소개말 없음", true, "소개말 없음", true, "");
    Check(ListingIdentity.Resolve(20, "다른 사람", description.NativeAgrees, 10, "나", []) == ListingOwner.Other, "other owner accepted");
    Check(ListingIdentity.Resolve(10, "다른 이름", description.NativeAgrees, 10, "나", []) == ListingOwner.Unknown, "name mismatch accepted");
});
Test("unreadable native text cannot be mistaken for an empty comment", () => {
    var description = ListingDescription.Resolve(true, "소개말 없음", true, "소개말 없음", false, "");
    Check(!description.NativeAgrees && !description.EmptyPlaceholder, "failed read accepted as empty");
});
Test("an empty native comment alone cannot override visible content", () => {
    Check(!ListingDescription.Resolve(true, "실제 모집 내용", false, "", true, "").NativeAgrees, "unreadable buffer accepted");
    Check(!ListingDescription.Resolve(true, "실제 모집 내용", true, "실제 모집 내용", true, "").NativeAgrees, "nonempty buffer accepted");
});
Test("nonempty comments must still agree and are never normalized to empty", () => {
    var matching = ListingDescription.Resolve(true, "모집 내용", true, "", true, "모집 내용");
    Check(matching.NativeAgrees && matching.Text == "모집 내용" && !matching.EmptyPlaceholder, "nonempty text discarded");
    var mismatch = ListingDescription.Resolve(true, "다른 모집 내용", true, "모집 내용", true, "모집 내용");
    Check(!mismatch.NativeAgrees, "nonempty mismatch bypassed through buffer");
});
Test("normal empty node and existing buffer fallback remain valid", () => {
    Check(ListingDescription.Resolve(true, "", false, "", true, "").NativeAgrees, "empty node rejected");
    var fallback = ListingDescription.Resolve(false, "", true, "내용", true, "내용");
    Check(fallback.NativeAgrees && fallback.Text == "내용", "buffer fallback broken");
    Check(!ListingDescription.Resolve(false, "", false, "", true, "").Readable, "failed UI reads treated as empty");
});
Test("empty listing refreshes once without copying the placeholder into the editor", () => {
    var description = ListingDescription.Resolve(true, "소개말 없음", true, "소개말 없음", true, "");
    var h = new FakeHost { OwnVerified = description.NativeAgrees };
    h.Target = h.Target with { Description = description.Text };
    var flow = new ListingRefresh(h);
    flow.Request(1000); flow.Update(1000); h.FinishQuery(); flow.Update(1500);
    flow.Update(2000); flow.Update(3500);
    var refreshed = ListingDescription.Resolve(true, "소개말 없음", true, "소개말 없음", true, "");
    h.OwnVerified = refreshed.NativeAgrees;
    h.Target = h.Target with { Description = refreshed.Text, RemainingSeconds = 3598 };
    h.FinishQuery(); flow.Update(4000);
    Check(flow.Stage == RefreshStage.Completed && h.SubmitCalls == 1, "empty refresh failed");
    Check(h.SubmittedDescription == "", "placeholder submitted");
});
Test("adding text during an empty-comment refresh is detected", () => {
    var h = new FakeHost(); h.Target = h.Target with { Description = "" };
    var flow = new ListingRefresh(h);
    flow.Request(1000); flow.Update(1000); h.FinishQuery(); flow.Update(1500);
    flow.Update(2000); flow.Update(3500);
    h.Target = h.Target with { Description = "추가된 문구", RemainingSeconds = 3598 };
    h.FinishQuery(); flow.Update(4000);
    Check(flow.Stage == RefreshStage.Unconfirmed && h.SubmitCalls == 1, "new content ignored or resubmitted");
});
ListingSlotsTests.Run(Test, Check);
ListingJobRulesTests.Run(Test, Check);
ListingRoleButtonsTests.Run(Test, Check);
ListingJobEditTests.Run(Test, Check);
ListingSlotRouteTests.Run(Test, Check);
ListingFriendlyMatchTests.Run(Test, Check);
ListingSparseSlotsTests.Run(Test, Check);
ListingRoleComparisonTests.Run(Test, Check);
ListingEditorCriteriaTests.Run(Test, Check);
ListingDescriptionEditTests.Run(Test, Check);
ListingObjectiveEditTests.Run(Test, Check);
ListingBeginnerEditTests.Run(Test, Check);
ListingUniqueJobsEditTests.Run(Test, Check);
ListingPrivateEditTests.Run(Test, Check);
ListingCompletionEditTests.Run(Test, Check);
ListingItemLevelEditTests.Run(Test, Check);
ListingRuleEditTests.Run(Test, Check);
CompatibilityTests.Run(Test, Check);
Console.WriteLine($"All {passed} tests passed. Native game interaction still requires in-game validation.");

sealed class FakeHost : IListingRefreshHost
{
    public ulong SelfId { get; set; } = 10;
    public string? BlockReason { get; set; }
    public bool QueryBusy { get; set; }
    public bool QuerySucceeded { get; set; }
    public long DetailRevision { get; set; }
    public bool EditorVisible { get; set; }
    public bool EditorReady { get; set; }
    public RecruitmentEditorMode EditorActionMode { get; set; } = RecruitmentEditorMode.Refresh;
    public long EditorRevision { get; set; }
    public long EditorClosedRevision { get; set; }
    public bool OwnVerified = true, SubmitAllowed = true, KeepEditorOpen, ThrowOnSubmit;
    public int QueryCalls, EditCalls, SubmitCalls;
    public string? SubmittedDescription, SubmittedDescriptionArgument;
    public RefreshTarget? SubmittedTarget, EditExpected;
    public int JobEditCalls, JobEditChecks;
    public bool JobEditAllowed = true, JobEditComplete = true, JobEditFailed, ThrowOnJobEdit;
    public int DescriptionEditCalls, DescriptionEditChecks;
    public bool DescriptionEditAllowed = true, DescriptionEditComplete = true, DescriptionEditFailed, ThrowOnDescriptionEdit;
    public RefreshTarget? DescriptionExpected;
    public int ObjectiveEditCalls;
    public bool ObjectiveEditAllowed = true, ObjectiveEditComplete = true, ObjectiveEditFailed, ThrowOnObjectiveEdit;
    public RefreshTarget? ObjectiveExpected;
    public int BeginnerEditCalls;
    public bool BeginnerEditAllowed = true, BeginnerEditComplete = true, BeginnerEditFailed, ThrowOnBeginnerEdit;
    public RefreshTarget? BeginnerExpected;
    public int UniqueJobsEditCalls;
    public bool UniqueJobsEditAllowed = true, UniqueJobsEditComplete = true, UniqueJobsEditFailed, ThrowOnUniqueJobsEdit;
    public RefreshTarget? UniqueJobsExpected;
    public int PrivateEditCalls;
    public bool PrivateEditAllowed = true, PrivateEditComplete = true, PrivateEditFailed, ThrowOnPrivateEdit;
    public RefreshTarget? PrivateExpected;
    public int CompletionEditCalls;
    public bool CompletionEditAllowed = true, CompletionEditComplete = true, CompletionEditFailed, ThrowOnCompletionEdit;
    public RefreshTarget? CompletionExpected;
    public int ItemLevelEditCalls;
    public bool ItemLevelEditAllowed = true, ItemLevelEditComplete = true, ItemLevelEditFailed, ThrowOnItemLevelEdit;
    public RefreshTarget? ItemLevelExpected;
    public int RuleEditCalls;
    public bool RuleEditAllowed = true, RuleEditComplete = true, RuleEditFailed, ThrowOnRuleEdit;
    public RefreshTarget? RuleExpected;
    public RefreshTarget Target = new(10, 1, 123, "기존 모집 문구", "same", 1800, 8);
    public void QueryOwn() { QueryCalls++; QueryBusy = true; QuerySucceeded = false; }
    public void FinishQuery() { QueryBusy = false; QuerySucceeded = true; DetailRevision++; }
    public void CancelQuery() => QueryBusy = false;
    public bool TryGetOwn(out RefreshTarget target, out string error) { target = Target; error = "작성자 확인 실패"; return OwnVerified; }
    public bool OpenEditor(RefreshTarget target, out string error)
    { EditCalls++; EditorVisible = EditorReady = true; EditorRevision++; error = ""; return true; }
    public bool Submit(RefreshTarget target, string? description, out string error)
    {
        SubmitCalls++; SubmittedTarget = target; SubmittedDescription = target.Description; error = "조건 확인 실패";
        SubmittedDescriptionArgument = description;
        if (ThrowOnSubmit) throw new InvalidOperationException("simulated native failure");
        if (SubmitAllowed && !KeepEditorOpen) EditorVisible = EditorReady = false;
        return SubmitAllowed;
    }
    public bool BeginSlotEdit(RefreshTarget target, ListingJobEdit edit, out string error)
    {
        JobEditCalls++; error = "직업 창 확인 실패";
        if (ThrowOnJobEdit) throw new InvalidOperationException("simulated role failure");
        EditExpected = edit.Expected(target);
        return JobEditAllowed;
    }
    public SlotEditProgress UpdateSlotEdit(out RefreshTarget expected, out string error)
    {
        JobEditChecks++; expected = EditExpected!; error = "직업 변경 확인 실패";
        return JobEditFailed ? SlotEditProgress.Failed : JobEditComplete ? SlotEditProgress.Complete : SlotEditProgress.Waiting;
    }
    public bool BeginDescriptionEdit(RefreshTarget target, ListingDescriptionEdit edit, out string error)
    {
        DescriptionEditCalls++; error = "소개말 입력 실패";
        if (ThrowOnDescriptionEdit) throw new InvalidOperationException("simulated description failure");
        DescriptionExpected = edit.Expected(target);
        return DescriptionEditAllowed;
    }
    public SlotEditProgress UpdateDescriptionEdit(out RefreshTarget expected, out string error)
    {
        DescriptionEditChecks++; expected = DescriptionExpected!; error = "소개말 반영 확인 실패";
        return DescriptionEditFailed ? SlotEditProgress.Failed : DescriptionEditComplete ? SlotEditProgress.Complete : SlotEditProgress.Waiting;
    }
    public bool BeginObjectiveEdit(RefreshTarget target, ListingObjectiveEdit edit, out string error)
    {
        ObjectiveEditCalls++; error = "목적 선택 실패";
        if (ThrowOnObjectiveEdit) throw new InvalidOperationException("simulated objective failure");
        // This fake tests flow/result validation only. It does not model or
        // establish a native dropdown/callback contract. UI phases have separate tests.
        ObjectiveExpected = edit.Expected(target);
        return ObjectiveEditAllowed;
    }
    public SlotEditProgress UpdateObjectiveEdit(out RefreshTarget expected, out string error)
    {
        expected = ObjectiveExpected!; error = "목적 반영 확인 실패";
        return ObjectiveEditFailed ? SlotEditProgress.Failed : ObjectiveEditComplete ? SlotEditProgress.Complete : SlotEditProgress.Waiting;
    }
    public bool BeginBeginnerEdit(RefreshTarget target, ListingBeginnerEdit edit, out string error)
    {
        BeginnerEditCalls++; error = "초보자 환영 설정 실패";
        if (ThrowOnBeginnerEdit) throw new InvalidOperationException("simulated beginner failure");
        // The fake validates the managed refresh flow, not native event behavior.
        BeginnerExpected = edit.Expected(target);
        return BeginnerEditAllowed;
    }
    public SlotEditProgress UpdateBeginnerEdit(out RefreshTarget expected, out string error)
    {
        expected = BeginnerExpected!; error = "초보자 환영 반영 확인 실패";
        return BeginnerEditFailed ? SlotEditProgress.Failed : BeginnerEditComplete ? SlotEditProgress.Complete : SlotEditProgress.Waiting;
    }
    public bool BeginUniqueJobsEdit(RefreshTarget target, ListingUniqueJobsEdit edit, out string error)
    {
        UniqueJobsEditCalls++; error = "잡 중복 없음 설정 실패";
        if (ThrowOnUniqueJobsEdit) throw new InvalidOperationException("simulated unique jobs failure");
        // This fake exercises the managed flow, not the native click route.
        UniqueJobsExpected = edit.Expected(target);
        return UniqueJobsEditAllowed;
    }
    public SlotEditProgress UpdateUniqueJobsEdit(out RefreshTarget expected, out string error)
    {
        expected = UniqueJobsExpected!; error = "잡 중복 없음 반영 확인 실패";
        return UniqueJobsEditFailed ? SlotEditProgress.Failed : UniqueJobsEditComplete ? SlotEditProgress.Complete : SlotEditProgress.Waiting;
    }
    public bool BeginPrivateEdit(RefreshTarget target, ListingPrivateEdit edit, out string error)
    {
        PrivateEditCalls++; error = "비공개 설정 실패";
        if (ThrowOnPrivateEdit) throw new InvalidOperationException("simulated private failure");
        // This fake exercises the managed flow, not the native click route.
        PrivateExpected = edit.Expected(target);
        return PrivateEditAllowed;
    }
    public SlotEditProgress UpdatePrivateEdit(out RefreshTarget expected, out string error)
    {
        expected = PrivateExpected!; error = "비공개 반영 확인 실패";
        return PrivateEditFailed ? SlotEditProgress.Failed : PrivateEditComplete ? SlotEditProgress.Complete : SlotEditProgress.Waiting;
    }
    public bool BeginCompletionEdit(RefreshTarget target, ListingCompletionEdit edit, out string error)
    {
        CompletionEditCalls++; error = "완료조건 설정 실패";
        if (ThrowOnCompletionEdit) throw new InvalidOperationException("simulated completion failure");
        // This fake exercises the managed flow, not the native click route.
        CompletionExpected = edit.Expected(target);
        return CompletionEditAllowed;
    }
    public SlotEditProgress UpdateCompletionEdit(out RefreshTarget expected, out string error)
    {
        expected = CompletionExpected!; error = "완료조건 반영 확인 실패";
        return CompletionEditFailed ? SlotEditProgress.Failed : CompletionEditComplete ? SlotEditProgress.Complete : SlotEditProgress.Waiting;
    }
    public bool BeginItemLevelEdit(RefreshTarget target, ListingItemLevelEdit edit, out string error)
    {
        ItemLevelEditCalls++; error = "평균 아이템 레벨 설정 실패";
        if (ThrowOnItemLevelEdit) throw new InvalidOperationException("simulated itemLevel failure");
        // This fake exercises the managed flow, not the native click route.
        ItemLevelExpected = edit.Expected(target);
        return ItemLevelEditAllowed;
    }
    public SlotEditProgress UpdateItemLevelEdit(out RefreshTarget expected, out string error)
    {
        expected = ItemLevelExpected!; error = "평균 아이템 레벨 반영 확인 실패";
        return ItemLevelEditFailed ? SlotEditProgress.Failed : ItemLevelEditComplete ? SlotEditProgress.Complete : SlotEditProgress.Waiting;
    }
    public bool BeginRuleEdit(RefreshTarget target, ListingRuleEdit edit, out string error)
    {
        RuleEditCalls++; error = "모집 규칙 설정 실패";
        if (ThrowOnRuleEdit) throw new InvalidOperationException("simulated rule failure");
        RuleExpected = edit.Expected(target);
        return RuleEditAllowed;
    }
    public SlotEditProgress UpdateRuleEdit(out RefreshTarget expected, out string error)
    {
        expected = RuleExpected!; error = "모집 규칙 반영 확인 실패";
        return RuleEditFailed ? SlotEditProgress.Failed : RuleEditComplete ? SlotEditProgress.Complete : SlotEditProgress.Waiting;
    }

}
