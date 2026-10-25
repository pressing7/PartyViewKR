using System.Text.Json;
using PartyViewKR;

static class AfkSettingsTests
{
    internal static void Run(Action<string, Action> test, Action<bool, string> check)
    {
        (AfkListingMonitor, AfkFakeHost) Start(AfkSettings settings, bool complete = true)
        {
            var m = new AfkListingMonitor(); var h = new AfkFakeHost();
            m.Configure(settings, 0); m.SetEnabled(true, h, 0); m.Update(h, 0);
            if (complete) { h.Complete(); m.Update(h, 1000); }
            return (m, h);
        }
        test("AFK periodic queries default off while membership queries remain enabled", () => {
            var (m, h) = Start(new()); m.Update(h, 4_000_000);
            check(h.Queries == 1 && !m.Pending, "periodic default still enabled");
            h.Roster = AfkFakeHost.RosterOf(10, 30); m.Update(h, 4_000_500);
            check(h.Queries == 2, "membership monitoring disabled with periodic option");
        });
        test("AFK period boundaries are exactly one and fifty five minutes", () => {
            foreach (var minutes in new[] { 1, 55 }) {
                var (m, h) = Start(new(true, minutes)); var due = 1000 + minutes * 60000L;
                m.Update(h, due - 1); check(h.Queries == 1, "period too early");
                m.Update(h, due); check(h.Queries == 2, "period not honored");
            }
        });
        test("AFK invalid persisted intervals clamp to supported bounds", () => {
            check(new AfkSettings(true, -10, true, 0).Normalize() == new AfkSettings(true, 1, true, 1), "lower bound");
            check(new AfkSettings(true, int.MaxValue, true, int.MaxValue).Normalize() == new AfkSettings(true, 55, true, 3600), "upper bound");
        });
        test("AFK disabling an overdue blocked periodic query removes only the periodic trigger", () => {
            var (m, h) = Start(new(true)); h.Blocked = "editor"; m.Update(h, 61000);
            check(m.Pending, "missing due work"); m.Configure(new(false), 62000); h.Blocked = null; m.Update(h, 62500);
            check(h.Queries == 1 && !m.Pending, "disabled timer still dispatched");
        });
        test("AFK changing the period starts a fresh interval", () => {
            var (m, h) = Start(new(true)); m.Configure(new(true, 2), 10000);
            m.Update(h, 129999); check(h.Queries == 1, "old period still active");
            m.Update(h, 130000); check(h.Queries == 2, "new period ignored");
        });
        test("AFK disabled retries retain failure without repeated unchanged queries", () => {
            var (m, h) = Start(new(RetryEnabled: false), false); h.QueryBusy = false; m.Update(h, 500);
            m.Update(h, 60000); m.Update(h, 4_000_000);
            check(h.Queries == 1 && m.Pending && !m.InFlight, "retry-off loop");
            h.Roster = AfkFakeHost.RosterOf(10, 30); m.Update(h, 4_000_500);
            check(h.Queries == 2, "retry-off swallowed new membership event");
        });
        test("AFK periodic timer stays independent when failure retry is disabled", () => {
            var (m, h) = Start(new(true, 1, false, 3600), false); h.QueryBusy = false; m.Update(h, 500);
            m.Update(h, 60499); check(h.Queries == 1, "failed request repeated early");
            m.Update(h, 60500); check(h.Queries == 2, "disabled long retry delay blocked periodic timer");
        });
        test("AFK configured retry seconds replace exponential backoff", () => {
            var h = new AfkFakeHost { Reject = true }; var m = new AfkListingMonitor();
            m.Configure(new(RetrySeconds: 23), 0); m.SetEnabled(true, h, 0); m.Update(h, 0);
            foreach (var tick in new[] { 23000L, 46000L, 69000L }) {
                var count = h.Queries; m.Update(h, tick - 1); check(h.Queries == count, "early retry");
                m.Update(h, tick); check(h.Queries == count + 1, "fixed retry delay changed");
            }
        });
        test("AFK periodic queries are not postponed by a longer enabled retry interval", () => {
            var (m, h) = Start(new(true, 1, true, 3600), false); h.QueryBusy = false; m.Update(h, 500);
            m.Update(h, 60499); check(h.Queries == 1, "early periodic retry");
            m.Update(h, 60500); check(h.Queries == 2, "long failure delay blocked periodic option");
        });
        test("AFK toggling retry while waiting reschedules instead of dispatching immediately", () => {
            var (m, h) = Start(new(), false); h.QueryBusy = false; m.Update(h, 500);
            m.Configure(new(RetryEnabled: false), 1000); m.Update(h, 6000); check(h.Queries == 1, "old retry remained");
            m.Configure(new(RetrySeconds: 10), 7000); m.Update(h, 16999); check(h.Queries == 1, "early enabled retry");
            m.Update(h, 17000); check(h.Queries == 2, "enabled retry lost");
        });
        test("AFK settings persist without changing saved connection data or enabling AFK", () => {
            var c = JsonSerializer.Deserialize<RelayConfiguration>("{\"EncryptedDiscordToken\":\"ciphertext\",\"ServerUrl\":\"https://example.invalid\"}")!;
            check(c.ReadAfkSettings() == new AfkSettings(), "old configuration defaults");
            c.WriteAfkSettings(new(true, 55, false, 17));
            var json = JsonSerializer.Serialize(c); var restored = JsonSerializer.Deserialize<RelayConfiguration>(json)!;
            check(restored.ReadAfkSettings() == new AfkSettings(true, 55, false, 17) && restored.EncryptedDiscordToken == "ciphertext" &&
                restored.ServerUrl == c.ServerUrl && !json.Contains("AfkEnabled"), "settings persistence altered unrelated state");
        });
    }
}
