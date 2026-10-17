using System.Text.Json;
using PartyViewKR;

static partial class ListingDiscordTests
{
    private static void RunCardFlow(Action<string, Action> test, Action<bool, string> check)
    {
        test("discord separate editor preserves drafts while overview navigation stays independent", () => {
            var (b, h, _) = ListingWebEditTests.Ready();
            h.Native.Target = WebEditFakeHost.Target(24); b.Update(h, 1001);
            var p = new ListingDiscordPanel(b, App, Owner);
            var overview = p.Complete(1001).Screen!;
            var opened = p.Handle(Input(Id(overview, "settings")), 1100);
            check(!opened.Replace && opened.Screen!.Text.Contains("모집 수정"), "editor replaced overview");
            var draft = Act(p, opened.Screen!, "toggle:beginner");
            foreach (var action in new[] { "query", "apply" }) {
                var blocked = p.Handle(Input(Id(overview, action)), 1100);
                check(blocked.Work == null && !blocked.Replace && blocked.Screen!.Text.Contains("편집 카드"), "overview discarded pending edits");
            }
            overview = Act(p, overview, "group", "1");
            check(overview.Text.Contains("B파티"), "overview navigation failed during editing");
            var resumed = p.Handle(Input(Id(overview, "settings")), 1100);
            check(!resumed.Replace && resumed.Screen!.Text.Contains("적용 대기 1개"), "resuming lost draft");
            check(p.Handle(Input(Id(draft, "apply")), 1100).Work == null, "older editor remained active");
            var work = p.Handle(Input(Id(resumed.Screen!, "apply")), 1100).Work!;
            using var body = JsonDocument.Parse(work.Body!);
            check(body.RootElement.GetProperty("changes").GetArrayLength() == 1 && h.StartCalls == 0, "navigation submitted or lost changes");
        });
        test("discord grouped modal rejects malformed fields atomically and explicit empty checks turn options off", () => {
            var (b, _, _) = ListingWebEditTests.Ready(); var p = new ListingDiscordPanel(b, App, Owner);
            var draft = Act(p, Act(p, p.Complete(1000).Screen!, "settings"), "toggle:beginner");
            var modal = p.Handle(Input(Id(draft, "form:conditions")), 1100).Modal!;
            var input = FormInput(modal);
            input.Fields["objective"] = "3"; input.Fields["itemLevel"] = "1000";
            check(!p.Handle(input, 1100).Screen!.HasControls, "invalid form accepted");
            var resumed = p.Handle(Input() with { Command = "pvkr" }, 1100).Screen!;
            check(resumed.Text.Contains("적용 대기 1개") && !resumed.Text.Contains("반복 공략"), "partial modal changes leaked into draft");
            modal = p.Handle(Input(Id(resumed, "form:conditions")), 1100).Modal!;
            input = FormInput(modal); input.Choices.Clear();
            check(!p.Handle(input, 1100).Screen!.HasControls, "missing checkbox list treated as empty");
            input = FormInput(modal);
            input.Fields["objective"] = "3"; input.Fields["completion"] = "4"; input.Fields["itemLevel"] = "555";
            input.Choices["toggles"] = [];
            draft = p.Handle(input, 1100).Screen!;
            check(draft.Text.Contains("적용 대기 3개") && draft.Text.Contains("→ **555**"), "bulk edits or comparison lost");
            using var body = JsonDocument.Parse(p.Handle(Input(Id(draft, "apply")), 1100).Work!.Body!);
            var changes = body.RootElement.GetProperty("changes").EnumerateArray().ToArray();
            check(changes.Length == 3 && changes.All(c => c.GetProperty("field").GetString() != "beginner"), "unchecked flag did not return to original");
        });
        test("discord private modal keeps an existing draft PIN without echoing it and removes it when disabled", () => {
            foreach (var keep in new[] { true, false }) {
                var (b, _, _) = ListingWebEditTests.Ready(); var p = new ListingDiscordPanel(b, App, Owner);
                var draft = Text(p, p.Complete(1000).Screen!, "private", "0017");
                var modal = p.Handle(Input(Id(draft, "form:private")), 1100).Modal!;
                var input = FormInput(modal);
                check(input.Fields["password"] == "", "PIN repopulated in modal");
                input.Checks["enabled"] = keep;
                draft = p.Handle(input, 1100).Screen!;
                check(!draft.Text.Contains("0017"), "PIN leaked in card");
                using var body = JsonDocument.Parse(p.Handle(Input(Id(draft, "apply")), 1100).Work!.Body!);
                var changes = body.RootElement.GetProperty("changes");
                check(keep ? changes.GetArrayLength() == 1 && changes[0].GetProperty("password").GetString() == "0017" : changes.GetArrayLength() == 0,
                    "blank PIN discarded existing draft or disabled private retained it");
            }
        });
    }
}
