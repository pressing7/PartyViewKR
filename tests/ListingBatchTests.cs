using System.Runtime.InteropServices;
using PartyViewKR;

static class ListingBatchTests
{
    internal static RefreshTarget Target(int count = 8)
    {
        var rules = ListingJobRules.Read(new uint[] { 19, 21, 24, 25, 43 }.Select(id => new RecruitmentJob(id, "직업" + id)));
        var flags = Enumerable.Repeat(rules.AllFlags, count).ToArray(); flags[0] = 1ul << 8;
        var jobs = new byte[count]; jobs[0] = 19;
        var members = new ulong[count]; members[0] = 10;
        return new(10, 123, 321, "기존 소개말", "4/321/1/0/1/0/0/1/" + Convert.ToHexString(MemoryMarshal.AsBytes(flags.AsSpan())), 1800, count) {
            Slots = ListingSlots.Read(true, count, 1, count <= 8 ? 1 : count / 8, count > 8, jobs, members, flags, id => "직업" + id, rules),
            ExtraCriteria = $"0/0/{count > 8}/{(count <= 8 ? 1 : count / 8)}", JobEditingAllowed = true
        };
    }
    internal static ListingChange Description(RefreshTarget t, string text = "새 소개말") => new(new ListingDescriptionEdit(t, text));
    internal static ListingChange Rule(RefreshTarget t, ListingRuleKind kind = ListingRuleKind.Unrestricted, int value = 1) => new(new ListingRuleEdit(t, kind, value));
    private static (BatchFakeHost host, ListingRefresh flow) Start(ListingChange[] changes)
    {
        var h = new BatchFakeHost { Target = changes[0].Original }; var f = new ListingRefresh(h);
        f.RequestBatch(1000, changes); f.Update(1000); h.FinishQuery(); f.Update(1500); f.Update(2000);
        return (h, f);
    }
    private static long RunToSubmit(BatchFakeHost h, ListingRefresh f, long tick = 2500)
    {
        for (var i = 0; i < 160 && f.Busy && h.SubmitCalls == 0; i++, tick += 500) f.Update(tick);
        return tick;
    }
    private static void Verify(BatchFakeHost h, ListingRefresh f, long tick, RefreshTarget? result = null)
    {
        f.Update(tick + 2000);
        h.Target = result ?? h.SubmittedTarget! with { RemainingSeconds = 3598 };
        h.FinishQuery(); f.Update(tick + 2500);
    }
    internal static void Run(Action<string, Action> test, Action<bool, string> check)
    {
        test("batch queue replaces individual keys, keeps multiple jobs, and restores original values", () => {
            var t = Target(); var q = new ListingPendingChanges();
            check(q.Queue(Description(t), out _) && q.Queue(Rule(t), out _), "queue rejected changes");
            check(q.Queue(Description(t, "두 번째 소개말"), out _) && q.Items.Count == 2, "duplicate description");
            foreach (var pos in new[] { 2, 3 }) check(q.Queue(new(new ListingJobEdit(t, pos, 1ul << 8, t.Slots.JobRules)), out _), "multi job rejected");
            check(q.Items.Count == 4 && q.Items[0].ValueLabel == "두 번째 소개말", "replacement lost ordering");
            check(q.Queue(Description(t, t.Description), out _) && q.Items.Count == 3, "restoring original did not remove queued field");
            check(q.Take().Length == 3 && q.Items.Count == 0, "queue replayed");
        });
        test("batch queue rejects invalid input and visibly drops stale listing drafts", () => {
            var t = Target(); var q = new ListingPendingChanges(); q.Queue(Description(t), out _);
            check(!q.Queue(new(new ListingPrivateEdit(t, true, "123")), out _) && q.Items.Count == 1, "invalid PIN queued");
            check(!q.Queue(Rule(t with { ListingId = 99 }), out var error) && error.Length > 0 && q.Items.Count == 0, "mixed listing queue");
        });
        test("all edit kinds and multiple slots share one cumulative editor and one submit", () => {
            foreach (var count in new[] { 4, 8, 24, 48 })
            {
                var t = Target(count);
                ListingChange[] changes = [Description(t), new(new ListingObjectiveEdit(t, 2)),
                    new(new ListingBeginnerEdit(t, true)), new(new ListingUniqueJobsEdit(t, true)),
                    new(new ListingPrivateEdit(t, true, "0000")), new(new ListingCompletionEdit(t, true, 1)),
                    new(new ListingItemLevelEdit(t, true, "555")), Rule(t), Rule(t, ListingRuleKind.MinimumItemLevel),
                    Rule(t, ListingRuleKind.SilenceEcho), Rule(t, ListingRuleKind.Loot, 2),
                    new(new ListingJobEdit(t, 2, 1ul << 8, t.Slots.JobRules)),
                    new(new ListingJobEdit(t, count, 1ul << 10, t.Slots.JobRules))];
                var (h, f) = Start(changes); var tick = RunToSubmit(h, f);
                check(h.EditCalls == 1 && h.SubmitCalls == 1 && h.Prepared.Count == changes.Length, "multiple editors or submissions");
                check(changes.All(c => c.MatchesListing(h.SubmittedTarget!)), "cumulative changes lost");
                Verify(h, f, tick);
                check(f.Stage == RefreshStage.Completed && f.BatchResults.All(r => r.Applied == true) && h.QueryCalls == 2, "result not re-read");
                check(!f.Diagnostic.Contains("0000") && !f.Diagnostic.Contains("555"), "values leaked to batch diagnostic");
            }
        });
        test("disabled rule, later item failure and final success continue to one refresh", () => {
            var t = Target(); var (h, f) = Start([Rule(t), Description(t), new(new ListingBeginnerEdit(t, true)), new(new ListingUniqueJobsEdit(t, true))]);
            h.RuleEditAllowed = false; h.BeginnerEditFailed = true;
            var tick = RunToSubmit(h, f); Verify(h, f, tick);
            check(h.SubmitCalls == 1 && h.EditCalls == 1 && h.UniqueJobsEditCalls == 1, "failure halted later item");
            check(f.BatchResults.Select(r => r.Applied).SequenceEqual(new bool?[] { false, true, false, true }), "incorrect per item results");
            check(f.Status.Contains("미반영·확인 필요 2개"), "missing partial warning");
        });
        test("every item failing still refreshes the unchanged game draft", () => {
            var t = Target(); var (h, f) = Start([Rule(t), Description(t)]);
            h.RuleEditAllowed = h.DescriptionEditAllowed = false;
            var tick = RunToSubmit(h, f); Verify(h, f, tick);
            check(h.SubmitCalls == 1 && RefreshEvidence.SameRecruitment(t, h.SubmittedTarget!) && f.BatchResults.All(r => r.Applied == false), "all failed skipped refresh");
        });
        test("item exception and timeout advance without retrying the failed operation", () => {
            foreach (var throws in new[] { true, false })
            {
                var t = Target(); var (h, f) = Start([Rule(t), Description(t)]);
                h.ThrowOnRuleEdit = throws; h.RuleEditComplete = false;
                var tick = RunToSubmit(h, f); Verify(h, f, tick);
                check(h.RuleEditCalls == 1 && h.DescriptionEditCalls == 1 && h.SubmitCalls == 1 && f.BatchResults[1].Applied == true, "failed item retried or blocked next");
            }
        });
        test("failed verification adopts actual intermediate values for later changes", () => {
            var t = Target(); var (h, f) = Start([Rule(t, ListingRuleKind.Loot, 2), Description(t)]);
            h.RuleEditFailed = true;
            h.MutateOnFirstFinish = t with { Criteria = ListingRules.WithValue(t.Criteria, ListingRuleKind.Loot, 1) };
            var tick = RunToSubmit(h, f); Verify(h, f, tick);
            check(ListingRules.Read(h.Prepared[1].Criteria, ListingRuleKind.Loot) == 1 &&
                ListingRules.Read(h.SubmittedTarget!.Criteria, ListingRuleKind.Loot) == 1 &&
                f.BatchResults[0].Applied == false && f.BatchResults[1].Applied == true, "partial draft overwritten");
        });
        test("fresh listing determines results even after editor checks passed or failed", () => {
            var t = Target(); var (h, f) = Start([Description(t), Rule(t)]);
            h.RuleEditFailed = true;
            var tick = RunToSubmit(h, f);
            var after = t with { Criteria = ListingRules.WithValue(t.Criteria, ListingRuleKind.Unrestricted, 1), RemainingSeconds = 3598 };
            Verify(h, f, tick, after);
            check(f.BatchResults[0].Applied == false && f.BatchResults[1].Applied == true && h.SubmitCalls == 1, "trusted editor over server");
        });
        test("failed PIN verification is never inferred from the server private flag", () => {
            var t = Target(); var (h, f) = Start([new(new ListingPrivateEdit(t, true, "0123")), Description(t)]);
            h.PrivateEditFailed = true;
            h.MutateOnFirstFinish = t with { ExtraCriteria = ListingPrivate.WithEnabled(t.ExtraCriteria, true) };
            var tick = RunToSubmit(h, f); Verify(h, f, tick);
            check(f.BatchResults[0].Applied == null && f.BatchResults[1].Applied == true &&
                !string.Join('\n', f.BatchResults.Select(r => r.Text)).Contains("0123") && !f.Diagnostic.Contains("0123"), "PIN success guessed or PIN exposed");
        });
        test("unreadable item recovery does not stop later items or refresh", () => {
            var t = Target(); var (h, f) = Start([Description(t), Rule(t)]); h.FinishReadFails = true;
            var tick = RunToSubmit(h, f); Verify(h, f, tick);
            check(h.SubmitCalls == 1 && h.RuleEditCalls == 1, "recovery read failed entire run");
        });
        test("stale listing, character and closed editor do not operate on another listing", () => {
            var t = Target(); var h = new BatchFakeHost { Target = t with { ListingId = 321 } }; var f = new ListingRefresh(h);
            f.RequestBatch(1000, [Description(t)]); f.Update(1000); h.FinishQuery(); f.Update(1500);
            check(h.EditCalls == 0 && f.Stage == RefreshStage.Stopped, "stale listing opened");
            foreach (var close in new[] { true, false })
            {
                (h, f) = Start([Description(t)]);
                if (close) h.EditorClosedRevision++; else h.SelfId = 99;
                f.Update(2500);
                check(!f.Busy && h.DescriptionEditCalls == 0 && h.SubmitCalls == 0, "foreign editor acted");
            }
        });
        test("submit exception never resubmits and verification timeout never claims success", () => {
            var t = Target(); var (h, f) = Start([Description(t)]); h.ThrowOnSubmit = true;
            RunToSubmit(h, f); f.Update(100000);
            check(h.SubmitCalls == 1 && f.Stage == RefreshStage.Unconfirmed, "submit retried");
            (h, f) = Start([Description(t)]); var tick = RunToSubmit(h, f);
            f.Update(tick + 2000); f.Update(tick + 20000);
            check(f.Stage == RefreshStage.Unconfirmed && f.BatchResults[0].Applied == null, "no query claimed applied");
        });
        test("batch cooldown and cancellation prevent repeated actions", () => {
            var t = Target(); var (h, f) = Start([Description(t)]); f.Cancel(); f.Update(2500);
            check(h.SubmitCalls == 0 && !f.Busy, "cancel sent refresh");
            (h, f) = Start([Description(t)]); var tick = RunToSubmit(h, f); Verify(h, f, tick);
            f.RequestBatch(tick + 2501, [Description(t)]); f.Update(tick + 2501);
            check(h.EditCalls == 1 && h.SubmitCalls == 1 && !f.Busy, "cooldown allowed repeat");
        });
        test("friendly sparse batch keeps native job routes and unused padding distinct", () => {
            var baseTarget = Target(24); var flags = Enumerable.Repeat(baseTarget.Slots.JobRules.AllFlags, 24).ToArray();
            foreach (var i in new[] { 5, 6, 7, 12, 13, 14, 15 }) flags[i] = 0;
            flags[0] = 1ul << 8;
            var jobs = new byte[24]; jobs[0] = 19; var members = new ulong[24]; members[0] = 10;
            var slots = ListingSlots.Read(true, 17, 1, 3, true, jobs, members, flags, id => "직업" + id, baseTarget.Slots.JobRules, 0x10);
            var fields = baseTarget.Criteria.Split('/'); fields[^1] = Convert.ToHexString(MemoryMarshal.AsBytes(flags.AsSpan()));
            var t = baseTarget with { TotalSlots = 17, Slots = slots, Criteria = string.Join('/', fields), ExtraCriteria = "0/16/True/3" };
            var firstObserver = Array.FindIndex(t.Slots.Slots, s => s.NativeIndex == 16) + 1;
            ListingChange[] changes = [new(new ListingJobEdit(t, 6, 1ul << 8, t.Slots.JobRules)),
                new(new ListingJobEdit(t, firstObserver, 1ul << 10, t.Slots.JobRules)), Description(t)];
            check(changes[0].NativeIndex == 8 && changes[1].NativeIndex == 16, "compacted position used as native index");
            var (h, f) = Start(changes); var tick = RunToSubmit(h, f); Verify(h, f, tick);
            check(f.BatchResults.All(r => r.Applied == true) && h.SubmittedTarget!.Slots.UnregisteredNativeIndices.SequenceEqual(slots.UnregisteredNativeIndices), "friendly slots changed");
            var raw = flags.ToArray();
            foreach (var i in new[] { 5, 6, 7, 13, 14, 15 }) raw[i] = 0xFDFFFFFE;
            fields[^1] = Convert.ToHexString(MemoryMarshal.AsBytes(raw.AsSpan()));
            check(ListingBatchSnapshot.TryRead(t, string.Join('/', fields), t.Description, t.ExtraCriteria, out var current, out _) &&
                RefreshEvidence.SameRecruitment(t, current) && ListingJobEdit.SameSlots(t.Slots, current.Slots), "fixed padding not normalized");
            raw[12] = 0xFDFFFFFE; fields[^1] = Convert.ToHexString(MemoryMarshal.AsBytes(raw.AsSpan()));
            check(!ListingBatchSnapshot.TryRead(t, string.Join('/', fields), t.Description, t.ExtraCriteria, out _, out _), "real unregistered B5 was ignored");
        });
        test("batch snapshot preserves actual partial changes and never mutates its source", () => {
            var t = Target(); var edited = new ListingJobEdit(t, 3, 1ul << 10, t.Slots.JobRules).Expected(t);
            check(ListingBatchSnapshot.TryRead(t, edited.Criteria, "실제 입력값", "555/32/False/1", out var current, out _), "snapshot refused readable state");
            check(current.Slots.Slots[2].SlotFlags == 1ul << 10 && current.Description == "실제 입력값" &&
                current.ExtraCriteria == "555/32/False/1" && t.Description == "기존 소개말" && t.Slots.Slots[2].SlotFlags == t.Slots.JobRules.AllFlags, "snapshot mutated original");
            check(!ListingBatchSnapshot.TryRead(t, "malformed", "", t.ExtraCriteria, out _, out _), "unreadable flags guessed");
        });
    }
}

