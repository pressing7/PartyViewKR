using System.Runtime.InteropServices;
using PartyViewKR;

static class ListingDescriptionEditTests
{
    internal static void Run(Action<string, Action> test, Action<bool, string> check)
    {
        RefreshTarget Target(int count = 8, string text = "기존 소개말")
        {
            var flags = Enumerable.Repeat(0x80000000FFFFFFFEul, count).ToArray();
            var members = new ulong[count]; members[0] = 10;
            var jobs = new byte[count]; jobs[0] = 19;
            return new(10, 123, 321, text, "4/321/0/0/0/0/0/1/" + Convert.ToHexString(MemoryMarshal.AsBytes(flags.AsSpan())), 1800, count) {
                Slots = ListingSlots.Read(true, count, 1, count > 8 ? count / 8 : 1, count > 8, jobs, members, flags, id => id.ToString()),
                JobEditingAllowed = true, ExtraCriteria = "700/0",
            };
        }
        (FakeHost host, ListingRefresh flow, ListingDescriptionEdit edit) Start(string text, int count = 8)
        {
            var h = new FakeHost { Target = Target(count) }; var f = new ListingRefresh(h);
            var edit = new ListingDescriptionEdit(h.Target, text);
            f.Request(1000, descriptionEdit: edit); f.Update(1000); h.FinishQuery(); f.Update(1500); f.Update(2000);
            check(f.Stage == RefreshStage.EditingDescription && h.DescriptionEditCalls == 1 && h.JobEditCalls == 0 && h.SubmitCalls == 0, "description entry");
            return (h, f, edit);
        }
        test("description draft is local and preserves the original until applied", () => {
            var t = Target(); var draft = new ListingDescriptionDraft(t);
            check(draft.Text == t.Description && !draft.Request.Valid, "prefill/unchanged draft");
            draft.Text = "수정 내용";
            check(t.Description == "기존 소개말" && draft.Request.Valid, "local draft mutated original");
            check(draft.Request.Expected(t).Slots == t.Slots && draft.Request.Expected(t).Criteria == t.Criteria, "other conditions changed");
        });
        test("plain text, Korean, empty and line endings validate without splitting UTF-8", () => {
            foreach (var text in new[] { "", " ", "새 소개말", "첫 줄\r\n둘째 줄", new string('a', 192), new string('가', 64), "소개말 없음" })
                check(new ListingDescriptionEdit(Target(), text).Valid, "valid text rejected");
            foreach (var text in new[] { new string('a', 193), new string('가', 65), "중간\0문자", "\ud800", "텍스트\u0002토큰\u0003", "\ufffd" })
                check(!new ListingDescriptionEdit(Target(), text).Valid, "invalid input accepted");
            check(new ListingDescriptionEdit(Target(), "첫 줄\r\n둘째 줄").Value == "첫 줄\n둘째 줄", "line endings not normalized");
            check(new ListingDescriptionEdit(Target(), "소개말 없음").Expected(Target()).Description == "소개말 없음", "literal placeholder removed");
            check(!new ListingDescriptionEdit(Target(text: ""), " ").Valid, "unchanged empty input accepted");
        });
        test("description edit applies once and verifies fresh results for 4/8/24/48 slots", () => {
            foreach (var count in new[] { 4, 8, 24, 48 })
            foreach (var text in new[] { "수정한 소개말", "" })
            {
                var (h, f, edit) = Start(text, count); h.DescriptionEditComplete = false;
                f.Update(2500); f.Update(3000);
                check(h.DescriptionEditCalls == 1 && h.SubmitCalls == 0, "pending input submitted/repeated");
                h.DescriptionEditComplete = true; f.Update(3500); f.Update(4000);
                check(h.SubmitCalls == 1 && h.SubmittedDescription == edit.Value, "wrong/duplicate description submit");
                check(h.SubmittedDescriptionArgument == edit.Value, "final submit omitted the edited comment argument");
                check(h.SubmittedTarget!.Criteria == h.Target.Criteria && ListingJobEdit.SameSlots(h.Target.Slots, h.SubmittedTarget.Slots), "slot/settings changed");
                f.Update(5000); h.Target = h.SubmittedTarget with { RemainingSeconds = 3598 }; h.FinishQuery(); f.Update(5500);
                check(f.Stage == RefreshStage.Completed && h.DescriptionEditCalls == 1 && h.SubmitCalls == 1, "description result unconfirmed/repeated");
            }
        });
        test("adding an introduction to an empty listing uses empty original text", () => {
            var h = new FakeHost { Target = Target(text: "") }; var f = new ListingRefresh(h);
            f.Request(1000, descriptionEdit: new(h.Target, "새 소개말")); f.Update(1000); h.FinishQuery(); f.Update(1500); f.Update(2000); f.Update(2500);
            check(h.SubmitCalls == 1 && h.SubmittedDescription == "새 소개말", "empty original rejected");
            check(h.SubmittedDescriptionArgument == "새 소개말", "final submit omitted added text");
        });
        test("final submission carries changed, empty and Korean text independently of the draft", () => {
            foreach (var text in new[] { "12345", "", "한글 소개말", "첫 줄\n둘째 줄" })
            {
                var (h, f, edit) = Start(text);
                // The editor snapshot alone is not the outgoing payload. In the
                // reported KR behavior, a button-only submit kept the old text.
                f.Update(2500);
                var postedText = h.SubmittedDescriptionArgument ?? h.Target.Description;
                check(h.SubmittedDescriptionArgument != null && postedText == edit.Value, "new comment absent from submit payload");
                check(h.SubmitCalls == 1 && h.Target.Description == "기존 소개말", "duplicate submit or original overwritten");
            }
        });
        test("reported three-character old text after a five-byte edit remains unconfirmed with diagnostic detail", () => {
            var h = new FakeHost { Target = Target(text: "old") with { RemainingSeconds = 3600 } };
            var f = new ListingRefresh(h);
            f.Request(1000, descriptionEdit: new(h.Target, "12345")); f.Update(1000); h.FinishQuery(); f.Update(1500);
            f.Update(2000); f.Update(2500); f.Update(4000); h.FinishQuery(); f.Update(4500); f.Update(20000);
            check(f.Stage == RefreshStage.Unconfirmed && h.SubmitCalls == 1, "old text accepted or resubmitted");
            check(f.Diagnostic.Contains("descriptionResultTextAgrees=False") &&
                f.Diagnostic.Contains("descriptionResultExpectedChars=5; descriptionResultActualChars=3") &&
                f.Diagnostic.Contains("descriptionResultCriteriaAgree=True") && f.Diagnostic.Contains("descriptionResultSlotsAgree=True"), "missing mismatch evidence");
            check(!f.Diagnostic.Contains("12345") && !f.Diagnostic.Contains("old"), "comment content leaked into diagnostics");
        });
        test("verified text at the same displayed timer is reported separately from timer confirmation", () => {
            var (h, f, _) = Start("등록된 새 소개말");
            f.Update(2500); f.Update(4000); h.Target = h.SubmittedTarget!; h.FinishQuery(); f.Update(4500);
            check(f.Stage == RefreshStage.Unconfirmed && f.Status.Contains("수정한 소개말이 등록된 것을 확인"), "verified text hidden by timer ambiguity");
            check(f.Diagnostic.Contains("descriptionResultTextAgrees=True"), "missing registered text confirmation");
        });
        test("failed or thrown description submissions never fall back to a button or retry", () => {
            foreach (var throws in new[] { false, true })
            {
                var (h, f, _) = Start("제출할 소개말"); h.SubmitAllowed = false; h.ThrowOnSubmit = throws;
                try { f.Update(2500); } catch (InvalidOperationException ex) { f.Fail(ex.Message); }
                f.Update(3000); f.Update(20000);
                check(!f.Busy && h.SubmitCalls == 1 && h.SubmittedDescriptionArgument == "제출할 소개말", "failed submission retried or lost text");
            }
        });
        test("changed description or alliance participants block stale drafts before opening", () => {
            var original = Target(48); var changedSlots = original.Slots.Slots.ToArray();
            changedSlots[16] = changedSlots[16] with { JobId = 24, MemberContentId = 99 };
            foreach (var current in new[] { original with { Description = "이미 변경" }, original with { ListingId = 124 },
                original with { Slots = original.Slots with { Slots = changedSlots, SlotsFilled = 2 } }, original with { ExtraCriteria = "changed" } })
            {
                var h = new FakeHost { Target = current }; var f = new ListingRefresh(h);
                f.Request(1000, descriptionEdit: new(original, "편집 내용")); f.Update(1000); h.FinishQuery(); f.Update(1500);
                check(f.Stage == RefreshStage.Stopped && h.EditCalls + h.DescriptionEditCalls + h.SubmitCalls == 0, "stale text overwritten");
            }
        });
        test("description failure, timeout, cancellation or editor closure never submits", () => {
            for (var mode = 0; mode < 4; mode++)
            {
                var (h, f, _) = Start("변경");
                if (mode == 0) h.DescriptionEditFailed = true;
                if (mode == 1) h.DescriptionEditComplete = false;
                if (mode == 2) f.Cancel();
                if (mode == 3) h.EditorClosedRevision++;
                f.Update(2500); f.Update(11000); f.Update(12000);
                check(!f.Busy && h.SubmitCalls == 0 && h.DescriptionEditCalls == 1, "failed text operation continued");
            }
        });
        test("a failed or thrown introduction input is never retried", () => {
            foreach (var throws in new[] { false, true })
            {
                var h = new FakeHost { Target = Target(), DescriptionEditAllowed = false, ThrowOnDescriptionEdit = throws }; var f = new ListingRefresh(h);
                f.Request(1000, descriptionEdit: new(h.Target, "변경")); f.Update(1000); h.FinishQuery(); f.Update(1500);
                try { f.Update(2000); } catch (InvalidOperationException ex) { f.Fail(ex.Message); }
                f.Update(3000); f.Update(20000);
                check(!f.Busy && h.DescriptionEditCalls == 1 && h.SubmitCalls == 0, "failed input retried/submitted");
            }
        });
        test("truncated text and changed job restrictions cannot pass pre/post-submit verification", () => {
            foreach (var afterSubmit in new[] { false, true })
            foreach (var mode in new[] { 0, 1, 2 })
            {
                var (h, f, _) = Start("수정한 전체 내용", 48);
                var correct = h.DescriptionExpected!; var slots = correct.Slots.Slots.ToArray(); slots[47] = slots[47] with { SlotFlags = 0x100 };
                var wrong = mode switch { 0 => correct with { Description = "수정한" }, 1 => correct with { Slots = correct.Slots with { Slots = slots } }, _ => correct with { Criteria = "changed" } };
                if (!afterSubmit)
                {
                    h.DescriptionExpected = wrong; f.Update(2500);
                    check(f.Stage == RefreshStage.Stopped && h.SubmitCalls == 0, "wrong editor text/settings submitted");
                }
                else
                {
                    f.Update(2500); f.Update(4000); h.Target = wrong with { RemainingSeconds = 3598 }; h.FinishQuery(); f.Update(4500);
                    check(f.Stage == RefreshStage.Unconfirmed && h.SubmitCalls == 1, "wrong final text/settings claimed success");
                }
            }
        });
        test("invalid or combined edit requests dispatch no game actions", () => {
            var t = Target();
            var rules = ListingJobRules.Read(new[] { new RecruitmentJob(19, "나이트"), new RecruitmentJob(24, "백마도사") });
            foreach (var mode in new[] { 0, 1, 2 })
            {
                var h = new FakeHost { Target = t }; var f = new ListingRefresh(h);
                f.Request(1000, mode == 2 ? new(t, 2, 0x100, rules) : null,
                    new(t, mode == 0 ? t.Description : mode == 1 ? new string('가', 65) : "변경"));
                f.Update(1000);
                check(!f.Busy && h.QueryCalls + h.EditCalls + h.DescriptionEditCalls + h.SubmitCalls == 0, "invalid edit dispatched");
            }
        });
    }
}
