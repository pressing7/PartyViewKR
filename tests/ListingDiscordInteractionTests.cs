using System.Text.Json;
using PartyViewKR;

static partial class ListingDiscordConnectionTests
{
    private static void RunInteractions(Action<string, Action> test, Action<bool, string> check)
    {
        test("discord Gateway buttons without values acknowledge settings", () => {
            using var bot = new InteractionFixture();
            bot.Open();
            foreach (var action in new[] { "settings", "jobs", "settings" }) {
                var before = bot.Transport.Responses.Count;
                var wasEditor = bot.Text.Contains("모집 수정");
                var oldMessageId = bot.Transport.Message!.Value.GetProperty("id").GetString();
                bot.Button(action);
                bot.ScreenAfter(before);
                var response = bot.Transport.Responses.Skip(before).First();
                var openingEditor = action == "settings" && !wasEditor;
                check(response.Method == "POST" && response.Body.GetProperty("type").GetInt32() == (openingEditor ? 4 : 7), "editor did not use a separate message");
                check((bot.Transport.Message!.Value.GetProperty("id").GetString() != oldMessageId) == openingEditor, "message identity changed incorrectly");
            }
            check(bot.Host.StartCalls == 0, "navigation started a mutation");
        });
        test("discord Gateway details use a new read-only message and preserve the overview", () => {
            using var bot = new InteractionFixture(); bot.Open();
            var overview = bot.Transport.Message!.Value;
            var editId = bot.Id("settings");
            var before = bot.Transport.Responses.Count;
            bot.Button("details"); bot.ScreenAfter(before, controls: false);
            var response = bot.Transport.Responses.Skip(before).Single();
            check(response.Body.GetProperty("type").GetInt32() == 4 && bot.Text.Contains("직업 상세") && bot.Text.Contains("등록 직업: 흑마도사"), "details replaced overview or lost listing jobs");
            check(bot.Transport.Message!.Value.GetProperty("id").GetString() != overview.GetProperty("id").GetString(), "details reused message identity");
            // A real click on the earlier overview still opens an editor.
            bot.Transport.Message = overview; before = bot.Transport.Responses.Count;
            bot.Transport.Interaction(3, new { component_type = 2, custom_id = editId });
            bot.ScreenAfter(before);
            check(bot.Text.Contains("모집 수정") && bot.Host.StartCalls == 0, "details invalidated overview or mutated game");
        });
        test("discord Gateway query and refresh buttons ACK before executing on framework", () => {
            foreach (var action in new[] { "query", "apply" }) {
                using var bot = new InteractionFixture();
                bot.Open();
                // The real bridge throttles repeat queries for two seconds.
                if (action == "query") bot.PumpUntil(() => Environment.TickCount64 >= bot.OpenedAt + 2100);
                var before = bot.Transport.Responses.Count;
                var revision = bot.Host.DetailRevision;
                bot.Button(action);
                bot.ScreenAfter(before);
                var response = bot.Transport.Responses.Skip(before).First();
                check(response.Method == "POST" && response.Body.GetProperty("type").GetInt32() == 6, "button was not deferred");
                if (action == "query") check(bot.Host.DetailRevision > revision && bot.Host.StartCalls == 0, "query did not execute");
                else check(bot.Host.StartCalls == 1 && bot.Host.Native.SubmitCalls == 1, "refresh did not submit exactly once");
            }
        });
        test("discord Gateway grouped radio and checkbox values survive modal navigation", () => {
            using var bot = new InteractionFixture();
            bot.Open(); bot.Navigate("settings");
            var conditions = bot.Modal("conditions");
            bot.Submit(conditions, [new { type = 21, custom_id = "objective", value = "3" },
                new { type = 21, custom_id = "completion", value = "1" },
                new { type = 4, custom_id = "itemLevel", value = "0" },
                new { type = 22, custom_id = "toggles", values = new[] { "beginner", "uniqueJobs" } }]);
            check(bot.Text.Contains("적용 대기 3개"), "checkbox choices lost");
            conditions = bot.Modal("conditions");
            bot.Submit(conditions, [new { type = 21, custom_id = "objective", value = "3" },
                new { type = 21, custom_id = "completion", value = "1" },
                new { type = 4, custom_id = "itemLevel", value = "0" },
                new { type = 22, custom_id = "toggles", values = Array.Empty<string>() }]);
            check(bot.Text.Contains("적용 대기 1개"), "radio value lost");
            var before = bot.Transport.Responses.Count;
            var modal = bot.Modal("description");
            bot.Submit(modal, [new { type = 4, custom_id = "value", value = "버튼 수정 확인" }]);
            check(bot.Text.Contains("적용 대기 2개"), "modal input lost");
            before = bot.Transport.Responses.Count; bot.Button("apply"); bot.ScreenAfter(before);
            check(bot.Host.Native.SubmitCalls == 1 && bot.Host.Native.Target.Description == "버튼 수정 확인", "modal changes did not submit");
            check(ListingObjectives.Read(bot.Host.Native.Target.Criteria) == 8, "select edit lost during submit");
        });
        test("discord Gateway private checkbox accepts false and keeps a blank reopened PIN", () => {
            using var bot = new InteractionFixture(); bot.Open(); bot.Navigate("settings");
            void Private(bool enabled, string password) => bot.Submit(bot.Modal("private"), [
                new { type = 23, custom_id = "enabled", value = enabled }, new { type = 4, custom_id = "password", value = password }]);
            Private(true, "0000");
            check(bot.Text.Contains("적용 대기 1개") && !bot.Text.Contains("0000"), "private input missing or PIN echoed");
            Private(false, ""); check(bot.Text.Contains("변경할 항목을 고른 뒤"), "false checkbox ignored");
            Private(true, "0000"); Private(true, "");
            check(bot.Text.Contains("적용 대기 1개"), "blank input removed pending PIN");
            var before = bot.Transport.Responses.Count; bot.Button("apply"); bot.ScreenAfter(before);
            check(bot.Host.Native.SubmitCalls == 1 && ListingPrivate.Read(bot.Host.Native.Target.ExtraCriteria) == true, "private settings not applied");
        });
        test("discord Gateway job presets and both select menus preserve every selected job", () => {
            using var bot = new InteractionFixture();
            uint[] ids = [19,21,32,37,24,33,28,40,20,22,30,34,39,41,43,23,31,38,25,27,35,42,36,1,3,6,2,4,29,5,7,26];
            bot.Host.Native.Target = bot.Host.Native.Target with { Slots = bot.Host.Native.Target.Slots with {
                JobRules = ListingJobRules.Read(ids.Select(id => new RecruitmentJob(id, "직업" + id))) } };
            bot.Open(); bot.Navigate("settings"); bot.Navigate("jobs"); bot.Navigate("preset:tank");
            string[] Selected() => WalkJson(bot.Screen.GetProperty("components"))
                .Where(c => c.TryGetProperty("custom_id", out var id) && id.GetString()!.Contains(":choosejobs:"))
                .SelectMany(c => c.GetProperty("options").EnumerateArray())
                .Where(o => o.TryGetProperty("default", out var d) && d.GetBoolean()).Select(o => o.GetProperty("value").GetString()!).ToArray();
            check(Selected().SequenceEqual(new[] { "19", "21", "32", "37" }), "tank preset included classes or other roles");
            bot.Select("choosejobs:0", "19"); bot.Select("choosejobs:1", "1", "26");
            check(Selected().SequenceEqual(new[] { "19", "1", "26" }), "select menu values lost across halves");
            var before = bot.Transport.Responses.Count; bot.Button("apply"); bot.ScreenAfter(before);
            check(bot.Host.Native.SubmitCalls == 1, "job batch did not submit");
        });
        test("discord Gateway unauthorized and stale buttons receive an error response", () => {
            using var bot = new InteractionFixture();
            bot.Open();
            var oldId = bot.Id("details");
            var before = bot.Transport.Responses.Count;
            bot.Transport.Interaction(3, new { component_type = 2, custom_id = oldId }, user: 601);
            bot.ScreenAfter(before, controls: false);
            var denied = bot.Transport.Responses.Skip(before).Single().Body;
            check(denied.GetProperty("type").GetInt32() == 4 && TextOf(denied.GetProperty("data")).Contains("소유자"), "unauthorized button not answered");
            bot.Open(); before = bot.Transport.Responses.Count;
            bot.Transport.Interaction(3, new { component_type = 2, custom_id = oldId });
            bot.ScreenAfter(before, controls: false);
            check(TextOf(bot.Transport.Responses.Skip(before).Single().Body.GetProperty("data")).Contains("만료"), "stale button not answered");
            check(bot.Host.StartCalls == 0, "rejected button mutated game");
        });
    }

