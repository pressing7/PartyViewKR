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
                JobEditingAllowed = true, ExtraCriteria = "700/0/False/1",
            };
        }
        ListingJobEdit Edit(RefreshTarget t) => new(t, 2, 0x2000, rules);
        RefreshTarget ReplaceSlot(RefreshTarget t, int index, ListingSlot slot) => t with {
            Slots = t.Slots with { Slots = t.Slots.Slots.Select((s, i) => i == index ? slot : s).ToArray() } };
        RefreshTarget WithFlags(RefreshTarget target, int index, ulong raw)
        {
            var fields = target.Criteria.Split('/');
            var bytes = Convert.FromHexString(fields[^1]);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(index * sizeof(ulong)), raw);
            fields[^1] = Convert.ToHexString(bytes);
            return ReplaceSlot(target, index, target.Slots.Slots[index] with { SlotFlags = raw, AllowedJobs = rules.Describe(raw) })
                with { Criteria = string.Join('/', fields) };
        }
        (FakeHost host, ListingRefresh flow, ListingJobEdit edit) Start(RefreshTarget? original = null, ulong? selected = null)
        {
            var h = new FakeHost { Target = original ?? Target() }; var f = new ListingRefresh(h);
            var edit = Edit(h.Target) with { Selected = selected ?? 0x2000 };
            f.Request(1000, edit); f.Update(1000); h.FinishQuery(); f.Update(1500); f.Update(2000);
            check(f.Stage == RefreshStage.EditingSlot && h.JobEditCalls == 1 && h.SubmitCalls == 0, "did not enter edit stage");
            return (h, f, edit);
        }
        test("only the edited slot adopts the game's raw value; other slots and settings stay exact", () => {
            foreach (var actualFlags in new[] { 0x2000ul, 0x8000000000002000ul })
            {
                var original = Target(); var edit = Edit(original); var expected = edit.Expected(original, actualFlags);
                check(edit.Valid && edit.MatchesOriginal(original), "valid edit rejected");
                check(expected.Slots.Slots[1].SlotFlags == actualFlags, "game's raw value not retained");
                check(edit.MatchesResult(original, expected), "valid result rejected");
                check(original.Slots.Slots[1].SlotFlags == 0x80000000FFFFFFFEul, "source mutated");
                foreach (var i in new[] { 0, 2, 3 }) check(expected.Slots.Slots[i] == original.Slots.Slots[i], "unselected slot changed");
                check(expected.Description == original.Description && expected.ExtraCriteria == original.ExtraCriteria, "other settings changed");
                check(expected.Criteria != original.Criteria && expected.Criteria.Split('/')[..8].SequenceEqual(original.Criteria.Split('/')[..8]), "non-role criteria changed");
            }
        });
        test("zero, unchanged, unknown, occupied and unsupported slot edits never start", () => {
            var target = Target();
            var cases = new[] {
                Edit(target) with { Selected = 0 }, Edit(target) with { Selected = rules.AllFlags },
                Edit(target) with { Selected = 1ul << 60 }, Edit(target) with { Position = 1 },
                Edit(target) with { Position = 0 }, Edit(target) with { Position = 5 },
                Edit(target with { JobEditingAllowed = false }), Edit(target with { TotalSlots = 24 }),
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
            foreach (var changed in new[] { joined, changedJob, changedOtherSlot, WithFlags(original, 1, rules.AllFlags), original with { ListingId = 124 },
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
            check(h.SubmitCalls == 1 && h.SubmittedTarget!.Slots.Slots[1].SlotFlags == edit.Selected, "wrong/duplicate submit");
            check(h.SubmittedDescriptionArgument == null, "job edit selected the description callback");
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
        test("anyone to restricted accepts the raw flags produced by the game", () => {
            // Synthetic unrestricted encodings: no captured KR raw mask is assumed.
            foreach (var raw in new[] { rules.AllFlags, ulong.MaxValue, rules.AllFlags | (1ul << 63) })
            {
                var (h, f, edit) = Start(WithFlags(Target(), 1, raw));
                check(rules.Describe(raw).Anyone, "fixture must be unrestricted");
                h.EditExpected = WithFlags(h.Target, 1, edit.Selected);
                f.Update(2500);
                check(h.SubmitCalls == 1 && h.SubmittedTarget!.Slots.Slots[1].SlotFlags == edit.Selected, "game-normalized restriction blocked");
                f.Update(4000); h.Target = h.SubmittedTarget! with { RemainingSeconds = 3598 }; h.FinishQuery(); f.Update(4500);
                check(f.Stage == RefreshStage.Completed, "normalized restriction not verified");
            }
        });
        test("restricted to anyone accepts normalization between editor and fresh listing", () => {
            foreach (var (editorRaw, listingRaw) in new[] { (ulong.MaxValue, rules.AllFlags), (rules.AllFlags, ulong.MaxValue) })
            {
                var (h, f, _) = Start(WithFlags(Target(), 1, 0x100), rules.AllFlags);
                h.EditExpected = WithFlags(h.Target, 1, editorRaw);
                f.Update(2500);
                check(h.SubmitCalls == 1 && h.SubmittedTarget!.Slots.Slots[1].SlotFlags == editorRaw, "editor raw flags not bound");
                f.Update(4000); h.Target = WithFlags(h.SubmittedTarget!, 1, listingRaw) with { RemainingSeconds = 3598 };
                h.FinishQuery(); f.Update(4500);
                check(f.Stage == RefreshStage.Completed, "unrestricted listing normalization rejected");
            }
        });
        test("normalization never hides wrong jobs, another slot's bits, members or other settings", () => {
            foreach (var afterSubmit in new[] { false, true })
            for (var mode = 0; mode < 6; mode++)
            {
                var (h, f, edit) = Start();
                var correct = WithFlags(h.Target, 1, edit.Selected);
                var wrong = mode switch {
                    0 => WithFlags(correct, 1, edit.Selected | 0x100),
                    1 => WithFlags(correct, 1, 0),
                    2 => WithFlags(correct, 2, correct.Slots.Slots[2].SlotFlags ^ (1ul << 63)),
                    3 => ReplaceSlot(correct, 1, correct.Slots.Slots[1] with { MemberContentId = 99, JobId = 24 }),
                    4 => correct with { Description = "변경된 소개말" },
                    _ => correct with { ExtraCriteria = "changed" },
                };
                if (!afterSubmit)
                {
                    h.EditExpected = wrong; f.Update(2500);
                    check(f.Stage == RefreshStage.Stopped && h.SubmitCalls == 0, "unexpected edit submitted");
                }
                else
                {
                    h.EditExpected = correct; f.Update(2500); f.Update(4000);
                    h.Target = wrong with { RemainingSeconds = 3598 }; h.FinishQuery(); f.Update(4500);
                    check(f.Stage == RefreshStage.Unconfirmed && h.SubmitCalls == 1, "unexpected listing accepted/retried");
                }
            }
        });
    }
}
