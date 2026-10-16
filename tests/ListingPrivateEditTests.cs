using System.Runtime.InteropServices;
using PartyViewKR;
using PartyViewKR.Compatibility;

static class ListingPrivateEditTests
{
    internal static void Run(Action<string, Action> test, Action<bool, string> check)
    {
        RefreshTarget Target(bool enabled = false, int count = 8)
        {
            var flags = Enumerable.Repeat(0xFFFFFFFFFFFFFFFEul, count).ToArray();
            var members = new ulong[count]; members[0] = 10;
            var jobs = new byte[count]; jobs[0] = 25;
            var groups = count > 8 ? count / 8 : 1;
            var join = 33 | (enabled ? 2 : 0) | (count > 8 ? 4 : 0);
            return new(10, 123, 321, "기존 소개말", "4/321/4/1/4/2/1/1/" + Convert.ToHexString(MemoryMarshal.AsBytes(flags.AsSpan())), 1800, count) {
                Slots = ListingSlots.Read(true, count, 1, groups, count > 8, jobs, members, flags, id => id.ToString()),
                JobEditingAllowed = true, ExtraCriteria = $"700/{join}/{count > 8}/{groups}"
            };
        }
        (FakeHost h, ListingRefresh f) Start(bool original = false, bool enabled = true, int count = 8, int seconds = 1800)
        {
            var h = new FakeHost { Target = Target(original, count) with { RemainingSeconds = seconds } };
            var f = new ListingRefresh(h); f.Request(1000, privateEdit: new(h.Target, enabled, enabled ? "0123" : ""));
            f.Update(1000); h.FinishQuery(); f.Update(1500); f.Update(2000);
            check(f.Stage == RefreshStage.EditingPrivate && h.PrivateEditCalls == 1 && h.SubmitCalls == 0, "wrong private route");
            return (h, f);
        }
        test("private PIN requires exactly four ASCII digits and keeps leading zeroes", () => {
            foreach (var pin in new[] { "0001", "0123", "1234", "5678", "9999" })
            {
                var d = new ListingPrivateDraft(Target()) { Enabled = true, Password = pin };
                check(d.Request.Valid && d.Request.Password == pin && d.Request.PasswordValue.ToString("D4") == pin, "four-digit PIN lost");
                check(!d.Request.ToString().Contains(pin), "PIN leaked in record formatting");
            }
            foreach (var pin in new[] { "", "1", "123", "12345", "1234 ", " 1234", "-123", "+123", "1.23", "１２３４", "١٢٣٤", "0000", "12\n3" })
            {
                var h = new FakeHost { Target = Target() }; var f = new ListingRefresh(h);
                f.Request(1000, privateEdit: new(h.Target, true, pin)); f.Update(1000);
                check(f.Stage == RefreshStage.Stopped && h.QueryCalls + h.EditCalls + h.SubmitCalls == 0, "invalid PIN began editing");
            }
        });
        test("private flag matches native metadata and preserves all unrelated join bits", () => {
            using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "native-contract.json"));
            var baseline = NativeContract.Parse(stream);
            check(baseline.Entries["field:FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentLookingForGroup+JoinCondition.Private"].Contains("constant=Byte:02;"), "wrong native private bit");
            for (var flags = 0; flags <= 255; flags++)
            foreach (var enabled in new[] { false, true })
            {
                var before = $"700/{flags}/False/1"; var after = ListingPrivate.WithEnabled(before, enabled);
                ListingUniqueJobs.TryFlags(after, out var actual);
                check(ListingPrivate.Read(after) == enabled && ((flags ^ actual) & ~ListingPrivate.Flag) == 0, "unrelated bit changed");
                check(after.Split('/').Where((_, i) => i != 1).SequenceEqual(before.Split('/').Where((_, i) => i != 1)), "extra criteria changed");
            }
            check(ListingPrivate.StoredEnabled(10000) == false && ListingPrivate.StoredEnabled(0) == true &&
                ListingPrivate.StoredEnabled(123) == true && ListingPrivate.StoredEnabled(10001) == null, "unknown stored state guessed");
        });
        test("private drafts never invent a password and off needs no password", () => {
            foreach (var original in new[] { false, true })
            {
                var d = new ListingPrivateDraft(Target(original));
                check(d.Enabled == original && d.Password == "" && !d.Request.Valid, "draft guessed a PIN");
                d.Enabled = true; d.Password = "0123"; check(d.Request.Valid, "password-only change rejected");
                d.Enabled = false; check(d.Request.Password == "" && d.Request.Valid == original, "off requires PIN or public no-op allowed");
            }
        });
        test("private enable waits for checkbox storage then sends one numeric change", () => {
            var ui = new ListingPrivateUi(false, true, 123);
            check(ui.Next(new(false, 10000, 0, false), 1000) == PrivateUiAction.Toggle, "missing toggle");
            check(ui.Next(new(true, 10000, 0, false), 1200) == PrivateUiAction.Wait, "input before enable stored");
            check(ui.Next(new(true, 0, 0, true), 1500) == PrivateUiAction.Input, "numeric change missing");
            check(ui.Next(new(true, 0, 123, true), 2000) == PrivateUiAction.Wait, "UI alone confirmed");
            check(ui.Next(new(true, 123, 123, true), 2500) == PrivateUiAction.Complete, "stored/UI match not confirmed");
            check(ui.Next(new(true, 123, 123, true), 3000) == PrivateUiAction.Complete, "mutation repeated");
            check(ui.Next(new(true, 456, 123, true), 3500) == PrivateUiAction.Failed, "final PIN reversal ignored");
        });
        test("private disable emits no numeric change and waits for public sentinel", () => {
            var ui = new ListingPrivateUi(true, false, 0);
            check(ui.Next(new(true, 123, 123, true), 1000) == PrivateUiAction.Toggle, "missing disable");
            check(ui.Next(new(false, 123, 0, false), 1500) == PrivateUiAction.Wait, "UI off alone confirmed");
            check(ui.Next(new(false, 10000, 0, false), 2000) == PrivateUiAction.Complete, "public sentinel not accepted");
            check(ui.Next(new(false, 10000, 0, false), 2500) == PrivateUiAction.Complete, "off repeated a mutation");
        });
        test("password-only updates never toggle off and on or retry unchanged input", () => {
            foreach (ushort previous in new ushort[] { 123, 5678 })
            {
                var ui = new ListingPrivateUi(true, true, 123);
                check(ui.Next(new(true, previous, previous, true), 1000) == PrivateUiAction.Input, "password edit toggled checkbox");
                check(ui.Next(new(true, 123, 123, true), 1001) == PrivateUiAction.Wait, "no settle interval");
                check(ui.Next(new(true, 123, 123, true), 1500) == PrivateUiAction.Complete, "PIN edit not complete");
            }
        });
        test("unknown state, wrong UI, missing storage and unavailable input fail without retry", () => {
            var unknown = new ListingPrivateUi(false, true, 123);
            check(unknown.Next(new(false, 65535, 0, false), 1000) == PrivateUiAction.Failed, "unknown accepted");
            check(unknown.Next(new(false, 10000, 0, true), 5000) == PrivateUiAction.Failed, "failed action retried");
            var unavailable = new ListingPrivateUi(true, true, 123);
            check(unavailable.Next(new(true, 456, 456, false), 1000) == PrivateUiAction.Wait, "disabled input used");
            check(unavailable.Next(new(true, 456, 456, false), 5000) == PrivateUiAction.Failed, "disabled input never timed out");
            foreach (var wrongUi in new[] { false, true })
            {
                var ui = new ListingPrivateUi(true, true, 123);
                ui.Next(new(true, 456, 456, true), 1000);
                var state = new PrivateUiState(true, 456, wrongUi ? 789 : 123, true);
                check(ui.Next(state, 1500) == (wrongUi ? PrivateUiAction.Failed : PrivateUiAction.Wait), "wrong mismatch handling");
                check(ui.Next(state, 5000) == PrivateUiAction.Failed, "mismatch retried");
            }
        });
        test("private on, off and password-only edits submit once on all supported layouts", () => {
            foreach (var count in new[] { 4, 8, 24, 48 })
            foreach (var mode in new[] { 0, 1, 2 })
            {
                var enabled = mode != 1; var (h, f) = Start(mode != 0, enabled, count);
                h.PrivateEditComplete = false; f.Update(2500); f.Update(3000);
                check(h.PrivateEditCalls == 1 && h.SubmitCalls == 0, "early/duplicate private operation");
                h.PrivateEditComplete = true; f.Update(3500); f.Update(4000);
                var submitted = h.SubmittedTarget!;
                check(h.SubmitCalls == 1 && ListingPrivate.Read(submitted.ExtraCriteria) == enabled &&
                    submitted.Criteria == h.Target.Criteria && submitted.Description == h.Target.Description &&
                    ListingJobEdit.SameSlots(h.Target.Slots, submitted.Slots) && h.SubmittedDescriptionArgument == null, "unrelated condition changed");
                f.Update(5000); h.Target = submitted with { RemainingSeconds = 3598 }; h.FinishQuery(); f.Update(5500);
                check(f.Stage == RefreshStage.Completed && h.SubmitCalls == 1 && f.Diagnostic.Contains("privateResultContentsAgree=True"), "private readback failed");
                check(f.Diagnostic.Contains("privatePasswordServerCheck=unavailable"), "server PIN verification fabricated");
            }
        });
        test("stale or unsupported private drafts never open the editor", () => {
            var original = Target(); var slots = original.Slots.Slots.ToArray(); slots[1] = slots[1] with { MemberContentId = 99 };
            foreach (var current in new[] { original with { ListingId = 124 }, original with { Description = "다름" },
                original with { ExtraCriteria = "700/1/False/1" }, original with { Slots = original.Slots with { Slots = slots } },
                original with { JobEditingAllowed = false }, original with { ExtraCriteria = "" } })
            {
                var h = new FakeHost { Target = current }; var f = new ListingRefresh(h);
                f.Request(1000, privateEdit: new(original, true, "0123")); f.Update(1000); h.FinishQuery(); f.Update(1500);
                check(f.Stage == RefreshStage.Stopped && h.EditCalls + h.PrivateEditCalls + h.SubmitCalls == 0, "stale draft edited");
            }
        });
        test("private edit excludes every other edit type", () => {
            for (var mode = 0; mode < 5; mode++)
            {
                var h = new FakeHost { Target = Target() }; var f = new ListingRefresh(h); var t = h.Target;
                f.Request(1000, privateEdit: new(t, true, "0123"), uniqueJobsEdit: mode == 0 ? new(t, false) : null,
                    beginnerEdit: mode == 1 ? new(t, false) : null, objectiveEdit: mode == 2 ? new(t, 3) : null,
                    descriptionEdit: mode == 3 ? new(t, "새 소개말") : null, edit: mode == 4 ? new(t, 2, 2, t.Slots.JobRules) : null);
                f.Update(1000);
                check(f.Stage == RefreshStage.Stopped && h.QueryCalls + h.EditCalls + h.SubmitCalls == 0, "combined edit dispatched");
            }
        });
        test("private failure, closure, timeout, cancellation or character change never submits", () => {
            for (var mode = 0; mode < 7; mode++)
            {
                var (h, f) = Start();
                if (mode == 0) h.PrivateEditFailed = true;
                if (mode == 1) h.PrivateEditComplete = false;
                if (mode == 2) f.Cancel();
                if (mode == 3) h.EditorClosedRevision++;
                if (mode == 4) h.SelfId = 99;
                if (mode == 5) h.BlockReason = "이동 중";
                if (mode == 6) h.EditorVisible = false;
                f.Update(2500); f.Update(11000); f.Update(12000);
                check(!f.Busy && h.PrivateEditCalls == 1 && h.SubmitCalls == 0, "failed edit submitted/retried");
            }
        });
        test("private dispatch failure or exception is never retried", () => {
            foreach (var throws in new[] { false, true })
            {
                var h = new FakeHost { Target = Target(), PrivateEditAllowed = false, ThrowOnPrivateEdit = throws };
                var f = new ListingRefresh(h); f.Request(1000, privateEdit: new(h.Target, true, "0123"));
                f.Update(1000); h.FinishQuery(); f.Update(1500);
                try { f.Update(2000); } catch (InvalidOperationException ex) { f.Fail(ex.Message); }
                f.Update(3000); f.Update(15000);
                check(!f.Busy && h.PrivateEditCalls == 1 && h.SubmitCalls == 0, "failed dispatch repeated");
            }
        });
        test("wrong private flag, other conditions or slot changes cannot pass verification", () => {
            foreach (var afterSubmit in new[] { false, true })
            for (var mode = 0; mode < 4; mode++)
            {
                var (h, f) = Start(count: 48); var correct = h.PrivateExpected!;
                var slots = correct.Slots.Slots.ToArray(); slots[47] = slots[47] with { SlotFlags = 0x100 };
                var wrong = mode switch {
                    0 => h.Target,
                    1 => correct with { ExtraCriteria = "700/7/True/6" }, // unique jobs bit lost
                    2 => correct with { Description = "바뀜" },
                    _ => correct with { Slots = correct.Slots with { Slots = slots } }
                };
                if (!afterSubmit)
                {
                    h.PrivateExpected = wrong; f.Update(2500);
                    check(f.Stage == RefreshStage.Stopped && h.SubmitCalls == 0, "wrong draft submitted");
                }
                else
                {
                    f.Update(2500); f.Update(4000); h.Target = wrong with { RemainingSeconds = 3598 }; h.FinishQuery(); f.Update(4500);
                    check(f.Stage == RefreshStage.Unconfirmed && h.SubmitCalls == 1 && f.Diagnostic.Contains("privateResultContentsAgree=False"), "wrong readback accepted");
                }
            }
        });
        test("private confirmation keeps timer evidence separate and refuses create mode", () => {
            var (h, f) = Start(seconds: 3600);
            f.Update(2500); f.Update(4000); h.Target = h.SubmittedTarget!; h.FinishQuery(); f.Update(4500);
            check(f.Stage == RefreshStage.Unconfirmed && f.Status.Contains("비공개 설정이 반영된 것을 확인"), "timer uncertainty hides private result");
            h = new FakeHost { Target = Target(), EditorActionMode = RecruitmentEditorMode.Create }; f = new ListingRefresh(h);
            f.Request(1000, privateEdit: new(h.Target, true, "0123")); f.Update(1000); h.FinishQuery(); f.Update(1500); f.Update(2000); f.Update(9000);
            check(!f.Busy && h.PrivateEditCalls + h.SubmitCalls == 0, "create screen edited");
        });
    }
}
