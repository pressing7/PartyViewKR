using System.Runtime.InteropServices;
using PartyViewKR;

static class ListingJobEditTests
{
    internal static void Run(Action<string, Action> test, Action<bool, string> check)
    {
        var rules = ListingJobRules.Read(Enumerable.Range(1, 7).Concat(Enumerable.Range(19, 24))
            .Select(id => new RecruitmentJob((uint)id, $"직업 #{id}")));
        RefreshTarget Target()
        {
            ulong[] flags = [0x400, 0x80000000FFFFFFFE, 0xFFFFFFFE, 0xFFFFFFFE];
            return new(10, 123, 321, "소개말", "4/321/0/0/0/0/0/1/" + Convert.ToHexString(MemoryMarshal.AsBytes(flags.AsSpan())), 1800, 4)
            {
                Slots = ListingSlots.Read(true, 4, 1, 1, false, new byte[] { 21, 0, 0, 0 },
                    new ulong[] { 10, 0, 0, 0 }, flags, id => $"직업 #{id}", rules),
                SinglePartyEditable = true, ExtraCriteria = "700/0/False/1",
            };
        }
        ListingJobEdit Edit(RefreshTarget t) => new(t, 2, 0x2000, rules);
        RefreshTarget ReplaceSlot(RefreshTarget t, int index, ListingSlot slot) => t with {
            Slots = t.Slots with { Slots = t.Slots.Slots.Select((s, i) => i == index ? slot : s).ToArray() } };
        (FakeHost host, ListingRefresh flow, ListingJobEdit edit) Start()
        {
            var h = new FakeHost { Target = Target() }; var f = new ListingRefresh(h); var edit = Edit(h.Target);
            f.Request(1000, edit); f.Update(1000); h.FinishQuery(); f.Update(1500); f.Update(2000);
            check(f.Stage == RefreshStage.EditingSlot && h.JobEditCalls == 1 && h.SubmitCalls == 0, "did not enter edit stage");
            return (h, f, edit);
        }
        test("one-slot selection preserves reserved bits, members, jobs and every other slot", () => {
            var original = Target(); var edit = Edit(original); var expected = edit.Expected(original);
            check(edit.Valid && edit.MatchesOriginal(original), "valid edit rejected");
            check(expected.Slots.Slots[1].SlotFlags == 0x8000000000002000ul, "reserved bits not preserved");
            check(original.Slots.Slots[1].SlotFlags == 0x80000000FFFFFFFEul, "source mutated");
            foreach (var i in new[] { 0, 2, 3 }) check(expected.Slots.Slots[i] == original.Slots.Slots[i], "unselected slot changed");
            check(expected.Description == original.Description && expected.ExtraCriteria == original.ExtraCriteria, "other settings changed");
            check(expected.Criteria != original.Criteria && expected.Criteria.Split('/')[..8].SequenceEqual(original.Criteria.Split('/')[..8]), "non-role criteria changed");
        });
        test("zero, unchanged, unknown, occupied and unsupported slot edits never start", () => {
            var target = Target();
            var cases = new[] {
                Edit(target) with { Selected = 0 }, Edit(target) with { Selected = rules.AllFlags },
                Edit(target) with { Selected = 1ul << 60 }, Edit(target) with { Position = 1 },
                Edit(target) with { Position = 0 }, Edit(target) with { Position = 5 },
                Edit(target with { SinglePartyEditable = false }), Edit(target with { TotalSlots = 24 }),
                Edit(target with { TotalSlots = 48 }), Edit(target) with { Rules = ListingJobRules.Unavailable },
            };
            foreach (var edit in cases)
            {
                var h = new FakeHost(); var f = new ListingRefresh(h); f.Request(1000, edit); f.Update(1000);
                check(!edit.Valid && !f.Busy && h.QueryCalls + h.EditCalls + h.JobEditCalls + h.SubmitCalls == 0, "invalid edit dispatched");
            }
        });
        test("fresh query detects changed members, jobs, listing identity and settings before editor", () => {
            var original = Target();
            var joined = ReplaceSlot(original, 1, original.Slots.Slots[1] with { MemberContentId = 20, JobId = 24 });
            var changedJob = ReplaceSlot(original, 0, original.Slots.Slots[0] with { JobId = 19 });
            var changedOtherSlot = ReplaceSlot(original, 2, original.Slots.Slots[2] with { SlotFlags = 0x100 });
            foreach (var changed in new[] { joined, changedJob, changedOtherSlot, original with { ListingId = 124 },
                original with { Description = "바뀐 글" }, original with { ExtraCriteria = "changed" } })
            {
                var h = new FakeHost { Target = changed }; var f = new ListingRefresh(h);
                f.Request(1000, Edit(original)); f.Update(1000); h.FinishQuery(); f.Update(1500);
                check(f.Stage == RefreshStage.Stopped && h.EditCalls + h.JobEditCalls + h.SubmitCalls == 0, "stale edit opened editor");
            }
        });
        test("job edit submits once and confirms the selected flags through fresh detail", () => {
            var (h, f, edit) = Start();
            h.JobEditComplete = false; f.Update(2500); f.Update(3000);
            check(h.JobEditCalls == 1 && h.SubmitCalls == 0, "unfinished edit submitted");
            h.JobEditComplete = true; f.Update(3500); f.Request(3600, edit); f.Update(4000);
            check(h.SubmitCalls == 1 && h.SubmittedTarget!.Slots.Slots[1].SlotFlags == edit.DesiredFlags, "wrong/duplicate submit");
            f.Update(5000); h.Target = h.SubmittedTarget! with { RemainingSeconds = 3598 }; h.FinishQuery(); f.Update(5500);
            check(f.Stage == RefreshStage.Completed && h.QueryCalls == 2 && h.EditCalls == 1 && h.JobEditCalls == 1, "edit not confirmed");
        });
        test("role failures, timeouts, cancellation and editor closure never submit", () => {
            for (var mode = 0; mode < 4; mode++)
            {
                var (h, f, _) = Start();
                if (mode == 0) h.JobEditFailed = true;
                if (mode == 1) h.JobEditComplete = false;
                if (mode == 2) f.Cancel();
                if (mode == 3) h.EditorClosedRevision++;
                f.Update(2500); f.Update(48000); f.Update(49000);
                check(!f.Busy && h.SubmitCalls == 0 && h.JobEditCalls == 1, "failed edit continued");
            }
        });
        test("an unexpected edit result cannot be submitted", () => {
            var (h, f, _) = Start(); h.EditExpected = h.Target; f.Update(2500);
            check(f.Stage == RefreshStage.Stopped && h.SubmitCalls == 0, "wrong edit result submitted");
        });
        test("opening a role editor that fails or throws is never retried", () => {
            foreach (var throws in new[] { false, true })
            {
                var h = new FakeHost { Target = Target(), JobEditAllowed = false, ThrowOnJobEdit = throws };
                var f = new ListingRefresh(h); f.Request(1000, Edit(h.Target)); f.Update(1000); h.FinishQuery(); f.Update(1500);
                try { f.Update(2000); } catch (InvalidOperationException ex) { f.Fail(ex.Message); }
                f.Update(2500); f.Update(50000);
                check(!f.Busy && h.JobEditCalls == 1 && h.SubmitCalls == 0, "failed role event was repeated");
            }
        });
        test("post-edit old flags or new occupants cannot be reported as success", () => {
            for (var mode = 0; mode < 2; mode++)
            {
                var (h, f, _) = Start(); var original = h.Target; f.Update(2500); f.Update(4000);
                h.Target = mode == 0 ? original with { RemainingSeconds = 3598 } :
                    ReplaceSlot(h.SubmittedTarget!, 1, h.SubmittedTarget!.Slots.Slots[1] with { MemberContentId = 99, JobId = 24 });
                h.FinishQuery(); f.Update(4500);
                check(f.Stage == RefreshStage.Unconfirmed && h.SubmitCalls == 1, "unverified result accepted or retried");
            }
        });
    }
}
