using System.Text;
using Discord;
using PartyViewKR;

static partial class ListingDiscordTests
{
    private static void RunResults(Action<string, Action> test, Action<bool, string> check)
    {
        test("discord complete results preserve every status, long line, emoji and Markdown character across pages", () => {
            var items = Enumerable.Range(1, 59).Select(i => new WebItemResult("item:" + i, "항목" + i,
                i % 3 == 0 ? null : i % 3 == 1,
                "시작" + i + " " + string.Concat(Enumerable.Repeat("한😀*_\\[]끝", i == 59 ? 1500 : 30)) + " 마지막" + i)).ToArray();
            var operation = new WebOperation("test", "partial", "Completed", "모든 처리 결과", true, true, items);
            var report = ListingDiscordSession.Describe(new(200, operation));
            check(items.All(i => report.Contains(i.Title + "\n" + i.Message)) && !report.Contains("게임의 PVKR"), "result items were omitted");
            check(report.Contains("[적용]") && report.Contains("[미적용]") && report.Contains("[확인 필요]"), "status lost");
            var (bridge, _, _) = ListingWebEditTests.Ready(); var panel = new ListingDiscordPanel(bridge, App, Owner);
            var overview = panel.Complete(1000, report).Screen!;
            var decision = panel.Handle(Input(Id(overview, "results")), 1100);
            check(!decision.Replace, "results replaced overview");
            var screen = decision.Screen!; var joined = new StringBuilder(); var pages = 0;
            while (true) {
                var content = ListingDiscordCard.Walk(screen.Components.Components).OfType<TextDisplayComponent>().ToArray();
                joined.Append(content[1].Content); pages++;
                check(screen.Text.Length <= 4000 && ListingDiscordCard.Walk(screen.Components.Components).Count() <= 40, "oversized result page");
                check(Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(content[1].Content)) == content[1].Content, "Unicode split at page boundary");
                var next = ListingDiscordCard.Walk(screen.Components.Components).OfType<ButtonComponent>().Single(b => b.Label == "다음");
                if (next.IsDisabled) break;
                screen = panel.Handle(Input(next.CustomId), 1100).Screen!;
                check(pages < 100, "pager loop");
            }
            var expected = new StringBuilder();
            foreach (var c in report) { if ("\\*_~`|>[]#".Contains(c)) expected.Append('\\'); expected.Append(c); }
            check(pages > 2 && joined.ToString() == expected.ToString(), "full result was truncated or altered");
            var previous = ListingDiscordCard.Walk(screen.Components.Components).OfType<ButtonComponent>().Single(b => b.Label == "이전");
            check(panel.Handle(Input(previous.CustomId), 1100).Screen!.Text.Contains($"{pages - 1}/{pages}"), "backwards result navigation failed");
        });
        test("discord results are read-only and do not discard a draft or accept stale page controls", () => {
            var (bridge, host, _) = ListingWebEditTests.Ready(); var panel = new ListingDiscordPanel(bridge, App, Owner);
            var overview = panel.Complete(1000, new string('한', 9000)).Screen!;
            Field(panel, overview, "beginner", "1");
            var result = Act(panel, overview, "results");
            var oldNext = Id(result, "resultpage:1");
            var prefix = oldNext[..oldNext.LastIndexOf(":resultpage:", StringComparison.Ordinal)];
            foreach (var action in new[] { "apply", "query", "settings", "jobs", "group", "form:settings", "toggle:beginner", "resultpage:-1", "resultpage:999" }) {
                var rejected = panel.Handle(Input(prefix + ":" + action), 1100);
                check(rejected.Work == null && rejected.Modal == null && !rejected.Replace, "result control became writable");
            }
            panel.Handle(Input(oldNext), 1100);
            check(!panel.Handle(Input(oldNext), 1100).Screen!.HasControls, "old page control still active");
            var draft = panel.Handle(Input(Id(overview, "settings")), 1100).Screen!;
            check(draft.Text.Contains("적용 대기 1개") && host.StartCalls == 0, "reading results changed draft or started game action");
            result = Act(panel, overview, "results");
            check(!panel.Handle(Input(Id(result, "resultpage:1")), 902000).Screen!.HasControls, "expired result accepted");
        });
        test("discord latest operation remains fully readable after another query", () => {
            var (bridge, host, _) = ListingWebEditTests.Ready(); host.Native.RuleEditAllowed = false;
            var panel = new ListingDiscordPanel(bridge, App, Owner);
            var draft = Field(panel, panel.Complete(1000).Screen!, "unrestricted", "1");
            draft = Text(panel, draft, "description", "결과 유지 확인");
            var work = panel.Handle(Input(Id(draft, "apply")), 1100).Work!;
            panel.Start(work, 1100); long tick;
            for (tick = 1500; tick < 30000; tick += 500) {
                host.Advance(tick); bridge.Update(host, tick);
                if (panel.Progress(work).Body is WebOperation { State: not ("queued" or "running") }) break;
            }
            var report = ListingDiscordSession.Describe(panel.Progress(work));
            var overview = panel.Complete(tick, report).Screen!;
            var query = panel.Handle(Input(Id(overview, "query")), tick + 1).Work!;
            panel.Start(query, tick + 1); bridge.Update(host, tick + 2);
            host.Advance(tick + 500); bridge.Update(host, tick + 500);
            overview = panel.Complete(tick + 500, ListingDiscordSession.Describe(panel.Progress(query))).Screen!;
            var results = panel.Handle(Input(Id(overview, "results")), tick + 501).Screen!;
            check(results.Text.Contains("\\[미적용\\] 제한 해제") && results.Text.Contains("\\[적용\\] 소개말") && host.StartCalls == 1,
                "query lost the previous operation results or replayed it");
        });
        test("discord unified editor groups every setting and removes standalone toggle controls", () => {
            var (bridge, _, _) = ListingWebEditTests.Ready(); var panel = new ListingDiscordPanel(bridge, App, Owner);
            var screen = Settings(panel, panel.Complete(1000).Screen!);
            var components = ListingDiscordCard.Walk(screen.Components.Components).ToArray();
            var text = components.OfType<TextDisplayComponent>().Select(t => t.Content).ToArray();
            foreach (var (title, labels) in new[] {
                ("목적", new[] { "모집 목적", "초행 및 초보자 환영" }),
                ("모집 참가 조건", new[] { "완료 조건 설정", "평균 아이템 레벨" }),
                ("임무 찾기 설정", new[] { "제한 해제", "최저 아이템 레벨로 참가", "초월하는 힘 무효화", "전리품 규칙" }),
                ("모집 범위", new[] { "비공개 설정" }), ("모집 역할", new[] { "잡 중복 없음" }) }) {
                var section = text.Single(t => t.StartsWith("### " + title + "\n"));
                check(labels.All(section.Contains), "setting misplaced: " + title);
            }
            var buttons = components.OfType<ButtonComponent>().ToArray();
            check(buttons.Count(b => b.Label == "함께 수정") == 1 && !buttons.Any(b => b.CustomId.Contains(":toggle:")), "individual toggle/forms remain");
            var roles = components.OfType<SectionComponent>().Single(s => s.Accessory is ButtonComponent { Label: "직업 수정" });
            check(roles.Components.OfType<TextDisplayComponent>().Single().Content.Contains("잡 중복 없음"), "job button not in roles");
            check(!screen.Text.Contains("변경할 항목을 고른 뒤") && screen.Text.Length < 4000 && components.Length <= 40, "editor instructions or oversized payload");
        });
    }
}
