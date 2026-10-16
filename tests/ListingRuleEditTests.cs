using System.Runtime.InteropServices;
using PartyViewKR;

static class ListingRuleEditTests
{
    internal static void Run(Action<string, Action> test, Action<bool, string> check)
    {
        RefreshTarget Target(int flags = 0, int loot = 0, int count = 8)
        {
            var slots = Enumerable.Repeat(0xFFFFFFFFFFFFFFFEul, count).ToArray();
            var members = new ulong[count]; members[0] = 10;
            var jobs = new byte[count]; jobs[0] = 25;
            var groups = count > 8 ? count / 8 : 1;
            return new(10, 123, 321, "기존 소개말", $"4/321/4/1/4/{flags}/{loot}/1/" + Convert.ToHexString(MemoryMarshal.AsBytes(slots.AsSpan())), 1800, count) {
                Slots = ListingSlots.Read(true, count, 1, groups, count > 8, jobs, members, slots, id => id.ToString()),
                JobEditingAllowed = true, ExtraCriteria = $"555/35/{count > 8}/{groups}"
            };
        }
        (FakeHost h, ListingRefresh f) Start(ListingRuleKind kind, int from = 0, int to = 1, int count = 8, int seconds = 1800)
        {
            var target = kind == ListingRuleKind.Loot ? Target(5, from, count) : Target(from == 1 ? 128 | ListingRules.Mask(kind) : 128, 2, count);
            var h = new FakeHost { Target = target with { RemainingSeconds = seconds } }; var f = new ListingRefresh(h);
            f.Request(1000, ruleEdit: new(h.Target, kind, to));
            f.Update(1000); h.FinishQuery(); f.Update(1500); f.Update(2000);
            check(f.Stage == RefreshStage.EditingRule && h.RuleEditCalls == 1 && h.SubmitCalls == 0, "wrong rule route");
            return (h, f);
        }
        LootUiState Loot(int value, bool open = false) => new(value, value, (byte)value, open, false, open, 3);

        test("rule toggles change only their own bit across all flag combinations", () => {
            foreach (var kind in Enum.GetValues<ListingRuleKind>().Where(k => k != ListingRuleKind.Loot))
            for (var flags = 0; flags <= 255; flags++)
            {
                var t = Target(flags, 2); var before = (flags & ListingRules.Mask(kind)) != 0 ? 1 : 0;
                var draft = new ListingRuleDraft(t, kind);
                check(draft.SelectedValue == before && !draft.Request.Valid, "draft ignored registered flags");
                draft.SelectedValue = 1 - before; var edit = draft.Request;
                check(edit.Valid && edit.MatchesOriginal(t), "valid rule edit rejected");
                var expected = edit.Expected(t); var fields = expected.Criteria.Split('/');
                check(byte.Parse(fields[5]) == (flags ^ ListingRules.Mask(kind)), "unrelated or unknown bit changed");
                check(fields.Where((_, i) => i != 5).SequenceEqual(t.Criteria.Split('/').Where((_, i) => i != 5)) &&
                    expected.ExtraCriteria == t.ExtraCriteria && expected.Description == t.Description &&
                    ListingJobEdit.SameSlots(t.Slots, expected.Slots), "other conditions changed");
            }
        });
        test("loot choices use exact manual indices and preserve every other condition", () => {
            check(ListingRules.LootLabels.SequenceEqual(new[] { "일반", "선입찰 금지", "파티장 분배" }), "wrong loot labels");
            for (var from = 0; from < 3; from++)
            for (var to = 0; to < 3; to++)
            {
                var t = Target(133, from); var edit = new ListingRuleEdit(t, ListingRuleKind.Loot, to);
                check(edit.Valid == (from != to), "unchanged loot accepted");
                if (!edit.Valid) continue;
                var after = edit.Expected(t);
                check(ListingRules.Read(after.Criteria, ListingRuleKind.Loot) == to &&
                    after.Criteria.Split('/').Where((_, i) => i != 6).SequenceEqual(t.Criteria.Split('/').Where((_, i) => i != 6)) &&
                    after.ExtraCriteria == t.ExtraCriteria && after.Description == t.Description && ListingJobEdit.SameSlots(t.Slots, after.Slots), "loot altered other conditions");
            }
        });
        test("invalid unknown unsupported and unchanged rules dispatch nothing", () => {
            foreach (var kind in Enum.GetValues<ListingRuleKind>().Append((ListingRuleKind)99))
            foreach (var value in new[] { -1, 0, 1, 2, 3, 255 })
            {
                var t = Target(); var edit = new ListingRuleEdit(t, kind, value);
                if (ListingRules.ValidValue(kind, value) && value != 0) continue;
                var h = new FakeHost { Target = t }; var f = new ListingRefresh(h);
                f.Request(1000, ruleEdit: edit); f.Update(1000);
                check(f.Stage == RefreshStage.Stopped && h.QueryCalls + h.RuleEditCalls + h.SubmitCalls == 0, "invalid rule reached game");
            }
            foreach (var kind in Enum.GetValues<ListingRuleKind>())
            foreach (var target in new[] { Target() with { Criteria = "" }, Target() with { JobEditingAllowed = false },
                Target(kind == ListingRuleKind.Loot ? 0 : 256, kind == ListingRuleKind.Loot ? 3 : 0) })
                check(!new ListingRuleEdit(target, kind, 1).Valid, "unknown detail guessed");
        });
        test("rule edits cannot combine with any other pending edit", () => {
            foreach (var kind in Enum.GetValues<ListingRuleKind>())
            for (var other = 0; other < 8; other++)
            {
                var h = new FakeHost { Target = Target() }; var t = h.Target; var f = new ListingRefresh(h);
                f.Request(1000, ruleEdit: new(t, kind, 1), edit: other == 0 ? new(t, 2, 2, t.Slots.JobRules) : null,
                    descriptionEdit: other == 1 ? new(t, "새 소개말") : null, objectiveEdit: other == 2 ? new(t, 3) : null,
                    beginnerEdit: other == 3 ? new(t, false) : null, uniqueJobsEdit: other == 4 ? new(t, false) : null,
                    privateEdit: other == 5 ? new(t, false, "") : null, completionEdit: other == 6 ? new(t, true, 0) : null,
                    itemLevelEdit: other == 7 ? new(t, false, "") : null);
                f.Update(1000);
                check(!f.Busy && h.QueryCalls + h.RuleEditCalls + h.SubmitCalls == 0, "combined edit acted");
            }
        });
        test("all rule edits preserve 4 8 24 and 48 slot listings and verify the fresh result", () => {
            foreach (var kind in Enum.GetValues<ListingRuleKind>())
            foreach (var count in new[] { 4, 8, 24, 48 })
            for (var from = 0; from < (kind == ListingRuleKind.Loot ? 3 : 2); from++)
            for (var to = 0; to < (kind == ListingRuleKind.Loot ? 3 : 2); to++)
            {
                if (from == to) continue;
                var (h, f) = Start(kind, from, to, count);
                h.RuleEditComplete = false; f.Update(2500); f.Update(3000);
                check(h.RuleEditCalls == 1 && h.SubmitCalls == 0, "early or repeated rule action");
                h.RuleEditComplete = true; f.Update(3500); f.Update(4000);
                check(h.SubmitCalls == 1 && h.SubmittedDescriptionArgument == null &&
                    ListingRules.Read(h.SubmittedTarget!.Criteria, kind) == to &&
                    new ListingRuleEdit(h.Target, kind, to).MatchesResult(h.Target, h.SubmittedTarget), "wrong submitted rule");
                f.Update(5000); h.Target = h.SubmittedTarget! with { RemainingSeconds = 3598 }; h.FinishQuery(); f.Update(5500);
                check(f.Stage == RefreshStage.Completed && h.SubmitCalls == 1 && f.Diagnostic.Contains("ruleResultContentsAgree=True"), "fresh result not verified");
            }
        });
        test("stale rule drafts reject changed listings members and unrelated rules before opening", () => {
            var original = Target(); var members = original.Slots.Slots.ToArray(); members[1] = members[1] with { MemberContentId = 99 };
            foreach (var kind in Enum.GetValues<ListingRuleKind>())
            foreach (var current in new[] { Target(4), Target(0, 2), original with { ListingId = 124 }, original with { Description = "변경" },
                original with { ExtraCriteria = "700/35/False/1" }, original with { Slots = original.Slots with { Slots = members } }, original with { JobEditingAllowed = false } })
            {
                var h = new FakeHost { Target = current }; var f = new ListingRefresh(h);
                f.Request(1000, ruleEdit: new(original, kind, 1)); f.Update(1000); h.FinishQuery(); f.Update(1500);
                check(f.Stage == RefreshStage.Stopped && h.EditCalls + h.RuleEditCalls + h.SubmitCalls == 0, "stale rule draft acted");
            }
        });
        test("rule failures timeout cancellation and window changes never submit or retry", () => {
            foreach (var kind in Enum.GetValues<ListingRuleKind>())
            for (var mode = 0; mode < 7; mode++)
            {
                var (h, f) = Start(kind);
                if (mode == 0) h.RuleEditFailed = true;
                if (mode == 1) h.RuleEditComplete = false;
                if (mode == 2) f.Cancel();
                if (mode == 3) h.EditorClosedRevision++;
                if (mode == 4) h.SelfId = 99;
                if (mode == 5) h.BlockReason = "이동 중";
                if (mode == 6) h.EditorVisible = false;
                f.Update(2500); f.Update(11000); f.Update(12000);
                check(!f.Busy && h.RuleEditCalls == 1 && h.SubmitCalls == 0, "failed edit retried or submitted");
            }
            foreach (var throws in new[] { false, true })
            {
                var h = new FakeHost { Target = Target(), RuleEditAllowed = false, ThrowOnRuleEdit = throws }; var f = new ListingRefresh(h);
                f.Request(1000, ruleEdit: new(h.Target, ListingRuleKind.Unrestricted, 1)); f.Update(1000); h.FinishQuery(); f.Update(1500);
                try { f.Update(2000); } catch (InvalidOperationException ex) { f.Fail(ex.Message); }
                f.Update(3000); f.Update(15000);
                check(!f.Busy && h.RuleEditCalls == 1 && h.SubmitCalls == 0, "failed dispatch repeated");
            }
        });
        test("wrong rule or side effects fail both draft and server readback verification", () => {
            foreach (var kind in Enum.GetValues<ListingRuleKind>())
            foreach (var afterSubmit in new[] { false, true })
            for (var mode = 0; mode < 7; mode++)
            {
                var (h, f) = Start(kind, count: 48); var correct = h.RuleExpected!;
                var slots = correct.Slots.Slots.ToArray(); slots[47] = slots[47] with { SlotFlags = 0x100 };
                var otherRule = kind == ListingRuleKind.Unrestricted ? ListingRuleKind.MinimumItemLevel : ListingRuleKind.Unrestricted;
                var fields = correct.Criteria.Split('/'); fields[5] = (byte.Parse(fields[5]) ^ 128).ToString();
                var wrong = mode switch {
                    0 => h.Target,
                    1 => correct with { Criteria = ListingRules.WithValue(correct.Criteria, otherRule, 1 - ListingRules.Read(correct.Criteria, otherRule)!.Value) },
                    2 => correct with { ExtraCriteria = "555/3/True/6" }, 3 => correct with { Description = "변경" },
                    4 => correct with { Slots = correct.Slots with { Slots = slots } },
                    5 => correct with { Criteria = string.Join('/', fields) },
                    _ => correct with { Criteria = correct.Criteria.Replace("4/321/4/1/4/", "4/321/2/1/4/") }
                };
                if (!afterSubmit)
                {
                    h.RuleExpected = wrong; f.Update(2500);
                    check(f.Stage == RefreshStage.Stopped && h.SubmitCalls == 0, "unexpected side effect submitted");
                }
                else
                {
                    f.Update(2500); f.Update(4000); h.Target = wrong with { RemainingSeconds = 3598 }; h.FinishQuery(); f.Update(4500);
                    check(f.Stage == RefreshStage.Unconfirmed && h.SubmitCalls == 1 && f.Diagnostic.Contains("ruleResultContentsAgree=False"), "wrong server readback accepted");
                }
            }
        });
        test("rule confirmation is separate from the timer and create mode never edits", () => {
            foreach (var kind in Enum.GetValues<ListingRuleKind>())
            {
                var (h, f) = Start(kind, seconds: 3600);
                f.Update(2500); f.Update(4000); h.Target = h.SubmittedTarget!; h.FinishQuery(); f.Update(4500);
                check(f.Stage == RefreshStage.Unconfirmed && f.Status.Contains("설정이 반영된 것을 확인"), "timer uncertainty hid rule result");
                h = new FakeHost { Target = Target(), EditorActionMode = RecruitmentEditorMode.Create }; f = new(h);
                f.Request(1000, ruleEdit: new(h.Target, kind, 1)); f.Update(1000); h.FinishQuery(); f.Update(1500); f.Update(2000); f.Update(9000);
                check(!f.Busy && h.RuleEditCalls + h.SubmitCalls == 0, "create screen edited");
            }
        });
        test("loot native sequence opens clicks and selects every different pair exactly once", () => {
            for (var from = 0; from < 3; from++)
            for (var to = 0; to < 3; to++)
            {
                if (from == to) continue;
                var ui = new ListingLootUi(from, to);
                check(ui.Next(Loot(from), 1000) == LootUiAction.Open, "did not open");
                check(ui.Next(Loot(from) with { Checked = true, Pending = true }, 1100) == LootUiAction.Wait, "clicked pending dropdown");
                check(ui.Next(Loot(from, true), 1500) == LootUiAction.ClickItem, "did not click item");
                check(ui.Next(Loot(from), 1500) == LootUiAction.SelectItem, "native highlight not selected");
                check(ui.Next(Loot(to), 1501) == LootUiAction.Wait, "selection not settled");
                check(ui.Next(Loot(to), 2000) == LootUiAction.Complete && ui.Next(Loot(to), 2500) == LootUiAction.Complete, "loot selection repeated or unconfirmed");
                check(ui.Next(Loot(from), 3000) == LootUiAction.Failed, "selection reversal ignored");
            }
        });
        test("loot unknown initial state and changed selection fail without guessing or retry", () => {
            foreach (var state in new[] { Loot(1), Loot(0, true), Loot(0) with { Pending = true }, Loot(0) with { Checked = true },
                Loot(0) with { Count = 4 }, Loot(0) with { Stored = 3 }, Loot(0) with { Index = -1 }, Loot(0) with { ListIndex = 1 } })
            {
                var ui = new ListingLootUi(0, 2);
                check(ui.Next(state, 1000) == LootUiAction.Failed && ui.Next(Loot(0), 2000) == LootUiAction.Failed, "bad state acted or retried");
            }
            var changed = new ListingLootUi(0, 2); changed.Next(Loot(0), 1000);
            check(changed.Next(Loot(1, true), 1500) == LootUiAction.Failed, "unexpected selection overwritten");
            var stillOpen = new ListingLootUi(0, 2); stillOpen.Next(Loot(0), 1000); stillOpen.Next(Loot(0, true), 1500);
            check(stillOpen.Next(Loot(0, true), 1500) == LootUiAction.Failed, "still-open item selected");
        });
        test("loot timeouts and UI-only changes never confirm or repeat native actions", () => {
            var ui = new ListingLootUi(0, 2); ui.Next(Loot(0), 1000);
            check(ui.Next(Loot(0), 1500) == LootUiAction.Wait && ui.Next(Loot(0), 4500) == LootUiAction.Failed, "opening never timed out");
            ui = new(0, 2); ui.Next(Loot(0), 1000); ui.Next(Loot(0, true), 1500); ui.Next(Loot(0), 1500);
            var uiOnly = Loot(2) with { Stored = 0 };
            check(ui.Next(uiOnly, 2000) == LootUiAction.Wait && ui.Next(uiOnly, 5000) == LootUiAction.Failed &&
                ui.Next(Loot(2), 6000) == LootUiAction.Failed, "UI-only selection confirmed or retried");
        });
        test("duty checkbox edits wait for stored flags and do not resend a click", () => {
            foreach (var kind in Enum.GetValues<ListingRuleKind>().Where(k => k != ListingRuleKind.Loot))
            foreach (var original in new[] { false, true })
            {
                var ui = new ListingCheckboxUi(original, !original, ListingRules.Id(kind));
                var before = new CheckboxUiState(original, original ? (byte)1 : (byte)0);
                check(ui.Next(before, 1000) == CheckboxUiAction.Click, "no toggle");
                var pending = before with { Checked = !original };
                check(ui.Next(pending, 1500) == CheckboxUiAction.Wait, "UI-only flag accepted");
                var after = pending with { Stored = original ? (byte)0 : (byte)1 };
                check(ui.Next(after, 2000) == CheckboxUiAction.Complete && ui.Next(after, 2500) == CheckboxUiAction.Complete, "flag unconfirmed or toggled again");
                check(ui.Next(before, 3000) == CheckboxUiAction.Failed, "flag reversal ignored");
            }
        });
    }
}
