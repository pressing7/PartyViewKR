using System.Runtime.InteropServices;
using PartyViewKR;

static class ListingObjectiveEditTests
{
    internal static void Run(Action<string, Action> test, Action<bool, string> check)
    {
        ListingObjectiveUiTests.Run(test, check);
        RefreshTarget Target(int count = 8, byte objective = 1)
        {
            var flags = Enumerable.Repeat(0xFFFFFFFFFFFFFFFEul, count).ToArray();
            var members = new ulong[count]; members[0] = 10;
            var jobs = new byte[count]; jobs[0] = 25;
            return new(10, 123, 321, "기존 소개말", $"4/321/{objective}/1/4/2/1/1/" + Convert.ToHexString(MemoryMarshal.AsBytes(flags.AsSpan())), 1800, count) {
                Slots = ListingSlots.Read(true, count, 1, count > 8 ? count / 8 : 1, count > 8, jobs, members, flags, id => id.ToString()),
                JobEditingAllowed = true, ExtraCriteria = "700/0",
            };
        }
        (FakeHost host, ListingRefresh flow, ListingObjectiveEdit edit) Start(int selected = 2, int count = 8, byte original = 1)
        {
            var h = new FakeHost { Target = Target(count, original) }; var f = new ListingRefresh(h);
            var edit = new ListingObjectiveEdit(h.Target, selected);
            f.Request(1000, objectiveEdit: edit); f.Update(1000); h.FinishQuery(); f.Update(1500); f.Update(2000);
            check(f.Stage == RefreshStage.EditingObjective && h.ObjectiveEditCalls == 1 &&
                h.DescriptionEditCalls + h.JobEditCalls + h.SubmitCalls == 0, "objective entry");
            return (h, f, edit);
        }
        test("objective menu indices match KR labels and native flags, including None=1", () => {
            var names = new[] { "설정 안 함", "완료 목적", "연습", "반복 공략" };
            byte[] flags = [1, 2, 4, 8];
            for (var i = 0; i < 4; i++)
            {
                check(ListingObjectives.Value(i) == flags[i] && ListingObjectives.Index(flags[i]) == i, "index/flag confusion");
                check(ListingObjectives.Label(flags[i]) == names[i], "KR label mismatch");
            }
            foreach (byte value in new byte[] { 0, 3, 16, 255 })
                check(ListingObjectives.Index(value) == -1 && ListingObjectives.Label(value) == "확인 불가", "unknown value labeled as None");
        });
        test("objective draft prefills current selection and does not mutate other settings", () => {
            var t = Target(objective: 2); var draft = new ListingObjectiveDraft(t);
            check(draft.SelectedIndex == 1 && !draft.Request.Valid, "wrong/changed prefill");
            draft.SelectedIndex = 3;
            var after = draft.Request.Expected(t);
            check(ListingObjectives.Read(t.Criteria) == 2 && ListingObjectives.Read(after.Criteria) == 8, "local edit mutated original");
            check(after.Description == t.Description && after.Slots == t.Slots && after.ExtraCriteria == t.ExtraCriteria, "unrelated fields changed");
            var beforeFields = t.Criteria.Split('/'); var afterFields = after.Criteria.Split('/');
            check(beforeFields.Where((_, i) => i != 2).SequenceEqual(afterFields.Where((_, i) => i != 2)), "beginner or other conditions changed");
        });
        test("manual trace keeps nested before/after order and stops without losing evidence", () => {
            var log = new ObjectiveTraceBuffer();
            log.Start(1000); log.Add(1010, "pre#1"); log.Add(1011, "pre#2");
            log.Add(1012, "post#2"); log.Add(1013, "post#1"); log.Stop("user_stopped");
            var saved = log.Snapshot(); log.Add(1014, "unexpected"); log.Stop("editor_closed");
            check(!log.Active && log.Reason == "user_stopped" && log.Snapshot() == saved, "stop lost/changed evidence");
            check(saved.IndexOf("pre#1") < saved.IndexOf("pre#2") && saved.IndexOf("post#2") < saved.IndexOf("post#1"), "nested order lost");
        });
        test("manual trace times out even when the user sends no more events", () => {
            var log = new ObjectiveTraceBuffer(); log.Start(1000); log.Add(1001, "first");
            log.Tick(1000 + ObjectiveTraceBuffer.DurationMs - 1); check(log.Active, "early timeout");
            log.Tick(1000 + ObjectiveTraceBuffer.DurationMs);
            check(!log.Active && log.Reason == "timeout" && log.Count == 1, "idle capture did not stop");
            log.Start(200000); log.Add(200000 + ObjectiveTraceBuffer.DurationMs, "late");
            check(!log.Active && log.Count == 0 && log.Reason == "timeout", "event beyond deadline accepted");
        });
        test("manual trace bounds a flood without evicting initial routing evidence", () => {
            var log = new ObjectiveTraceBuffer(); log.Start(1000);
            for (var i = 0; i <= ObjectiveTraceBuffer.MaxEntries; i++) log.Add(1010, "entry=" + i);
            check(!log.Active && log.Count == ObjectiveTraceBuffer.MaxEntries && log.Reason == "entry_limit", "unbounded trace");
            check(log.Snapshot().Contains("entry=0\n"), "first evidence evicted");
            log.Start(2000); log.Add(2001, "new_capture");
            check(log.Active && log.Count == 1 && !log.Snapshot().Contains("entry=0"), "restart mixed sessions");
        });
        test("the reported Practice value after a Loot request still blocks submission", () => {
            var (h, f, _) = Start(selected: 3, original: 2);
            h.ObjectiveExpected = h.Target with { Criteria = h.Target.Criteria.Replace("/321/2/", "/321/4/") };
            f.Update(2500); f.Update(3000); f.Update(20000);
            check(f.Stage == RefreshStage.Stopped && h.ObjectiveEditCalls == 1 && h.SubmitCalls == 0,
                "mismatched purpose submitted or selection retried");
        });
        test("every purpose transition including clearing sends once and verifies 4/8/24/48 listings", () => {
            foreach (var count in new[] { 4, 8, 24, 48 })
            foreach (byte original in new byte[] { 1, 2, 4, 8 })
            for (var selected = 0; selected < 4; selected++)
            {
                if (ListingObjectives.Value(selected) == original) continue;
                var (h, f, edit) = Start(selected, count, original);
                h.ObjectiveEditComplete = false; f.Update(2500); f.Update(3000);
                check(h.ObjectiveEditCalls == 1 && h.SubmitCalls == 0, "waiting resent or submitted");
                h.ObjectiveEditComplete = true; f.Update(3500); f.Update(4000);
                check(h.SubmitCalls == 1 && ListingObjectives.Read(h.SubmittedTarget!.Criteria) == edit.Value, "wrong/duplicate purpose submission");
                var submitted = h.SubmittedTarget!;
                check(h.SubmittedDescriptionArgument == null && submitted.Description == h.Target.Description, "description route used");
                f.Update(5000); h.Target = submitted with { RemainingSeconds = 3598 }; h.FinishQuery(); f.Update(5500);
                check(f.Stage == RefreshStage.Completed && h.SubmitCalls == 1 && h.ObjectiveEditCalls == 1, "transition unconfirmed or repeated");
            }
        });
        test("invalid, unchanged, unknown, PvP or combined objective requests dispatch no actions", () => {
            var t = Target();
            var cases = new[] { new ListingObjectiveEdit(t, -1), new(t, 4), new(t, 0),
                new(Target(objective: 0), 2), new(Target(objective: 3), 2),
                new(t with { Criteria = "malformed" }, 2), new(t with { JobEditingAllowed = false }, 2) };
            foreach (var edit in cases)
            {
                var h = new FakeHost { Target = t }; var f = new ListingRefresh(h);
                f.Request(1000, objectiveEdit: edit); f.Update(1000);
                check(!f.Busy && h.QueryCalls + h.EditCalls + h.ObjectiveEditCalls + h.SubmitCalls == 0, "invalid request dispatched");
            }
            var both = new ListingRefresh(new FakeHost { Target = t });
            both.Request(1000, descriptionEdit: new(t, "새 소개말"), objectiveEdit: new(t, 2)); both.Update(1000);
            check(both.Stage == RefreshStage.Stopped, "combined edit accepted");
        });
        test("stale objective, description, participants or flags block before opening the game editor", () => {
            var original = Target(48); var slots = original.Slots.Slots.ToArray();
            slots[16] = slots[16] with { MemberContentId = 99, JobId = 24 };
            foreach (var current in new[] { original with { ListingId = 124 }, original with { Description = "변경됨" },
                original with { Criteria = original.Criteria.Replace("/321/1/", "/321/2/") },
                original with { Slots = original.Slots with { Slots = slots, SlotsFilled = 2 } }, original with { ExtraCriteria = "changed" } })
            {
                var h = new FakeHost { Target = current }; var f = new ListingRefresh(h);
                f.Request(1000, objectiveEdit: new(original, 2)); f.Update(1000); h.FinishQuery(); f.Update(1500);
                check(f.Stage == RefreshStage.Stopped && h.EditCalls + h.ObjectiveEditCalls + h.SubmitCalls == 0, "stale edit opened");
            }
        });
        test("failed, timed out, cancelled, closed or rebound objective editing never submits", () => {
            for (var mode = 0; mode < 6; mode++)
            {
                var (h, f, _) = Start();
                if (mode == 0) h.ObjectiveEditFailed = true;
                if (mode == 1) h.ObjectiveEditComplete = false;
                if (mode == 2) f.Cancel();
                if (mode == 3) h.EditorClosedRevision++;
                if (mode == 4) h.SelfId = 99;
                if (mode == 5) h.BlockReason = "지역 이동 중";
                f.Update(2500); f.Update(11000); f.Update(12000);
                check(!f.Busy && h.ObjectiveEditCalls == 1 && h.SubmitCalls == 0, "failed operation continued");
            }
        });
        test("failed or thrown purpose operations are not retried", () => {
            foreach (var throws in new[] { false, true })
            {
                var h = new FakeHost { Target = Target(), ObjectiveEditAllowed = false, ThrowOnObjectiveEdit = throws }; var f = new ListingRefresh(h);
                f.Request(1000, objectiveEdit: new(h.Target, 2)); f.Update(1000); h.FinishQuery(); f.Update(1500);
                try { f.Update(2000); } catch (InvalidOperationException ex) { f.Fail(ex.Message); }
                f.Update(3000); f.Update(20000);
                check(!f.Busy && h.ObjectiveEditCalls == 1 && h.SubmitCalls == 0, "failed callback retried");
            }
        });
        test("old objective, changed beginner flag, description or alliance slot cannot pass verification", () => {
            foreach (var afterSubmit in new[] { false, true })
            for (var mode = 0; mode < 4; mode++)
            {
                var (h, f, _) = Start(count: 48);
                var correct = h.ObjectiveExpected!; var slots = correct.Slots.Slots.ToArray(); slots[47] = slots[47] with { SlotFlags = 0x100 };
                var wrong = mode switch {
                    0 => h.Target, 1 => correct with { Criteria = correct.Criteria.Replace("/321/4/1/", "/321/4/0/") },
                    2 => correct with { Description = "다른 내용" }, _ => correct with { Slots = correct.Slots with { Slots = slots } }
                };
                if (!afterSubmit)
                {
                    h.ObjectiveExpected = wrong; f.Update(2500);
                    check(f.Stage == RefreshStage.Stopped && h.SubmitCalls == 0, "wrong editor result submitted");
                }
                else
                {
                    f.Update(2500); f.Update(4000); h.Target = wrong with { RemainingSeconds = 3598 }; h.FinishQuery(); f.Update(4500);
                    check(f.Stage == RefreshStage.Unconfirmed && h.SubmitCalls == 1 && f.Diagnostic.Contains("objectiveResultContentsAgree=False"), "wrong final result accepted");
                }
            }
        });
        test("verified purpose and unchanged timer are reported separately", () => {
            var (h, f, _) = Start(selected: 0, original: 8);
            f.Update(2500); f.Update(4000); h.Target = h.SubmittedTarget!; h.FinishQuery(); f.Update(4500);
            check(f.Stage == RefreshStage.Unconfirmed && f.Status.Contains("수정한 목적이 등록된 것을 확인"), "verified purpose hidden");
            check(f.Diagnostic.Contains("objectiveResultExpected=1; objectiveResultActual=1"), "missing cleared purpose evidence");
        });
        test("objective edit does not run from a new-party screen", () => {
            var h = new FakeHost { Target = Target(), EditorActionMode = RecruitmentEditorMode.Create }; var f = new ListingRefresh(h);
            f.Request(1000, objectiveEdit: new(h.Target, 2)); f.Update(1000); h.FinishQuery(); f.Update(1500); f.Update(2000); f.Update(9000);
            check(!f.Busy && h.ObjectiveEditCalls + h.SubmitCalls == 0, "purpose sent to create screen");
        });
    }
}