    private static IEnumerable<JsonElement> WalkJson(JsonElement components)
    {
        foreach (var c in components.EnumerateArray()) {
            yield return c;
            if (c.TryGetProperty("components", out var children)) foreach (var child in WalkJson(children)) yield return child;
            if (c.TryGetProperty("accessory", out var accessory)) yield return accessory;
        }
    }
    private static string TextOf(JsonElement data) => string.Join('\n', WalkJson(data.GetProperty("components"))
        .Where(c => c.GetProperty("type").GetInt32() == 10).Select(c => c.GetProperty("content").GetString()));

    private sealed class InteractionFixture : IDisposable
    {
        internal readonly OfflineTransport Transport = new();
        internal readonly WebEditFakeHost Host;
        private readonly ListingWebBridge bridge;
        private readonly ListingDiscordClient client;
        private long gameTick = Environment.TickCount64;
        internal long OpenedAt;
        internal JsonElement Screen;
        internal string Text => TextOf(Screen);
        internal InteractionFixture()
        {
            (bridge, Host, _) = ListingWebEditTests.Ready(Environment.TickCount64);
            client = new(bridge, FakeToken, Transport.Config());
            Wait(() => Transport.Identified.Task.IsCompleted || !client.Running);
            Transport.Ready().GetAwaiter().GetResult();
            Wait(() => client.Connected || !client.Running);
            if (!client.Connected) throw new Exception(client.Status);
        }
        internal void Open()
        {
            var before = Transport.Responses.Count;
            bridge.Update(Host, Environment.TickCount64);
            OpenedAt = Environment.TickCount64;
            Transport.Interaction(2, new { id = "700", name = "pvkr", type = 1 });
            ScreenAfter(before);
        }
        internal string Id(string action) => WalkJson(Screen.GetProperty("components")).Where(c => c.TryGetProperty("custom_id", out _))
            .Select(c => c.GetProperty("custom_id").GetString()!).Single(id => id.EndsWith(":" + action, StringComparison.Ordinal));
        internal void Button(string action) => Transport.Interaction(3, new { component_type = 2, custom_id = Id(action) });
        internal void Navigate(string action) { var before = Transport.Responses.Count; Button(action); ScreenAfter(before); }
        internal string Modal(string form)
        {
            var before = Transport.Responses.Count; Button("form:" + form);
            PumpUntil(() => Transport.Responses.Count > before);
            var response = Transport.Responses.Skip(before).Single().Body;
            if (response.GetProperty("type").GetInt32() != 9) throw new Exception("Modal was not opened");
            return response.GetProperty("data").GetProperty("custom_id").GetString()!;
        }
        internal void Submit(string id, object[] fields)
        {
            var before = Transport.Responses.Count;
            Transport.Interaction(5, new { custom_id = id, components = fields.Select(c => new { type = 18, component = c }).ToArray() });
            ScreenAfter(before);
        }
        internal void Select(string action, params string[] values)
        {
            var before = Transport.Responses.Count;
            Transport.Interaction(3, new { component_type = 3, custom_id = Id(action), values });
            ScreenAfter(before);
        }
        internal void ScreenAfter(int before, bool controls = true)
        {
            JsonElement? result = null;
            PumpUntil(() => {
                foreach (var response in Transport.Responses.Skip(before)) {
                    var data = response.Method == "PATCH" ? response.Body :
                        response.Body.TryGetProperty("data", out var d) ? d : default;
                    if (data.ValueKind != JsonValueKind.Object || !data.TryGetProperty("components", out var c)) continue;
                    if (controls && !WalkJson(c).Any(x => x.GetProperty("type").GetInt32() is 2 or 3)) continue;
                    result = data;
                }
                return result.HasValue;
            });
            Screen = result!.Value;
        }
        internal void PumpUntil(Func<bool> completed)
        {
            var timeout = Environment.TickCount64 + 5000;
            while (!completed()) {
                if (Environment.TickCount64 >= timeout) throw new Exception("Discord did not respond to the Gateway interaction");
                // Only this thread touches the fake game, just like Framework.Update.
                Host.Advance(gameTick += 500); bridge.Update(Host, Environment.TickCount64);
                Thread.Sleep(1);
            }
        }
        public void Dispose()
        {
            client.Dispose(); client.Completion.WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
        }
    }
}
