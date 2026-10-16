using System.Runtime.InteropServices;
using PartyViewKR;

static class ListingBeginnerEditTests
{
    internal static void Run(Action<string, Action> test, Action<bool, string> check)
    {
        RefreshTarget Target(int count = 8, byte beginner = 0, byte objective = 4)
        {
            var flags = Enumerable.Repeat(0xFFFFFFFFFFFFFFFEul, count).ToArray();
            var members = new ulong[count]; members[0] = 10;
            var jobs = new byte[count]; jobs[0] = 25;
            return new(10, 123, 321, "기존 소개말", $"4/321/{objective}/{beginner}/4/2/1/1/" + Convert.ToHexString(MemoryMarshal.AsBytes(flags.AsSpan())), 1800, count) {
                Slots = ListingSlots.Read(true, count, 1, count > 8 ? count / 8 : 1, count > 8, jobs, members, flags, id => id.ToString()),
                JobEditingAllowed = true, ExtraCriteria = "700/0",
            };
        }
        (FakeHost host, ListingRefresh flow) Start(bool enabled = true, int count = 8, byte objective = 4, int seconds = 1800)
        {
            var h = new FakeHost { Target = Target(count, enabled ? (byte)0 : (byte)1, objective) with { RemainingSeconds = seconds } };
            var f = new ListingRefresh(h);
            f.Request(1000, beginnerEdit: new(h.Target, enabled)); f.Update(1000); h.FinishQuery(); f.Update(1500); f.Update(2000);
            check(f.Stage == RefreshStage.EditingBeginner && h.BeginnerEditCalls == 1 &&
                h.DescriptionEditCalls + h.JobEditCalls + h.ObjectiveEditCalls + h.SubmitCalls == 0, "beginner entry");
            return (h, f);
        }
        test("beginner draft uses known detail state and preserves purpose, text and slots", () => {
            foreach (byte value in new byte[] { 0, 1 })
            {
                var t = Target(beginner: value); var draft = new ListingBeginnerDraft(t);
                check(draft.Enabled == (value == 1) && !draft.Request.Valid, "prefill should not request a change");
                draft.Enabled = !draft.Enabled;
                var expected = draft.Request.Expected(t);
                check(ListingBeginners.Read(t.Criteria) == (value == 1) && ListingBeginners.Read(expected.Criteria) == draft.Enabled, "original mutated");
                check(t.Criteria.Split('/').Where((_, i) => i != 3).SequenceEqual(expected.Criteria.Split('/').Where((_, i) => i != 3)), "another criterion changed");
                check(expected.Description == t.Description && expected.Slots == t.Slots && expected.ExtraCriteria == t.ExtraCriteria, "text/slots changed");
            }
            foreach (byte value in new byte[] { 2, 18, 19, 255 })
                check(ListingBeginners.Decode(value) == null && ListingBeginners.Read(Target(beginner: value).Criteria) == null, "unknown flag treated as checked");
            check(ListingBeginners.Read("malformed") == null, "malformed criteria accepted");
        });
        test("beginner UI on and off each dispatch one click and require both UI and stored state", () => {
            foreach (var enabled in new[] { false, true })
            {
                byte old = enabled ? (byte)0 : (byte)1, wanted = enabled ? (byte)1 : (byte)0;
                var ui = new ListingBeginnerUi(!enabled, enabled);
                check(ui.Next(new(!enabled, old), 1000) == BeginnerUiAction.Click, "missing initial click");
                check(ui.Next(new(enabled, old), 1500) == BeginnerUiAction.Wait, "UI-only change accepted");
                check(ui.Next(new(enabled, old), 2000) == BeginnerUiAction.Wait, "click repeated while waiting");
                check(ui.Next(new(enabled, wanted), 2500) == BeginnerUiAction.Complete, "matching states not confirmed");
                check(ui.Next(new(enabled, wanted), 3000) == BeginnerUiAction.Complete, "completed operation clicked again");
                check(ui.Next(new(enabled, old), 3500) == BeginnerUiAction.Failed, "late reversal accepted");
            }
        });
        test("beginner UI unknown, initially stale, reversed and UI-only timeout states fail without retry", () => {
            var unchanged = new ListingBeginnerUi(true, true);
            check(unchanged.Next(new(true, 1), 1000) == BeginnerUiAction.Failed, "no-op clicked");
            var stale = new ListingBeginnerUi(false, true);
            check(stale.Next(new(true, 0), 1000) == BeginnerUiAction.Failed, "stale UI clicked");
            foreach (var mode in new[] { 0, 1, 2 })
            {
                var ui = new ListingBeginnerUi(false, true); ui.Next(new(false, 0), 1000);
                var wrong = mode switch { 0 => new BeginnerUiState(true, 2), 1 => new(false, 1), _ => new(true, 0) };
                check(ui.Next(wrong, 4001) == BeginnerUiAction.Failed, "unverified UI succeeded");
                check(ui.Next(new(true, 1), 4500) == BeginnerUiAction.Failed, "failure retried");
            }
        });
        test("beginner on/off refreshes 4/8/24/48 once, preserving every purpose", () => {
            foreach (var enabled in new[] { true, false })
            foreach (var count in new[] { 4, 8, 24, 48 })
            foreach (byte objective in new byte[] { 1, 2, 4, 8 })
            {
                var (h, f) = Start(enabled, count, objective);
                h.BeginnerEditComplete = false; f.Update(2500); f.Update(3000);
                check(h.BeginnerEditCalls == 1 && h.SubmitCalls == 0, "waiting retried or submitted");
                h.BeginnerEditComplete = true; f.Update(3500); f.Update(4000);
                var submitted = h.SubmittedTarget!;
                check(h.SubmitCalls == 1 && ListingBeginners.Read(submitted.Criteria) == enabled, "wrong or duplicate submission");
                check(ListingObjectives.Read(submitted.Criteria) == objective && submitted.Description == h.Target.Description &&
                    h.SubmittedDescriptionArgument == null && ListingJobEdit.SameSlots(h.Target.Slots, submitted.Slots), "unrelated edit");
                f.Update(5000); h.Target = submitted with { RemainingSeconds = 3598 }; h.FinishQuery(); f.Update(5500);
                check(f.Stage == RefreshStage.Completed && h.BeginnerEditCalls == 1 && h.SubmitCalls == 1, "final verification failed");
            }
        });
        test("unchanged, unknown, malformed or PvP beginner requests dispatch nothing", () => {
            foreach (var target in new[] { Target(beginner: 1), Target(beginner: 2), Target() with { Criteria = "malformed" },
                Target() with { JobEditingAllowed = false } })
            {
                var h = new FakeHost { Target = target }; var f = new ListingRefresh(h);
                f.Request(1000, beginnerEdit: new(target, true)); f.Update(1000);
                check(f.Stage == RefreshStage.Stopped && h.QueryCalls + h.EditCalls + h.BeginnerEditCalls + h.SubmitCalls == 0, "invalid request dispatched");
            }
        });
        test("beginner cannot be combined with a purpose, description or job edit", () => {
            var t = Target();
            for (var mode = 0; mode < 3; mode++)
            {
                var h = new FakeHost { Target = t }; var f = new ListingRefresh(h);
                f.Request(1000, beginnerEdit: new(t, true), objectiveEdit: mode == 0 ? new(t, 3) : null,
                    descriptionEdit: mode == 1 ? new(t, "새 소개말") : null,
                    edit: mode == 2 ? new(t, 2, 2, t.Slots.JobRules) : null);
                f.Update(1000);
                check(f.Stage == RefreshStage.Stopped && h.QueryCalls + h.EditCalls + h.SubmitCalls == 0, "combined request dispatched");
            }
        });
        test("stale beginner settings, purpose, listing, comment or participant stop before opening", () => {
            var original = Target(48); var members = original.Slots.Slots.ToArray();
            members[16] = members[16] with { MemberContentId = 99, JobId = 24 };
            foreach (var current in new[] { original with { ListingId = 999 }, original with { Description = "다른 소개말" },
                original with { Criteria = original.Criteria.Replace("/321/4/0/", "/321/4/1/") },
                original with { Criteria = original.Criteria.Replace("/321/4/0/", "/321/2/0/") },
                original with { Slots = original.Slots with { Slots = members, SlotsFilled = 2 } } })
            {
                var h = new FakeHost { Target = current }; var f = new ListingRefresh(h);
                f.Request(1000, beginnerEdit: new(original, true)); f.Update(1000); h.FinishQuery(); f.Update(1500);
                check(f.Stage == RefreshStage.Stopped && h.EditCalls + h.BeginnerEditCalls + h.SubmitCalls == 0, "stale request opened editor");
            }
        });
        test("failed, delayed, cancelled, closed or rebound beginner editing never submits", () => {
            for (var mode = 0; mode < 7; mode++)
            {
                var (h, f) = Start();
                if (mode == 0) h.BeginnerEditFailed = true;
                if (mode == 1) h.BeginnerEditComplete = false;
                if (mode == 2) f.Cancel();
                if (mode == 3) h.EditorClosedRevision++;
                if (mode == 4) h.SelfId = 99;
                if (mode == 5) h.BlockReason = "지역 이동 중";
                if (mode == 6) h.EditorVisible = false;
                f.Update(2500); f.Update(11000); f.Update(12000);
                check(!f.Busy && h.BeginnerEditCalls == 1 && h.SubmitCalls == 0, "failed operation submitted or retried");
            }
        });
        test("failed or thrown beginner operations are not repeated", () => {
            foreach (var throws in new[] { false, true })
            {
                var h = new FakeHost { Target = Target(), BeginnerEditAllowed = false, ThrowOnBeginnerEdit = throws };
                var f = new ListingRefresh(h); f.Request(1000, beginnerEdit: new(h.Target, true));
                f.Update(1000); h.FinishQuery(); f.Update(1500);
                try { f.Update(2000); } catch (InvalidOperationException ex) { f.Fail(ex.Message); }
                f.Update(3000); f.Update(20000);
                check(!f.Busy && h.BeginnerEditCalls == 1 && h.SubmitCalls == 0, "failed dispatch retried");
            }
        });
        test("unchanged flag or changed purpose, comment or alliance slot fail before and after submit", () => {
            foreach (var afterSubmit in new[] { false, true })
            for (var mode = 0; mode < 5; mode++)
            {
                var (h, f) = Start(count: 48); var correct = h.BeginnerExpected!;
                var slots = correct.Slots.Slots.ToArray(); slots[47] = slots[47] with { SlotFlags = 0x100 };
                var wrong = mode switch {
                    0 => h.Target,
                    1 => correct with { Criteria = correct.Criteria.Replace("/321/4/1/", "/321/2/1/") },
                    2 => correct with { Description = "다른 소개말" },
                    3 => correct with { Slots = correct.Slots with { Slots = slots } },
                    _ => correct with { Criteria = correct.Criteria.Replace("/321/4/1/", "/321/4/2/") }
                };
                if (!afterSubmit)
                {
                    h.BeginnerExpected = wrong; f.Update(2500);
                    check(f.Stage == RefreshStage.Stopped && h.SubmitCalls == 0, "incorrect draft submitted");
                }
                else
                {
                    f.Update(2500); f.Update(4000); h.Target = wrong with { RemainingSeconds = 3598 }; h.FinishQuery(); f.Update(4500);
                    check(f.Stage == RefreshStage.Unconfirmed && h.SubmitCalls == 1 && f.Diagnostic.Contains("beginnerResultContentsAgree=False"), "incorrect fresh result accepted");
                }
            }
        });
        test("beginner confirmation is reported independently of unchanged sixty-minute timer", () => {
            var (h, f) = Start(false, seconds: 3600);
            f.Update(2500); f.Update(4000); h.Target = h.SubmittedTarget!; h.FinishQuery(); f.Update(4500);
            check(f.Stage == RefreshStage.Unconfirmed && f.Status.Contains("초보자 환영 설정이 반영된 것을 확인"), "verified flag hidden");
            check(f.Diagnostic.Contains("beginnerResultExpected=False; beginnerResultActual=False"), "missing off verification");
        });
        test("beginner editing does not run in a new-party screen", () => {
            var h = new FakeHost { Target = Target(), EditorActionMode = RecruitmentEditorMode.Create }; var f = new ListingRefresh(h);
            f.Request(1000, beginnerEdit: new(h.Target, true)); f.Update(1000); h.FinishQuery(); f.Update(1500); f.Update(2000); f.Update(9000);
            check(!f.Busy && h.BeginnerEditCalls + h.SubmitCalls == 0, "new-party screen edited");
        });
    }
}
