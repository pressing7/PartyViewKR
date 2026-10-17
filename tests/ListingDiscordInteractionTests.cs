using System.Text.Json;
using PartyViewKR;

static partial class ListingDiscordConnectionTests
{
    private static void RunInteractions(Action<string, Action> test, Action<bool, string> check)
    {
        test("discord Gateway buttons without values acknowledge status and settings", () => {
            using var bot = new InteractionFixture();
            bot.Open();
            foreach (var action in new[] { "status", "settings", "main" }) {
                var before = bot.Transport.Responses.Count;
                bot.Button(action);
                bot.ScreenAfter(before);
                var response = bot.Transport.Responses.Skip(before).First();
                check(response.Method == "POST" && response.Body.GetProperty("type").GetInt32() == 7, "button did not update its message");
            }
            check(bot.Host.StartCalls == 0, "navigation started a mutation");
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
        test("discord Gateway select values and modal input survive button navigation", () => {
            using var bot = new InteractionFixture();
            bot.Open(); bot.Navigate("settings");
            bot.Select("field", "objective"); bot.Select("value", "3");
            check(bot.Screen.GetProperty("content").GetString()!.Contains("적용 대기 1개"), "select value lost");
            var before = bot.Transport.Responses.Count;
            bot.Select("field", "description", modal: true);
            var modal = bot.Transport.Responses.Skip(before).Single().Body;
            check(modal.GetProperty("type").GetInt32() == 9, "input modal not opened");
            before = bot.Transport.Responses.Count;
            bot.Transport.Interaction(5, new {
                custom_id = modal.GetProperty("data").GetProperty("custom_id").GetString(),
                components = new[] { new { type = 1, components = new[] { new { type = 4, custom_id = "value", value = "버튼 수정 확인" } } } }
            });
            bot.ScreenAfter(before);
            check(bot.Screen.GetProperty("content").GetString()!.Contains("적용 대기 2개"), "modal input lost");
            before = bot.Transport.Responses.Count; bot.Button("apply"); bot.ScreenAfter(before);
            check(bot.Host.Native.SubmitCalls == 1 && bot.Host.Native.Target.Description == "버튼 수정 확인", "modal changes did not submit");
            check(ListingObjectives.Read(bot.Host.Native.Target.Criteria) == 8, "select edit lost during submit");
        });
        test("discord Gateway unauthorized and stale buttons receive an error response", () => {
            using var bot = new InteractionFixture();
            bot.Open();
            var oldId = bot.Id("status");
            var before = bot.Transport.Responses.Count;
            bot.Transport.Interaction(3, new { component_type = 2, custom_id = oldId }, user: 601);
            bot.ScreenAfter(before, controls: false);
            var denied = bot.Transport.Responses.Skip(before).Single().Body;
            check(denied.GetProperty("type").GetInt32() == 4 && denied.GetProperty("data").GetProperty("content").GetString()!.Contains("소유자"), "unauthorized button not answered");
            bot.Open(); before = bot.Transport.Responses.Count;
            bot.Transport.Interaction(3, new { component_type = 2, custom_id = oldId });
            bot.ScreenAfter(before, controls: false);
            check(bot.Transport.Responses.Skip(before).Single().Body.GetProperty("data").GetProperty("content").GetString()!.Contains("만료"), "stale button not answered");
            check(bot.Host.StartCalls == 0, "rejected button mutated game");
        });
    }

    private sealed class InteractionFixture : IDisposable
    {
        internal readonly OfflineTransport Transport = new();
        internal readonly WebEditFakeHost Host;
        private readonly ListingWebBridge bridge;
        private readonly ListingDiscordClient client;
        private long gameTick = Environment.TickCount64;
        internal long OpenedAt;
        internal JsonElement Screen;
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
        internal string Id(string action) => Screen.GetProperty("components").EnumerateArray()
            .SelectMany(row => row.GetProperty("components").EnumerateArray())
            .Select(c => c.GetProperty("custom_id").GetString()!).Single(id => id.EndsWith(":" + action, StringComparison.Ordinal));
        internal void Button(string action) => Transport.Interaction(3, new { component_type = 2, custom_id = Id(action) });
        internal void Navigate(string action) { var before = Transport.Responses.Count; Button(action); ScreenAfter(before); }
        internal void Select(string action, string value, bool modal = false)
        {
            var before = Transport.Responses.Count;
            Transport.Interaction(3, new { component_type = 3, custom_id = Id(action), values = new[] { value } });
            if (modal) PumpUntil(() => Transport.Responses.Count > before); else ScreenAfter(before);
        }
        internal void ScreenAfter(int before, bool controls = true)
        {
            JsonElement? result = null;
            PumpUntil(() => {
                foreach (var response in Transport.Responses.Skip(before)) {
                    var data = response.Method == "PATCH" ? response.Body :
                        response.Body.TryGetProperty("data", out var d) ? d : default;
                    if (data.ValueKind != JsonValueKind.Object || !data.TryGetProperty("content", out _)) continue;
                    if (controls && (!data.TryGetProperty("components", out var c) || c.GetArrayLength() == 0)) continue;
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
