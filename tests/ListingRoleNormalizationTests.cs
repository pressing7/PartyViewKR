using System.Runtime.InteropServices;
using PartyViewKR;

static class ListingRoleNormalizationTests
{
    private static RefreshTarget ReplaceFlags(RefreshTarget source, params (int Index, ulong Flags)[] changes)
    {
        if (!ListingEditorCriteria.TryReadFlags(source.Criteria, source.Slots.NativeSlotCount, out var flags)) throw new Exception("bad fixture");
        foreach (var (i, value) in changes) flags[i] = value;
        var fields = source.Criteria.Split('/'); fields[^1] = Convert.ToHexString(MemoryMarshal.AsBytes(flags.AsSpan()));
        return source with { Criteria = string.Join('/', fields), Slots = source.Slots with {
            Slots = source.Slots.Slots.Select(s => s with { SlotFlags = flags[s.NativeIndex] }).ToArray()
        } };
    }

    private static RefreshTarget Target(int count, bool friendly = false)
    {
        var t = ListingBatchTests.Target(count);
        var flags = Enumerable.Repeat(t.Slots.JobRules.AllFlags, count).ToArray();
        if (friendly) foreach (var i in new[] { 5, 6, 7, 13, 14, 15 }) flags[i] = 0;
        var jobs = new byte[count]; var members = new ulong[count];
        jobs[0] = 19; members[0] = 10; jobs[1] = 24; members[1] = 20;
        var total = flags.Count(f => f != 0); var groups = count <= 8 ? 1 : count / 8;
        var fields = t.Criteria.Split('/'); fields[^1] = Convert.ToHexString(MemoryMarshal.AsBytes(flags.AsSpan()));
        return t with { Criteria = string.Join('/', fields), TotalSlots = total, Slots =
            ListingSlots.Read(true, total, 2, groups, groups > 1, jobs, members, flags, id => $"직업{id}", t.Slots.JobRules, friendly ? (byte)0x13 : (byte)0) };
    }

    private static RefreshTarget Narrow(RefreshTarget t) => ReplaceFlags(t, (0, 1ul << 8), (1, 1ul << 13));

