using System.Runtime.InteropServices;
using PartyViewKR;

static class ListingItemLevelEditTests
{
    internal static void Run(Action<string, Action> test, Action<bool, string> check)
    {
        RefreshTarget Target(int level = 700, int count = 8)
        {
            var flags = Enumerable.Repeat(0xFFFFFFFFFFFFFFFEul, count).ToArray();
            var members = new ulong[count]; members[0] = 10;
            var jobs = new byte[count]; jobs[0] = 25;
            var groups = count > 8 ? count / 8 : 1;
            return new(10, 123, 321, "기존 소개말", "4/321/4/1/4/2/1/1/" + Convert.ToHexString(MemoryMarshal.AsBytes(flags.AsSpan())), 1800, count) {
                Slots = ListingSlots.Read(true, count, 1, groups, count > 8, jobs, members, flags, id => id.ToString()),
                JobEditingAllowed = true, ExtraCriteria = $"{level}/{(count > 8 ? 39 : 35)}/{count > 8}/{groups}"
            };
        }
        ItemLevelUiState State(bool enabled, int stored = 1, int? numeric = null, bool? ready = null) =>
            new(enabled, enabled ? (byte)1 : (byte)0, (ushort)stored, numeric ?? stored, ready ?? enabled, 1, 999);
        (FakeHost h, ListingRefresh f) Start(int original = 700, int requested = 555, int count = 8, int seconds = 1800)
        {
            var h = new FakeHost { Target = Target(original, count) with { RemainingSeconds = seconds } };
            var f = new ListingRefresh(h); f.Request(1000, itemLevelEdit: new(h.Target, requested != 0, requested == 0 ? "" : requested.ToString()));
            f.Update(1000); h.FinishQuery(); f.Update(1500); f.Update(2000);
            check(f.Stage == RefreshStage.EditingItemLevel && h.ItemLevelEditCalls == 1 && h.SubmitCalls == 0, "wrong item level route");
            return (h, f);
        }
        test("item level accepts exactly the 1 to 999 range without clamping invalid input", () => {
            for (var level = 1; level <= 999; level++)
            {
                check(ListingItemLevels.TryLevel(level.ToString(), out var parsed) && parsed == level, "valid level rejected");
                var edit = new ListingItemLevelEdit(Target(0), true, level.ToString());
                check(edit.Valid && edit.Value == level, "valid requirement changed");
            }
            foreach (var text in new[] { "", "0", "000", "1000", "9999", "-1", "+1", "1.0", " 1", "1 ", "5\n5", "５５５", "٥٥٥", "1e2", "0001" })
            {
                check(!ListingItemLevels.TryLevel(text, out _), "invalid input parsed");
                var h = new FakeHost { Target = Target() }; var f = new ListingRefresh(h);
                f.Request(1000, itemLevelEdit: new(h.Target, true, text)); f.Update(1000);
                check(f.Stage == RefreshStage.Stopped && h.QueryCalls + h.EditCalls + h.SubmitCalls == 0, "invalid input reached game");
            }
            check(ListingItemLevels.TryLevel("001", out var one) && one == 1, "numeric leading zeroes rejected");
        });
        test("item level draft uses the listing requirement and preserves other conditions", () => {
            foreach (var original in new[] { 0, 1, 555, 999 })
            {
                var t = Target(original); var d = new ListingItemLevelDraft(t);
                check(d.Enabled == (original != 0) && d.LevelText == Math.Max(1, original).ToString() && !d.Request.Valid, "wrong initial draft");
                d.Enabled = original == 0; d.LevelText = "555"; var edit = d.Request;
                check(edit.Valid && edit.Value == (original == 0 ? 555 : 0), "wrong toggle result");
                var after = edit.Expected(t);
                check(after.Criteria == t.Criteria && after.Description == t.Description && ListingJobEdit.SameSlots(t.Slots, after.Slots), "unrelated content changed");
                check(after.ExtraCriteria.Split('/').Skip(1).SequenceEqual(t.ExtraCriteria.Split('/').Skip(1)), "join flags or group shape changed");
            }
            var off = new ListingItemLevelDraft(Target()) { Enabled = false, LevelText = "invalid" };
            check(off.Request.Valid && off.Request.Value == 0, "turning off requires numeric input");
            foreach (var text in new[] { "", "1/2/False", "1000/2/False/1", "-1/2/False/1", "x/2/False/1" })
                check(ListingItemLevels.Read(text) == null, "unknown detail guessed");
            check(ListingItemLevels.Label(0) == "설정 안 함" && ListingItemLevels.Label(555) == "555 이상", "wrong display");
        });
        test("enabling item level waits for checkbox storage then enters the requested number once", () => {
            var ui = new ListingItemLevelUi(0, 555);
            check(ui.Next(State(false), 1000) == ItemLevelUiAction.Toggle, "missing enable");
            check(ui.Next(State(false) with { Checked = true }, 1100) == ItemLevelUiAction.Wait, "input before enable stored");
            check(ui.Next(State(true), 1500) == ItemLevelUiAction.Input, "missing numeric input");
            check(ui.Next(State(true, 1, 555), 2000) == ItemLevelUiAction.Wait, "UI-only number confirmed");
            check(ui.Next(State(true, 555), 2500) == ItemLevelUiAction.Complete, "matching number unconfirmed");
            check(ui.Next(State(true, 555), 3000) == ItemLevelUiAction.Complete, "numeric mutation repeated");
        });
        test("disabling item level preserves a cached number and requires the stored enabled flag", () => {
            foreach (var cached in new[] { 0, 1, 555, 999 })
            {
                var ui = new ListingItemLevelUi(555, 0);
                check(ui.Next(State(true, 555), 1000) == ItemLevelUiAction.Toggle, "missing disable");
                check(ui.Next(State(true, 555) with { Checked = false }, 1500) == ItemLevelUiAction.Wait, "unchecked UI alone confirmed");
                check(ui.Next(State(false, cached, 555), 2000) == ItemLevelUiAction.Complete, "cached number confused with enabled restriction");
                check(ui.Next(State(false, cached, 555), 2500) == ItemLevelUiAction.Complete, "disable sent numeric update");
            }
        });
        test("enabling an already matching cached level does not send redundant numeric input", () => {
            foreach (var level in new[] { 1, 555, 999 })
            {
                var ui = new ListingItemLevelUi(0, level);
                check(ui.Next(State(false, level), 1000) == ItemLevelUiAction.Toggle, "missing enable");
                check(ui.Next(State(true, level), 1001) == ItemLevelUiAction.Wait, "no settle interval");
                check(ui.Next(State(true, level), 1500) == ItemLevelUiAction.Complete, "matching restored number rewritten");
            }
        });
        test("number-only item level edits keep the checkbox on and cover both boundaries", () => {
            foreach (var original in new[] { 1, 555, 999 })
            foreach (var requested in new[] { 1, 555, 999 }.Where(v => v != original))
            {
                var ui = new ListingItemLevelUi(original, requested);
                check(ui.Next(State(true, original), 1000) == ItemLevelUiAction.Input, "number-only edit toggled checkbox");
                check(ui.Next(State(true, requested), 1001) == ItemLevelUiAction.Wait, "number did not settle");
                check(ui.Next(State(true, requested), 1500) == ItemLevelUiAction.Complete, "number not verified");
                check(ui.Next(State(true, original), 2000) == ItemLevelUiAction.Failed, "later reversal ignored");
            }
        });
        test("unknown item level state, stale controls and game range restrictions dispatch nothing", () => {
            foreach (var state in new[] { State(true, 555), State(true, 700, 555), State(false, 700),
                State(true, 700) with { EnabledRaw = 2 }, State(true, 1000), State(true, 700, ready:false),
                State(true, 700) with { Minimum = 600 }, State(true, 700) with { Maximum = 500 } })
            {
                var ui = new ListingItemLevelUi(700, 555);
                check(ui.Next(state, 1000) == ItemLevelUiAction.Failed, "bad initial/range state mutated");
                check(ui.Next(State(true, 700), 2000) == ItemLevelUiAction.Failed, "failure retried");
            }
            foreach (var pair in new[] { (-1,555), (1000,555), (700,1000), (700,-1), (700,700), (0,0) })
                check(new ListingItemLevelUi(pair.Item1,pair.Item2).Next(State(true,700),1000) == ItemLevelUiAction.Failed, "bad request mutated");
        });
        test("missing input or stored changes time out without retry and UI mismatches fail", () => {
            var ui = new ListingItemLevelUi(0, 555); ui.Next(State(false), 1000);
            var uncheckedStorage = State(false) with { Checked = true };
            check(ui.Next(uncheckedStorage, 1500) == ItemLevelUiAction.Wait && ui.Next(uncheckedStorage, 4500) == ItemLevelUiAction.Failed, "checkbox storage timeout missing");
            ui = new(0, 555); ui.Next(State(false), 1000);
            check(ui.Next(State(true, ready:false), 1500) == ItemLevelUiAction.Wait && ui.Next(State(true, ready:false), 5000) == ItemLevelUiAction.Failed, "disabled input never timed out");
            ui = new(700, 555); ui.Next(State(true, 700), 1000);
            check(ui.Next(State(true, 700, 555), 1500) == ItemLevelUiAction.Wait && ui.Next(State(true, 700, 555), 4500) == ItemLevelUiAction.Failed, "UI-only value never timed out");
            foreach (var state in new[] { State(true, 555, 700), State(true, 777, 555), State(false, 555), State(true, 555, ready:false) })
            {
                ui = new(700,555); ui.Next(State(true,700),1000);
                check(ui.Next(state,1500) == ItemLevelUiAction.Failed, "unexpected value accepted");
            }
        });
        test("item level on off and number-only edits preserve 4 8 24 and 48 slot listings", () => {
            foreach (var count in new[] { 4, 8, 24, 48 })
            foreach (var requested in new[] { 1, 555, 999 })
            foreach (var mode in new[] { 0, 1, 2 })
            {
                var original = mode == 0 ? 0 : 700; var value = mode == 1 ? 0 : requested;
                var (h, f) = Start(original, value, count);
                h.ItemLevelEditComplete = false; f.Update(2500); f.Update(3000);
                check(h.ItemLevelEditCalls == 1 && h.SubmitCalls == 0, "early or duplicate edit");
                h.ItemLevelEditComplete = true; f.Update(3500); f.Update(4000);
                var submitted = h.SubmittedTarget!;
                check(h.SubmitCalls == 1 && ListingItemLevels.Read(submitted.ExtraCriteria) == value &&
                    submitted.Criteria == h.Target.Criteria && submitted.Description == h.Target.Description &&
                    submitted.ExtraCriteria.Split('/').Skip(1).SequenceEqual(h.Target.ExtraCriteria.Split('/').Skip(1)) &&
                    ListingJobEdit.SameSlots(submitted.Slots, h.Target.Slots) && h.SubmittedDescriptionArgument == null, "other setting changed");
                f.Update(5000); h.Target = submitted with { RemainingSeconds = 3598 }; h.FinishQuery(); f.Update(5500);
                check(f.Stage == RefreshStage.Completed && h.SubmitCalls == 1 && f.Diagnostic.Contains("itemLevelResultContentsAgree=True"), "fresh level not confirmed");
            }
        });
        test("unchanged unknown or unsupported item level edits do not query", () => {
            foreach (var (target, requested) in new[] { (Target(), "700"), (Target() with { JobEditingAllowed = false }, "555"),
                (Target(1000), "555"), (Target() with { ExtraCriteria = "" }, "555") })
            {
                var h = new FakeHost { Target = target }; var f = new ListingRefresh(h);
                f.Request(1000, itemLevelEdit: new(target, true, requested)); f.Update(1000);
                check(f.Stage == RefreshStage.Stopped && h.QueryCalls + h.EditCalls + h.SubmitCalls == 0, "no-op or unsupported edit acted");
            }
            var off = new FakeHost { Target = Target(0) }; var refresh = new ListingRefresh(off);
            refresh.Request(1000,itemLevelEdit:new(off.Target,false,"")); refresh.Update(1000);
            check(refresh.Stage == RefreshStage.Stopped && off.QueryCalls == 0, "disabled no-op queried");
        });
        test("item level cannot be combined with any other edit", () => {
            for (var mode = 0; mode < 7; mode++)
            {
                var h = new FakeHost { Target = Target() }; var f = new ListingRefresh(h); var t = h.Target;
                f.Request(1000,itemLevelEdit:new(t,true,"555"),completionEdit:mode==0?new(t,true,0):null,
                    privateEdit:mode==1?new(t,false,""):null,uniqueJobsEdit:mode==2?new(t,false):null,
                    beginnerEdit:mode==3?new(t,false):null,objectiveEdit:mode==4?new(t,3):null,
                    descriptionEdit:mode==5?new(t,"새 소개말"):null,edit:mode==6?new(t,2,2,t.Slots.JobRules):null);
                f.Update(1000);
                check(f.Stage == RefreshStage.Stopped && h.QueryCalls + h.EditCalls + h.SubmitCalls == 0, "combined edit acted");
            }
        });
        test("stale item level drafts detect changed requirements participants and other conditions", () => {
            var original = Target(); var slots = original.Slots.Slots.ToArray(); slots[1] = slots[1] with { MemberContentId = 99 };
            foreach (var current in new[] { original with { ListingId=124 }, original with { Description="변경" },
                original with { ExtraCriteria="555/35/False/1" }, original with { ExtraCriteria="700/33/False/1" },
                original with { Slots=original.Slots with { Slots=slots } }, original with { JobEditingAllowed=false } })
            {
                var h=new FakeHost { Target=current }; var f=new ListingRefresh(h);
                f.Request(1000,itemLevelEdit:new(original,true,"555")); f.Update(1000); h.FinishQuery(); f.Update(1500);
                check(f.Stage==RefreshStage.Stopped && h.EditCalls+h.ItemLevelEditCalls+h.SubmitCalls==0,"stale draft opened editor");
            }
        });
        test("item level failures timeouts cancellation and editor or character changes never submit or retry", () => {
            for (var mode=0; mode<7; mode++)
            {
                var (h,f)=Start();
                if(mode==0)h.ItemLevelEditFailed=true;
                if(mode==1)h.ItemLevelEditComplete=false;
                if(mode==2)f.Cancel();
                if(mode==3)h.EditorClosedRevision++;
                if(mode==4)h.SelfId=99;
                if(mode==5)h.BlockReason="이동 중";
                if(mode==6)h.EditorVisible=false;
                f.Update(2500); f.Update(11000); f.Update(12000);
                check(!f.Busy && h.ItemLevelEditCalls==1 && h.SubmitCalls==0,"failed edit submitted or repeated");
            }
            foreach(var throws in new[]{false,true})
            {
                var h=new FakeHost { Target=Target(), ItemLevelEditAllowed=false, ThrowOnItemLevelEdit=throws }; var f=new ListingRefresh(h);
                f.Request(1000,itemLevelEdit:new(h.Target,true,"555")); f.Update(1000); h.FinishQuery(); f.Update(1500);
                try { f.Update(2000); } catch(InvalidOperationException ex) { f.Fail(ex.Message); }
                f.Update(3000); f.Update(15000);
                check(!f.Busy && h.ItemLevelEditCalls==1 && h.SubmitCalls==0,"failed dispatch repeated");
            }
        });
        test("wrong item level and unrelated changes fail both pre-submit and fresh result checks", () => {
            foreach(var afterSubmit in new[]{false,true})
            for(var mode=0;mode<5;mode++)
            {
                var (h,f)=Start(count:48); var correct=h.ItemLevelExpected!;
                var slots=correct.Slots.Slots.ToArray(); slots[47]=slots[47] with { SlotFlags=0x100 };
                var wrong=mode switch {
                    0=>h.Target, 1=>correct with { ExtraCriteria="555/7/True/6" },
                    2=>correct with { Description="변경" }, 3=>correct with { Criteria=correct.Criteria.Replace("/4/1/4/", "/2/1/4/") },
                    _=>correct with { Slots=correct.Slots with { Slots=slots } }
                };
                if(!afterSubmit)
                {
                    h.ItemLevelExpected=wrong; f.Update(2500);
                    check(f.Stage==RefreshStage.Stopped && h.SubmitCalls==0,"wrong draft submitted");
                }
                else
                {
                    f.Update(2500); f.Update(4000); h.Target=wrong with { RemainingSeconds=3598 }; h.FinishQuery(); f.Update(4500);
                    check(f.Stage==RefreshStage.Unconfirmed && h.SubmitCalls==1 && f.Diagnostic.Contains("itemLevelResultContentsAgree=False"),"wrong readback accepted");
                }
            }
        });
        test("item level result confirmation is separate from timer evidence and refuses create mode", () => {
            var (h,f)=Start(seconds:3600);
            f.Update(2500); f.Update(4000); h.Target=h.SubmittedTarget!; h.FinishQuery(); f.Update(4500);
            check(f.Stage==RefreshStage.Unconfirmed && f.Status.Contains("평균 아이템 레벨이 등록된 것을 확인"),"timer uncertainty hid confirmed requirement");
            h=new FakeHost { Target=Target(), EditorActionMode=RecruitmentEditorMode.Create }; f=new ListingRefresh(h);
            f.Request(1000,itemLevelEdit:new(h.Target,true,"555")); f.Update(1000); h.FinishQuery(); f.Update(1500); f.Update(2000); f.Update(9000);
            check(!f.Busy && h.ItemLevelEditCalls+h.SubmitCalls==0,"create screen edited");
        });
    }
}
