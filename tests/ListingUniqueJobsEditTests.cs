using System.Runtime.InteropServices;
using PartyViewKR;
using PartyViewKR.Compatibility;

static class ListingUniqueJobsEditTests
{
    internal static void Run(Action<string, Action> test, Action<bool, string> check)
    {
        RefreshTarget Target(int count = 8, byte joinFlags = 1, byte beginner = 1, byte objective = 4)
        {
            var flags = Enumerable.Repeat(0xFFFFFFFFFFFFFFFEul, count).ToArray();
            var members = new ulong[count]; members[0] = 10;
            var jobs = new byte[count]; jobs[0] = 25;
            var groups = count > 8 ? count / 8 : 1;
            if (count > 8) joinFlags |= 4;
            return new(10, 123, 321, "기존 소개말", $"4/321/{objective}/{beginner}/4/2/1/1/" + Convert.ToHexString(MemoryMarshal.AsBytes(flags.AsSpan())), 1800, count) {
                Slots = ListingSlots.Read(true, count, 1, groups, count > 8, jobs, members, flags, id => id.ToString()),
                JobEditingAllowed = true, ExtraCriteria = $"700/{joinFlags}/{count > 8}/{groups}",
            };
        }
        (FakeHost host, ListingRefresh flow) Start(bool enabled = true, int count = 8, byte beginner = 1, byte objective = 4, int seconds = 1800)
        {
            var h = new FakeHost { Target = Target(count, enabled ? (byte)1 : (byte)33, beginner, objective) with { RemainingSeconds = seconds } };
            var f = new ListingRefresh(h); f.Request(1000, uniqueJobsEdit: new(h.Target, enabled));
            f.Update(1000); h.FinishQuery(); f.Update(1500); f.Update(2000);
            check(f.Stage == RefreshStage.EditingUniqueJobs && h.UniqueJobsEditCalls == 1 &&
                h.JobEditCalls + h.DescriptionEditCalls + h.ObjectiveEditCalls + h.BeginnerEditCalls + h.SubmitCalls == 0, "wrong edit route");
            return (h, f);
        }
        test("unique jobs uses the captured native flag and preserves every other join bit", () => {
            using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "native-contract.json"));
            var baseline = NativeContract.Parse(stream);
            var key = "field:FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentLookingForGroup+JoinCondition.OnePlayerPerJob";
            check(baseline.Entries[key].Contains($"constant=Byte:{ListingUniqueJobs.Flag:X2};"), "detail flag differs from KR DLL metadata");
            for (var flags = 0; flags <= byte.MaxValue; flags++)
            foreach (var enabled in new[] { false, true })
            {
                var before = $"700/{flags}/False/1";
                var after = ListingUniqueJobs.WithEnabled(before, enabled);
                check(ListingUniqueJobs.Read(after) == enabled && ListingUniqueJobs.TryFlags(after, out _), "wrong flag state");
                ListingUniqueJobs.TryFlags(after, out var actual);
                check(((flags ^ actual) & ~ListingUniqueJobs.Flag) == 0, "unrelated join bit changed");
                check(after.Split('/').Where((_, i) => i != 1).SequenceEqual(before.Split('/').Where((_, i) => i != 1)), "other extra criteria changed");
            }
        });
        test("unique jobs draft prefills the detail bit independently of beginner and purpose", () => {
            foreach (byte flags in new byte[] { 1, 33 })
            {
                var t = Target(joinFlags: flags); var d = new ListingUniqueJobsDraft(t);
                check(d.Enabled == (flags == 33) && !d.Request.Valid, "prefill incorrectly requests a change");
                d.Enabled = !d.Enabled; var expected = d.Request.Expected(t);
                check(expected.Criteria == t.Criteria && expected.Description == t.Description && expected.Slots == t.Slots, "another condition changed");
                check(t.ExtraCriteria == $"700/{flags}/False/1" && ListingUniqueJobs.Read(expected.ExtraCriteria) == d.Enabled, "original mutated");
            }
        });
        test("unique jobs on/off submits once for every layout, purpose and beginner state", () => {
            foreach (var enabled in new[] { false, true })
            foreach (var count in new[] { 4, 8, 24, 48 })
            foreach (byte beginner in new byte[] { 0, 1 })
            foreach (byte objective in new byte[] { 1, 2, 4, 8 })
            {
                var (h, f) = Start(enabled, count, beginner, objective);
                h.UniqueJobsEditComplete = false; f.Update(2500); f.Update(3000);
                check(h.UniqueJobsEditCalls == 1 && h.SubmitCalls == 0, "waiting resent or submitted");
                h.UniqueJobsEditComplete = true; f.Update(3500); f.Update(4000);
                var submitted = h.SubmittedTarget!;
                check(h.SubmitCalls == 1 && ListingUniqueJobs.Read(submitted.ExtraCriteria) == enabled, "wrong/duplicate submit");
                check(submitted.Criteria == h.Target.Criteria && submitted.Description == h.Target.Description &&
                    h.SubmittedDescriptionArgument == null && ListingJobEdit.SameSlots(submitted.Slots, h.Target.Slots), "unrelated fields changed");
                f.Update(5000); h.Target = submitted with { RemainingSeconds = 3598 }; h.FinishQuery(); f.Update(5500);
                check(f.Stage == RefreshStage.Completed && h.UniqueJobsEditCalls == 1 && h.SubmitCalls == 1, "fresh verification failed");
            }
        });
        test("unknown join data, unchanged state or unsupported unique jobs requests never query", () => {
            foreach (var t in new[] { Target(joinFlags: 33), Target() with { JobEditingAllowed = false },
                Target() with { ExtraCriteria = "" }, Target() with { ExtraCriteria = "700/1" },
                Target() with { ExtraCriteria = "700/-1/False/1" }, Target() with { ExtraCriteria = "700/256/False/1" } })
            {
                var h = new FakeHost { Target = t }; var f = new ListingRefresh(h);
                f.Request(1000, uniqueJobsEdit: new(t, true)); f.Update(1000);
                check(f.Stage == RefreshStage.Stopped && h.QueryCalls + h.EditCalls + h.UniqueJobsEditCalls + h.SubmitCalls == 0, "invalid edit dispatched");
            }
        });
        test("unique jobs cannot be combined with any other edit", () => {
            var t = Target();
            for (var mode = 0; mode < 4; mode++)
            {
                var h = new FakeHost { Target = t }; var f = new ListingRefresh(h);
                f.Request(1000, uniqueJobsEdit: new(t, true), beginnerEdit: mode == 0 ? new(t, false) : null,
                    objectiveEdit: mode == 1 ? new(t, 3) : null, descriptionEdit: mode == 2 ? new(t, "새 소개말") : null,
                    edit: mode == 3 ? new(t, 2, 2, t.Slots.JobRules) : null);
                f.Update(1000);
                check(f.Stage == RefreshStage.Stopped && h.QueryCalls + h.EditCalls + h.SubmitCalls == 0, "combined edit accepted");
            }
        });
        test("stale unique job draft detects flags, text, purpose, identity and participants", () => {
            var original = Target(48); var slots = original.Slots.Slots.ToArray();
            slots[16] = slots[16] with { MemberContentId = 99, JobId = 24 };
            foreach (var current in new[] { original with { ListingId = 124 }, original with { Description = "바뀜" },
                original with { ExtraCriteria = ListingUniqueJobs.WithEnabled(original.ExtraCriteria, true) },
                original with { ExtraCriteria = "700/7/True/6" }, original with { Criteria = original.Criteria.Replace("/321/4/", "/321/2/") },
                original with { Slots = original.Slots with { Slots = slots, SlotsFilled = 2 } } })
            {
                var h = new FakeHost { Target = current }; var f = new ListingRefresh(h);
                f.Request(1000, uniqueJobsEdit: new(original, true)); f.Update(1000); h.FinishQuery(); f.Update(1500);
                check(f.Stage == RefreshStage.Stopped && h.EditCalls + h.UniqueJobsEditCalls + h.SubmitCalls == 0, "stale draft opened editor");
            }
        });
        test("unique jobs failure, timeout, cancel, character change and editor closure never submit", () => {
            for (var mode = 0; mode < 7; mode++)
            {
                var (h, f) = Start();
                if (mode == 0) h.UniqueJobsEditFailed = true;
                if (mode == 1) h.UniqueJobsEditComplete = false;
                if (mode == 2) f.Cancel();
                if (mode == 3) h.EditorClosedRevision++;
                if (mode == 4) h.SelfId = 99;
                if (mode == 5) h.BlockReason = "이동 중";
                if (mode == 6) h.EditorVisible = false;
                f.Update(2500); f.Update(11000); f.Update(12000);
                check(!f.Busy && h.UniqueJobsEditCalls == 1 && h.SubmitCalls == 0, "failed edit continued");
            }
        });
        test("failed or thrown unique job clicks are not retried", () => {
            foreach (var throws in new[] { false, true })
            {
                var h = new FakeHost { Target = Target(), UniqueJobsEditAllowed = false, ThrowOnUniqueJobsEdit = throws };
                var f = new ListingRefresh(h); f.Request(1000, uniqueJobsEdit: new(h.Target, true));
                f.Update(1000); h.FinishQuery(); f.Update(1500);
                try { f.Update(2000); } catch (InvalidOperationException ex) { f.Fail(ex.Message); }
                f.Update(3000); f.Update(20000);
                check(!f.Busy && h.UniqueJobsEditCalls == 1 && h.SubmitCalls == 0, "failed click retried");
            }
        });
        test("old unique job bit or changed conditions and alliance slots fail pre/post-submit verification", () => {
            foreach (var afterSubmit in new[] { false, true })
            for (var mode = 0; mode < 8; mode++)
            {
                var (h, f) = Start(count: 48); var correct = h.UniqueJobsExpected!;
                var slots = correct.Slots.Slots.ToArray(); slots[47] = slots[47] with { SlotFlags = 0x100 };
                var wrong = mode switch {
                    0 => h.Target,
                    1 => correct with { ExtraCriteria = "700/39/True/6" }, // private bit changed too
                    2 => correct with { ExtraCriteria = "701/37/True/6" },
                    3 => correct with { Criteria = correct.Criteria.Replace("/321/4/1/", "/321/2/1/") },
                    4 => correct with { Criteria = correct.Criteria.Replace("/321/4/1/", "/321/4/0/") },
                    5 => correct with { Description = "바뀜" },
                    6 => correct with { Slots = correct.Slots with { Slots = slots } },
                    _ => correct with { ExtraCriteria = "700/37/True/3" }
                };
                if (!afterSubmit)
                {
                    h.UniqueJobsExpected = wrong; f.Update(2500);
                    check(f.Stage == RefreshStage.Stopped && h.SubmitCalls == 0, "wrong draft submitted");
                }
                else
                {
                    f.Update(2500); f.Update(4000); h.Target = wrong with { RemainingSeconds = 3598 }; h.FinishQuery(); f.Update(4500);
                    check(f.Stage == RefreshStage.Unconfirmed && h.SubmitCalls == 1 && f.Diagnostic.Contains("uniqueJobsResultContentsAgree=False"), "wrong fresh result accepted");
                }
            }
        });
        test("unique jobs confirmation does not require the displayed sixty-minute timer to increase", () => {
            var (h, f) = Start(false, seconds: 3600);
            f.Update(2500); f.Update(4000); h.Target = h.SubmittedTarget!; h.FinishQuery(); f.Update(4500);
            check(f.Stage == RefreshStage.Unconfirmed && f.Status.Contains("잡 중복 없음 설정이 반영된 것을 확인"), "verified toggle hidden");
            check(f.Diagnostic.Contains("uniqueJobsResultExpected=False; uniqueJobsResultActual=False"), "missing off evidence");
        });
        test("unique jobs does not act on a new-party editor", () => {
            var h = new FakeHost { Target = Target(), EditorActionMode = RecruitmentEditorMode.Create }; var f = new ListingRefresh(h);
            f.Request(1000, uniqueJobsEdit: new(h.Target, true)); f.Update(1000); h.FinishQuery(); f.Update(1500); f.Update(2000); f.Update(9000);
            check(!f.Busy && h.UniqueJobsEditCalls + h.SubmitCalls == 0, "create screen edited");
        });
    }
}