    internal static void Run(Action<string, Action> test, Action<bool, string> check)
    {
        test("occupied entry job normalization works for normal alliance and friendly listings without changing displayed source", () => {
            foreach (var (count, friendly) in new[] { (4, false), (8, false), (24, false), (48, false), (24, true) }) {
                var t = Target(count, friendly); var after = Narrow(t); var groups = count <= 8 ? 1 : count / 8;
                check(ListingPendingChanges.SameSource(t, after) && ListingWebEdit.SameSource(t, after), "normalization looked stale");
                check(ListingEditorCriteria.Matches(t, after.Criteria, groups, friendly ? 5 : Math.Min(count, 8), out var padding, out var occupied) &&
                    padding.Length == 0 && occupied.SequenceEqual(new[] { 0, 1 }), "entry-job masks blocked editor");
                check(t.Slots.Slots[0].SlotFlags == t.Slots.JobRules.AllFlags && t.Slots.Slots[0].JobId == 19, "comparison rewrote recruitment data");
                check(!ListingPendingChanges.SameSource(after, t), "mask widening incorrectly treated as normalization");
            }
        });
        test("occupied normalization never ignores vacancy changes wrong entry jobs or unverified job masks", () => {
            var t = Target(8); var narrow = Narrow(t);
            foreach (var bad in new[] {
                ReplaceFlags(narrow, (2, 1ul << 8)), ReplaceFlags(narrow, (0, 1ul << 13)),
                ReplaceFlags(narrow, (0, (1ul << 8) | (1ul << 13))), ReplaceFlags(narrow, (0, 0)),
                ReplaceFlags(narrow, (0, (1ul << 8) | (1ul << 63))) }) {
                check(!ListingPendingChanges.SameSource(t, bad), "unrelated role change accepted");
                check(!ListingEditorCriteria.Matches(t, bad.Criteria, 1, 8, out _), "unrelated editor role change accepted");
            }
            var missingJob = t with { Slots = t.Slots with { JobRules = ListingJobRules.Unavailable } };
            check(!ListingEditorCriteria.Matches(missingJob, narrow.Criteria, 1, 8, out _), "unknown catalog accepted");
            check(!ListingEditorCriteria.Matches(ReplaceFlags(t, (0, 1ul << 13)), narrow.Criteria, 1, 8, out _), "entry job absent in original restriction accepted");
            check(!ListingEditorCriteria.Matches(t, narrow.Criteria, 3, 8, out _), "wrong layout accepted");
        });
        test("occupied normalization retains exact participant and registered job identity checks", () => {
            var t = Target(8); var narrow = Narrow(t);
            foreach (var change in new Func<ListingSlot, ListingSlot>[] {
                s => s with { MemberContentId = 999 }, s => s with { JobId = 25 }, s => s with { MemberContentId = 0 },
                s => s with { JobId = 0 }, s => s with { MemberContentId = 0, JobId = 0 } }) {
                var slots = narrow.Slots.Slots.ToArray(); slots[1] = change(slots[1]);
                var bad = narrow with { Slots = narrow.Slots with { Slots = slots } };
                check(!ListingPendingChanges.SameSource(t, bad), "changed or missing participant accepted");
            }
            var joined = narrow.Slots.Slots.ToArray(); joined[2] = joined[2] with { MemberContentId = 30, JobId = 21 };
            check(!ListingPendingChanges.SameSource(t, narrow with { Slots = narrow.Slots with { Slots = joined, SlotsFilled = 3 } }), "new join treated as normalization");
            var unknown = t.Slots.Slots.ToArray(); unknown[0] = unknown[0] with { MemberContentId = 0 };
            check(!ListingEditorCriteria.Matches(t with { Slots = t.Slots with { Slots = unknown } }, narrow.Criteria, 1, 8, out _), "CID-less occupant normalized");
        });
        test("normalization compares all other settings and rejects contradictory role snapshots", () => {
            var t = Target(8); var after = Narrow(t);
            for (var i = 0; i < 8; i++) {
                var fields = after.Criteria.Split('/'); fields[i] = "999";
                var bad = after with { Criteria = string.Join('/', fields) };
                check(!ListingPendingChanges.SameSource(t, bad) && !ListingEditorCriteria.Matches(t, bad.Criteria, 1, 8, out _), "setting ignored");
            }
            foreach (var bad in new[] { after with { Description = "수동 수정" }, after with { ExtraCriteria = "1/0/False/1" },
                after with { ListingId = 999 }, after with { SelfId = 999 }, after with { DutyId = 999 },
                after with { Slots = t.Slots }, after with { Criteria = after.Criteria[..^1] } })
                check(!ListingPendingChanges.SameSource(t, bad), "unrelated or inconsistent snapshot accepted");
        });
        test("friendly hidden padding and occupied entry-job normalization compose while real unregistered slots stay exact", () => {
            var t = Target(24, true); var n = Narrow(t);
            var indices = new[] { 5, 6, 7, 13, 14, 15 };
            var editor = ReplaceFlags(n, indices.Select(i => (i, 0xFDFFFFFEul)).ToArray());
            check(ListingEditorCriteria.Matches(t, editor.Criteria, 3, 5, out var p, out var occupied) &&
                p.SequenceEqual(indices) && occupied.SequenceEqual(new[] { 0, 1 }), "combined normalization rejected");
            check(!ListingEditorCriteria.Matches(t, ReplaceFlags(editor, (12, 0)).Criteria, 3, 5, out _), "real friendly slot ignored");
        });
        test("every individual edit keeps its preflight and result checks after occupied masks normalize", () => {
            var t = Target(8); var n = Narrow(t);
            var description = new ListingDescriptionEdit(t, "새 소개말"); var job = new ListingJobEdit(t, 3, 1ul << 8, t.Slots.JobRules);
            var objective = new ListingObjectiveEdit(t, 1); var beginner = new ListingBeginnerEdit(t, true);
            var unique = new ListingUniqueJobsEdit(t, true); var privacy = new ListingPrivateEdit(t, true, "0000");
            var completion = new ListingCompletionEdit(t, true, 1); var level = new ListingItemLevelEdit(t, true, "555");
            check(description.MatchesOriginal(n) && description.MatchesResult(t, Narrow(description.Expected(t))), "description blocked");
            check(job.MatchesOriginal(n) && job.MatchesResult(t, Narrow(job.Expected(t))), "job edit blocked");
            check(objective.MatchesOriginal(n) && objective.MatchesResult(t, Narrow(objective.Expected(t))), "objective blocked");
            check(beginner.MatchesOriginal(n) && beginner.MatchesResult(t, Narrow(beginner.Expected(t))), "beginner blocked");
            check(unique.MatchesOriginal(n) && unique.MatchesResult(t, Narrow(unique.Expected(t))), "unique jobs blocked");
            check(privacy.MatchesOriginal(n) && privacy.MatchesResult(t, Narrow(privacy.Expected(t))), "privacy blocked");
            check(completion.MatchesOriginal(n) && completion.MatchesResult(t, Narrow(completion.Expected(t))), "completion blocked");
            check(level.MatchesOriginal(n) && level.MatchesResult(t, Narrow(level.Expected(t))), "item level blocked");
            foreach (var kind in Enum.GetValues<ListingRuleKind>()) {
                var rule = new ListingRuleEdit(t, kind, 1);
                check(rule.MatchesOriginal(n) && rule.MatchesResult(t, Narrow(rule.Expected(t))), "rule blocked: " + kind);
            }
        });
        test("bound refresh and batch preflight accept normalized occupants and submit once", () => {
            foreach (var batch in new[] { false, true }) {
                var t = Target(8); var h = new BatchFakeHost { Target = Narrow(t) }; var f = new ListingRefresh(h);
                if (batch) f.RequestBatch(1000, [ListingBatchTests.Description(t)]);
                else f.Request(1000, source: t);
                f.Update(1000); h.FinishQuery(); f.Update(1500);
                check(h.EditCalls == 1 && f.Stage == RefreshStage.OpeningEditor, "normalization blocked preflight");
                long tick = 2000;
                for (var i = 0; i < 40 && f.Busy && h.SubmitCalls == 0; i++, tick += 500) f.Update(tick);
                check(h.SubmitCalls == 1, "refresh did not submit once");
                f.Update(tick + 2000); h.Target = h.SubmittedTarget! with { RemainingSeconds = 3598 };
                h.FinishQuery(); f.Update(tick + 2500); f.Update(tick + 3000);
                check(f.Stage == RefreshStage.Completed && h.SubmitCalls == 1, "normalized refresh result unconfirmed");
            }
        });
    }
}
