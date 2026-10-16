using System.Runtime.InteropServices;
using PartyViewKR;
using PartyViewKR.Compatibility;

static class ListingCompletionEditTests
{
    internal static void Run(Action<string, Action> test, Action<bool, string> check)
    {
        RefreshTarget Target(byte completion = 1, int count = 8)
        {
            var flags = Enumerable.Repeat(0xFFFFFFFFFFFFFFFEul, count).ToArray();
            var members = new ulong[count]; members[0] = 10; var jobs = new byte[count]; jobs[0] = 25;
            var groups = count > 8 ? count / 8 : 1;
            return new(10, 123, 321, "기존 소개말", $"4/321/4/1/{completion}/2/1/1/" + Convert.ToHexString(MemoryMarshal.AsBytes(flags.AsSpan())), 1800, count) {
                Slots = ListingSlots.Read(true, count, 1, groups, count > 8, jobs, members, flags, id => id.ToString()),
                JobEditingAllowed = true, ExtraCriteria = $"700/{(count > 8 ? 39 : 35)}/{count > 8}/{groups}"
            };
        }
        ListingCompletionEdit Edit(RefreshTarget t, byte value) => new(t, value != 1, ListingCompletions.Index(value));
        CompletionUiState Closed(byte stored, bool? enabled = null, int? index = null, bool canSelect = true) =>
            new(enabled ?? stored != 1, index ?? Math.Max(0, ListingCompletions.Index(stored)),
                index ?? Math.Max(0, ListingCompletions.Index(stored)), stored, false, false, false, 2, canSelect);
        (FakeHost h, ListingRefresh f) Start(byte original = 1, byte wanted = 2, int count = 8, int seconds = 1800)
        {
            var h = new FakeHost { Target = Target(original, count) with { RemainingSeconds = seconds } };
            var f = new ListingRefresh(h); f.Request(1000, completionEdit: Edit(h.Target, wanted));
            f.Update(1000); h.FinishQuery(); f.Update(1500); f.Update(2000);
            check(f.Stage == RefreshStage.EditingCompletion && h.CompletionEditCalls == 1 && h.SubmitCalls == 0, "wrong edit route");
            return (h, f);
        }
        test("completion flags match the KR enum rather than callback ordinals", () => {
            using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "native-contract.json"));
            var c = NativeContract.Parse(stream);
            foreach (var (name, value) in new[] { ("None", ListingCompletions.None), ("DutyComplete", ListingCompletions.Complete), ("DutyIncomplete", ListingCompletions.Incomplete) })
                check(c.Entries[$"field:FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentLookingForGroup+CompletionStatus.{name}"].Contains($"constant=Byte:{value:X2};"), "incorrect native flag");
            check(ListingCompletions.Value(false, -1) == 1 && ListingCompletions.Value(true, 0) == 2 && ListingCompletions.Value(true, 1) == 4, "wrong checked/index mapping");
            check(ListingCompletions.Label(2) == "공략완료" && ListingCompletions.Label(4) == "미완료", "KR labels reversed");
            check(!ListingCompletions.Supported(8) && ListingCompletions.Label(8).Contains("미지원"), "unobserved option silently editable");
        });
        test("completion draft preserves the current checkbox and selected requirement", () => {
            foreach (byte original in new byte[] { 1, 2, 4 })
            {
                var t = Target(original); var d = new ListingCompletionDraft(t);
                check(d.Enabled == (original != 1) && !d.Request.Valid, "wrong/changed draft prefill");
                check(d.SelectedIndex == (original == 4 ? 1 : 0), "wrong dropdown prefill");
                d.Enabled = true; d.SelectedIndex = original == 4 ? 0 : 1;
                var changed = d.Request.Expected(t);
                check(changed.Criteria.Split('/').Where((_, i) => i != 4).SequenceEqual(t.Criteria.Split('/').Where((_, i) => i != 4)) &&
                    changed.Description == t.Description && changed.ExtraCriteria == t.ExtraCriteria && changed.Slots == t.Slots, "another condition mutated");
            }
        });
        test("completion checkbox on and off require both UI state and stored condition", () => {
            var on = new ListingCompletionUi(1, 2);
            check(on.Next(Closed(1, canSelect: false), 1000) == CompletionUiAction.Toggle, "no enable");
            check(on.Next(Closed(1, enabled: true), 1500) == CompletionUiAction.Wait, "UI-only enable accepted");
            check(on.Next(Closed(2), 2000) == CompletionUiAction.Complete, "stored complete not accepted");
            foreach (byte original in new byte[] { 2, 4 })
            {
                var off = new ListingCompletionUi(original, 1);
                check(off.Next(Closed(original, canSelect: false), 1000) == CompletionUiAction.Toggle, "disabled dropdown prevented uncheck");
                check(off.Next(Closed(original, enabled: false, canSelect: false), 1500) == CompletionUiAction.Wait, "UI-only off accepted");
                var none = Closed(1, index: original == 4 ? 1 : 0, canSelect: false);
                check(off.Next(none, 2000) == CompletionUiAction.Complete && off.Next(none, 2500) == CompletionUiAction.Complete, "off changed dropdown or repeated toggle");
            }
        });
        test("completion dropdown uses open then click and native selection for both observed indices", () => {
            foreach (var (before, after) in new[] { ((byte)2, (byte)4), ((byte)4, (byte)2) })
            {
                var ui = new ListingCompletionUi(before, after); var old = Closed(before);
                check(ui.Next(old, 1000) == CompletionUiAction.Open, "dropdown not opened");
                check(ui.Next(old with { Open = true, Pending = true, DropdownChecked = true }, 1500) == CompletionUiAction.Wait, "clicked pending list");
                check(ui.Next(old with { Open = true, DropdownChecked = true }, 2000) == CompletionUiAction.ClickItem, "item click missing");
                check(ui.Next(old with { Pending = true, DropdownChecked = true }, 2000) == CompletionUiAction.SelectItem, "selection missing");
                check(ui.Next(Closed(after) with { Pending = true }, 2500) == CompletionUiAction.Wait, "submitted during close transition");
                check(ui.Next(Closed(after), 3000) == CompletionUiAction.Complete, "final selection not verified");
                check(ui.Next(Closed(after), 3500) == CompletionUiAction.Complete, "completed run repeated action");
            }
        });
        test("enabling completion selects the requested requirement only when the restored choice differs", () => {
            var ui = new ListingCompletionUi(1, 4);
            check(ui.Next(Closed(1), 1000) == CompletionUiAction.Toggle, "no toggle");
            check(ui.Next(Closed(2, canSelect: false), 1500) == CompletionUiAction.Wait, "selected disabled control");
            check(ui.Next(Closed(2), 2000) == CompletionUiAction.Open, "default complete did not open for incomplete");
            var restored = new ListingCompletionUi(1, 4);
            restored.Next(Closed(1, index: 1), 1000);
            check(restored.Next(Closed(4), 1500) == CompletionUiAction.Complete, "correct restored choice needlessly selected again");
        });
        test("unknown completion states, unavailable items and inconsistent initial UI never mutate", () => {
            foreach (var state in new[] { Closed(2) with { CanSelect = false }, Closed(2) with { Count = 1 },
                Closed(2) with { Open = true }, Closed(2) with { ListIndex = 1 }, Closed(2) with { Stored = 8 }, Closed(2, enabled: false) })
            {
                var ui = new ListingCompletionUi(2, 4);
                check(ui.Next(state, 1000) == CompletionUiAction.Failed, "unavailable initial state acted");
                check(ui.Next(Closed(2), 5000) == CompletionUiAction.Failed, "failed state retried");
            }
        });
        test("completion missing storage, open timeout and changed selection fail without retry", () => {
            var toggle = new ListingCompletionUi(1, 2); toggle.Next(Closed(1), 1000);
            check(toggle.Next(Closed(1, enabled: true), 5000) == CompletionUiAction.Failed, "enable not bounded");
            var ui = new ListingCompletionUi(2, 4); ui.Next(Closed(2), 1000);
            check(ui.Next(Closed(2), 2000) == CompletionUiAction.Wait && ui.Next(Closed(2), 5000) == CompletionUiAction.Failed, "open retried or not bounded");
            foreach (var wrong in new[] { Closed(4) with { Stored = 2 }, Closed(2) with { Stored = 4 }, Closed(4) with { Pending = true } })
            {
                ui = new ListingCompletionUi(2, 4); var old = Closed(2);
                ui.Next(old, 1000); ui.Next(old with { Open = true, DropdownChecked = true }, 1500);
                ui.Next(old with { Pending = true, DropdownChecked = true }, 1500);
                check(ui.Next(wrong, 2000) == CompletionUiAction.Wait, "partial result accepted");
                check(ui.Next(wrong, 5000) == CompletionUiAction.Failed, "partial result timeout absent");
            }
            ui = new ListingCompletionUi(1, 2); ui.Next(Closed(1), 1000); ui.Next(Closed(2), 1500);
            check(ui.Next(Closed(4), 2000) == CompletionUiAction.Failed, "changed verified selection accepted");
        });
        test("all six completion transitions preserve unrelated settings and submit once on every layout", () => {
            foreach (var count in new[] { 4, 8, 24, 48 })
            foreach (byte original in new byte[] { 1, 2, 4 })
            foreach (byte wanted in new byte[] { 1, 2, 4 })
            {
                if (original == wanted) continue;
                var (h, f) = Start(original, wanted, count);
                h.CompletionEditComplete = false; f.Update(2500); f.Update(3000);
                check(h.CompletionEditCalls == 1 && h.SubmitCalls == 0, "early/repeated operation");
                h.CompletionEditComplete = true; f.Update(3500); f.Update(4000); var submitted = h.SubmittedTarget!;
                check(h.SubmitCalls == 1 && ListingCompletions.Read(submitted.Criteria) == wanted &&
                    submitted.ExtraCriteria == h.Target.ExtraCriteria && submitted.Description == h.Target.Description &&
                    ListingJobEdit.SameSlots(submitted.Slots, h.Target.Slots) && h.SubmittedDescriptionArgument == null, "wrong submitted conditions");
                f.Update(5000); h.Target = submitted with { RemainingSeconds = 3598 }; h.FinishQuery(); f.Update(5500);
                check(f.Stage == RefreshStage.Completed && h.SubmitCalls == 1 && f.Diagnostic.Contains("completionResultContentsAgree=True"), "fresh verification failed");
            }
        });
        test("unknown, unchanged or unsupported completion requests never query", () => {
            foreach (var t in new[] { Target(8), Target(0), Target(3), Target(2), Target() with { JobEditingAllowed = false }, Target() with { Criteria = "" } })
            {
                var h = new FakeHost { Target = t }; var f = new ListingRefresh(h);
                f.Request(1000, completionEdit: new(t, true, 0)); f.Update(1000);
                check(f.Stage == RefreshStage.Stopped && h.QueryCalls + h.EditCalls + h.SubmitCalls == 0, "invalid completion dispatched");
            }
        });
        test("completion cannot be combined with any of the six other edits", () => {
            var t = Target();
            for (var mode = 0; mode < 6; mode++)
            {
                var h = new FakeHost { Target = t }; var f = new ListingRefresh(h);
                f.Request(1000, completionEdit: new(t, true, 0), privateEdit: mode == 0 ? new(t, true, "0000") : null,
                    uniqueJobsEdit: mode == 1 ? new(t, false) : null, beginnerEdit: mode == 2 ? new(t, false) : null,
                    objectiveEdit: mode == 3 ? new(t, 3) : null, descriptionEdit: mode == 4 ? new(t, "수정") : null,
                    edit: mode == 5 ? new(t, 2, 2, t.Slots.JobRules) : null);
                f.Update(1000);
                check(f.Stage == RefreshStage.Stopped && h.QueryCalls + h.EditCalls + h.SubmitCalls == 0, "combined edit dispatched");
            }
        });
        test("stale completion drafts detect changed participants, flags, text and identity", () => {
            var original = Target(1, 48); var slots = original.Slots.Slots.ToArray(); slots[47] = slots[47] with { MemberContentId = 99 };
            foreach (var current in new[] { original with { ListingId = 124 }, original with { Description = "다름" },
                original with { ExtraCriteria = "700/37/True/6" }, original with { Criteria = Target(4, 48).Criteria },
                original with { Slots = original.Slots with { Slots = slots } } })
            {
                var h = new FakeHost { Target = current }; var f = new ListingRefresh(h);
                f.Request(1000, completionEdit: Edit(original, 2)); f.Update(1000); h.FinishQuery(); f.Update(1500);
                check(f.Stage == RefreshStage.Stopped && h.EditCalls + h.CompletionEditCalls + h.SubmitCalls == 0, "stale draft edited");
            }
        });
        test("completion failure, timeout, cancellation, character change or closure never submits", () => {
            for (var mode = 0; mode < 7; mode++)
            {
                var (h, f) = Start();
                if (mode == 0) h.CompletionEditFailed = true;
                if (mode == 1) h.CompletionEditComplete = false;
                if (mode == 2) f.Cancel();
                if (mode == 3) h.EditorClosedRevision++;
                if (mode == 4) h.SelfId = 99;
                if (mode == 5) h.BlockReason = "이동 중";
                if (mode == 6) h.EditorVisible = false;
                f.Update(2500); f.Update(11000); f.Update(12000);
                check(!f.Busy && h.CompletionEditCalls == 1 && h.SubmitCalls == 0, "failed edit continued");
            }
        });
        test("failed or thrown completion dispatch never retries", () => {
            foreach (var throws in new[] { false, true })
            {
                var h = new FakeHost { Target = Target(), CompletionEditAllowed = false, ThrowOnCompletionEdit = throws };
                var f = new ListingRefresh(h); f.Request(1000, completionEdit: Edit(h.Target, 2)); f.Update(1000); h.FinishQuery(); f.Update(1500);
                try { f.Update(2000); } catch (InvalidOperationException ex) { f.Fail(ex.Message); }
                f.Update(3000); f.Update(15000);
                check(!f.Busy && h.CompletionEditCalls == 1 && h.SubmitCalls == 0, "failed native operation repeated");
            }
        });
        test("wrong completion, purpose, flags or slot results cannot pass pre/post-submit verification", () => {
            foreach (var afterSubmit in new[] { false, true })
            for (var mode = 0; mode < 5; mode++)
            {
                var (h, f) = Start(count: 48); var correct = h.CompletionExpected!;
                var slots = correct.Slots.Slots.ToArray(); slots[47] = slots[47] with { SlotFlags = 0x100 };
                var wrong = mode switch {
                    0 => h.Target,
                    1 => correct with { Criteria = correct.Criteria.Replace("/321/4/", "/321/2/") },
                    2 => correct with { ExtraCriteria = "700/37/True/6" },
                    3 => correct with { Description = "바뀜" },
                    _ => correct with { Slots = correct.Slots with { Slots = slots } }
                };
                if (!afterSubmit)
                { h.CompletionExpected = wrong; f.Update(2500); check(f.Stage == RefreshStage.Stopped && h.SubmitCalls == 0, "wrong draft submitted"); }
                else
                {
                    f.Update(2500); f.Update(4000); h.Target = wrong with { RemainingSeconds = 3598 }; h.FinishQuery(); f.Update(4500);
                    check(f.Stage == RefreshStage.Unconfirmed && h.SubmitCalls == 1 && f.Diagnostic.Contains("completionResultContentsAgree=False"), "wrong fresh result accepted");
                }
            }
        });
        test("completion setting confirmation is separate from timer and create mode is blocked", () => {
            var (h, f) = Start(seconds: 3600); f.Update(2500); f.Update(4000); h.Target = h.SubmittedTarget!; h.FinishQuery(); f.Update(4500);
            check(f.Stage == RefreshStage.Unconfirmed && f.Status.Contains("수정한 완료조건이 등록"), "confirmed condition hidden");
            h = new FakeHost { Target = Target(), EditorActionMode = RecruitmentEditorMode.Create }; f = new ListingRefresh(h);
            f.Request(1000, completionEdit: Edit(h.Target, 2)); f.Update(1000); h.FinishQuery(); f.Update(1500); f.Update(2000); f.Update(9000);
            check(!f.Busy && h.CompletionEditCalls + h.SubmitCalls == 0, "create screen edited");
        });
    }
}
