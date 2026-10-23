using Discord;
using PartyViewKR;

static partial class ListingDiscordTests
{
    private static void RunRoster(Action<string, Action> test, Action<bool, string> check)
    {
        test("discord compact roster uses checked recruitment conditions and safe placeholders when assets are missing", () => {
            var host = new WebFakeHost(); var bridge = new ListingWebBridge("test"); bridge.Update(host, 1000);
            var slots = bridge.ReadView().Listing!.Groups[0].Slots;
            check(slots[0].JobId == 25, "recruitment job identity lost in managed projection");
            var icons = new ListingDiscordIcons(new Dictionary<string, ulong> { ["checked_job_25"] = 123456, ["anyone"] = 0 });
            var panel = new ListingDiscordPanel(bridge, App, Owner, icons);
            var overview = panel.Complete(1000).Screen!;
            var buttons = ListingDiscordCard.Walk(overview.Components.Components).OfType<ButtonComponent>().Select(c => c.Label).ToArray();
            check(buttons.SequenceEqual(new[] { "조회", "갱신", "수정", "직업 상세" }), "overview controls not simplified");
            check(overview.Text.Contains("<:join_black_mage_job:123456>") && overview.Text.Contains("🟨") && overview.Text.Contains("⬜"), "roster icons or fallbacks missing");
            check(!overview.Text.Contains("나이트") && !overview.Text.Contains("흑마도사"), "verbose job roster leaked into overview");
            check(new ListingDiscordIcons().ForSlot(slots[0]) == "🟦", "missing job image did not fall back");
            check(icons.ForSlot(slots[2] with { RulesComplete = false, Anyone = true }) == "⬛", "unreadable rules became anyone");
            var detail = panel.Handle(Input(Id(overview, "details")), 1100);
            check(!detail.Replace && detail.Screen!.Text.Contains("등록 직업: 흑마도사") && detail.Screen.Text.Contains("허용 직업: 나이트, 백마도사"), "read-only details unavailable without editable model");
            check(bridge.ReadView().Editor == null && host.Calls == 0, "opening details requested game changes");
        });
        test("discord job details preserve draft and full overview and reject forged editing actions", () => {
            var (bridge, host, _) = ListingWebEditTests.Ready();
            host.Native.Target = WebEditFakeHost.Target(24); bridge.Update(host, 1001);
            var panel = new ListingDiscordPanel(bridge, App, Owner);
            var overview = panel.Complete(1001).Screen!;
            var draft = Field(panel, overview, "beginner", "1");
            var details = panel.Handle(Input(Id(overview, "details")), 1100).Screen!;
            var oldGroup = Id(details, "group");
            details = Act(panel, details, "group", "2");
            check(details.Text.Contains("C파티 8") && !details.Text.Contains("적용 대기"), "details showed draft or lost slot");
            foreach (var action in new[] { "query", "apply", "settings", "jobs", "toggle:beginner" }) {
                var forged = Id(details, "group")[..^5] + action;
                var rejected = panel.Handle(Input(forged), 1100);
                check(rejected.Work == null && rejected.Modal == null && !rejected.Replace && rejected.Screen!.Text.Contains("조회 전용"), "details nonce could edit: " + action);
            }
            var reopened = panel.Handle(Input(Id(overview, "details")), 1100);
            check(!reopened.Replace && reopened.Screen!.Text.Contains("A파티 1"), "details changed overview group");
            check(!panel.Handle(Input(oldGroup, ["1"]), 1100).Replace, "older detail card remained active");
            check(overview.Text.Contains("A파티") && overview.Text.Contains("B파티") && overview.Text.Contains("C파티"), "overview omitted a party");
            check(!ListingDiscordCard.Walk(overview.Components.Components).OfType<SelectMenuComponent>().Any(), "overview still requires party selection");
            var work = panel.Handle(Input(Id(draft, "apply")), 1100).Work!;
            using var body = System.Text.Json.JsonDocument.Parse(work.Body!);
            check(body.RootElement.GetProperty("changes").GetArrayLength() == 1 && host.StartCalls == 0, "detail viewing discarded/submitted the draft");
            check(!panel.Handle(Input(Id(reopened.Screen!, "group"), ["1"]), 1100).Replace, "cleared detail remained interactive after submit");
        });
        test("discord job details preserve friendly and sparse alliance groups and never truncate allowed job lists", () => {
            foreach (var (count, friendly) in new[] { (4, false), (24, false), (48, false), (18, true) }) {
                var sample = ListingWebTests.Sample(count, friendly);
                var source = sample.Slots;
                var removed = !friendly && count > 8 ? new[] { 15 } : Array.Empty<int>();
                string[] allowed = ["나이트", "전사", "암흑기사", "건브레이커", "백마도사", "점성술사", "학자", "현자", "몽크", "용기사", "닌자", "사무라이", "리퍼", "바이퍼", "마수조련사", "음유시인", "기공사", "무도가", "흑마도사", "소환사", "적마도사", "픽토맨서", "청마도사", "검술사", "도끼술사", "환술사", "격투사", "창술사", "쌍검사", "궁술사", "주술사"];
                source = source with { TotalSlots = source.TotalSlots - removed.Length,
                    Slots = source.Slots.Where(s => !removed.Contains(s.NativeIndex)).Select((s, i) => s with {
                        Position = i + 1, AllowedJobs = s.HasMember ? s.AllowedJobs : new(false, allowed, true)
                    }).ToArray(), UnregisteredNativeIndices = [.. source.UnregisteredNativeIndices, .. removed] };
                var host = new WebFakeHost { Listing = sample with { Slots = source } };
                var bridge = new ListingWebBridge("test"); bridge.Update(host, 1000);
                var panel = new ListingDiscordPanel(bridge, App, Owner); var overview = panel.Complete(1000).Screen!;
                var details = panel.Handle(Input(Id(overview, "details")), 1100).Screen!;
                var groups = bridge.ReadView().Listing!.Groups;
                if (friendly) check(groups.Select(g => g.Slots.Length).SequenceEqual(new[] { 5, 5, 8 }), "friendly padding included");
                var icons = new ListingDiscordIcons();
                foreach (var group in groups) {
                    check(overview.Text.Contains(group.Name + " · ") && overview.Text.Contains(string.Join(" ", group.Slots.Select(icons.ForSlot))), "overview lost a party or its registered icons");
                }
                check(overview.Text.Length <= 4000 && ListingDiscordCard.Walk(overview.Components.Components).Count() <= 40, "full overview exceeds Discord limits");
                for (var i = 0; i < groups.Length; i++) {
                    if (i > 0) details = Act(panel, details, "group", i.ToString());
                    foreach (var slot in groups[i].Slots) check(details.Text.Contains("**" + slot.Position + " · "), "registered seat missing: " + slot.Position);
                    check(details.Text.Contains(string.Join(", ", allowed)) && !details.Text.Contains("…"), "long allowed-job list shortened");
                    check(details.Text.Length <= 4000 && ListingDiscordCard.Walk(details.Components.Components).Count() <= 40, "details exceed Discord limits");
                    if (groups[i].Name == "B파티" && removed.Length > 0) check(!details.Text.Contains("**B파티 8 · "), "unregistered tail displayed");
                }
            }
        });
        test("discord relative expiry stays anchored while navigating and updates only from a new snapshot", () => {
            var at = new DateTime(2026, 10, 17, 16, 0, 0, DateTimeKind.Utc);
            var host = new WebFakeHost { Listing = ListingWebTests.Sample(24) with { ReadAt = at, Remaining = "남은 시간: 35:42", IsLive = false } };
            var bridge = new ListingWebBridge("test"); bridge.Update(host, 1000);
            var panel = new ListingDiscordPanel(bridge, App, Owner);
            var overview = panel.Complete(1000).Screen!;
            var expected = $"<t:{new DateTimeOffset(at).AddSeconds(2142).ToUnixTimeSeconds()}:R>";
            check(overview.Text.Contains(expected), "relative expiry not calculated from game read time");
            host.Listing = host.Listing with { ReadAt = at.AddMinutes(5), Remaining = "60분 00초" }; bridge.Update(host, 1002);
            var details = panel.Handle(Input(Id(overview, "details")), 1100).Screen!;
            details = Act(panel, details, "group", "1");
            check(details.Text.Contains(expected) && details.Text.Contains("최신 정보는 다시 조회"), "cached timer restarted or lost stale label");
            overview = panel.Complete(1100).Screen!;
            var refreshed = $"<t:{new DateTimeOffset(at).AddMinutes(65).ToUnixTimeSeconds()}:R>";
            check(overview.Text.Contains(refreshed) && !overview.Text.Contains(expected), "new snapshot did not replace expiry");
        });
        test("discord never invents a countdown or allows detail controls after expiry or logout", () => {
            foreach (var remaining in new[] { "", "모집 종료", "99:00" }) {
                var host = new WebFakeHost { Listing = ListingWebTests.Sample(24) with { Remaining = remaining } };
                var bridge = new ListingWebBridge("test"); bridge.Update(host, 1000);
                var panel = new ListingDiscordPanel(bridge, App, Owner); var overview = panel.Complete(1000).Screen!;
                check(!overview.Text.Contains(":R>"), "unreadable countdown guessed");
                var details = panel.Handle(Input(Id(overview, "details")), 1100).Screen!;
                var oldId = Id(details, "group");
                check(!panel.Handle(Input(oldId, ["1"]), 901100).Replace, "expired details still navigated");
                host.CurrentSelf = 0; bridge.Update(host, 1001);
                var result = panel.Handle(Input(oldId, ["1"]), 1200);
                check(result.Work == null && !result.Replace && result.Screen!.Text.Contains("접속 상태"), "details survived character change");
            }
        });
    }
}