sealed class BatchFakeHost : FakeHost, IListingBatchHost
{
    public RefreshTarget? Current, MutateOnFirstFinish;
    public bool FinishReadFails;
    public List<RefreshTarget> Prepared = [];
    public bool PrepareBatchStep(RefreshTarget identity, out RefreshTarget current, out string error)
    {
        current = Current ?? identity; error = ""; Current = current; Prepared.Add(current);
        EditExpected = DescriptionExpected = ObjectiveExpected = BeginnerExpected = UniqueJobsExpected = PrivateExpected = CompletionExpected = ItemLevelExpected = RuleExpected = null;
        return true;
    }
    public SlotEditProgress FinishBatchStep(RefreshTarget identity, out RefreshTarget current, out string error)
    {
        var candidates = new[] {
            (EditExpected, JobEditAllowed && JobEditComplete && !JobEditFailed),
            (DescriptionExpected, DescriptionEditAllowed && DescriptionEditComplete && !DescriptionEditFailed),
            (ObjectiveExpected, ObjectiveEditAllowed && ObjectiveEditComplete && !ObjectiveEditFailed),
            (BeginnerExpected, BeginnerEditAllowed && BeginnerEditComplete && !BeginnerEditFailed),
            (UniqueJobsExpected, UniqueJobsEditAllowed && UniqueJobsEditComplete && !UniqueJobsEditFailed),
            (PrivateExpected, PrivateEditAllowed && PrivateEditComplete && !PrivateEditFailed),
            (CompletionExpected, CompletionEditAllowed && CompletionEditComplete && !CompletionEditFailed),
            (ItemLevelExpected, ItemLevelEditAllowed && ItemLevelEditComplete && !ItemLevelEditFailed),
            (RuleExpected, RuleEditAllowed && RuleEditComplete && !RuleEditFailed)
        };
        Current = MutateOnFirstFinish ?? candidates.FirstOrDefault(c => c.Item1 != null && c.Item2).Item1 ?? Current ?? identity;
        MutateOnFirstFinish = null;
        current = Current; error = "수정창 읽기 실패";
        return FinishReadFails ? SlotEditProgress.Failed : SlotEditProgress.Complete;
    }
    public bool SubmitBatch(RefreshTarget identity, out string error) => Submit(Current ?? identity, (Current ?? identity).Description, out error);
}
