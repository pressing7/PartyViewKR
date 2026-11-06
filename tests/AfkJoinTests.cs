using System.Text.Json;
using PartyViewKR;

static class AfkJoinTests
{
    internal static void Run(Action<string, Action> test, Action<bool, string> check)
    {
        (AfkListingMonitor m, AfkFakeHost h) Start(bool alerts = true)
        {
            var h = new AfkFakeHost { Blocked = "editor" };
            var m = new AfkListingMonitor();
            m.Configure(new(JoinNotificationsEnabled: alerts), 0);
            m.SetEnabled(true, h, 0); m.Update(h, 0);
            return (m, h);
        }

        test("AFK join alerts default off and first readable roster is only a baseline", () => {
            var (m, h) = Start(false);
            h.Roster = AfkFakeHost.RosterOf(10, 20, 30); m.Update(h, 500);
            check(m.LastJoin == null && !m.Settings.JoinNotificationsEnabled, "default sent a join");
            m.Configure(new(JoinNotificationsEnabled: true), 600); m.Update(h, 1000);
            check(m.LastJoin == null, "enabling announced existing players");
        });
        test("AFK newcomer alert does not wait for a listing read or an open editor", () => {
            var (m, h) = Start();
            h.Roster = AfkFakeHost.RosterOf(10, 20, 30); m.Update(h, 500);
            check(m.LastJoin is { Revision: 1, JoinedCount: 1, MemberCount: 3 } && h.Queries == 0,
                "join depended on successful listing");
            for (var i = 1; i < 20; i++) m.Update(h, 500 + i * 500);
            check(m.LastJoin!.Revision == 1, "stable roster repeated join");
        });
        test("AFK counts newcomers by identity even with equal or lower member counts", () => {
            var (m, h) = Start();
            h.Roster = AfkFakeHost.RosterOf(10, 30); m.Update(h, 500);
            check(m.LastJoin is { JoinedCount: 1, MemberCount: 2 }, "equal-size replacement missed");
            h.Roster = AfkFakeHost.RosterOf(10, 30, 40, 50); m.Update(h, 1000);
            check(m.LastJoin is { JoinedCount: 2, MemberCount: 4 }, "multiple arrivals missed");
            h.Roster = AfkFakeHost.RosterOf(10, 60); m.Update(h, 1500);
            check(m.LastJoin is { Revision: 3, JoinedCount: 1, MemberCount: 2 }, "net decrease concealed newcomer");
        });
        test("AFK order and departures do not alert but an observed rejoin does", () => {
            var (m, h) = Start(); h.Roster = AfkFakeHost.RosterOf(20, 10); m.Update(h, 500);
            check(m.LastJoin == null, "reorder counted as arrival");
            h.Roster = AfkFakeHost.RosterOf(10); m.Update(h, 1000);
            check(m.LastJoin == null, "departure sent join");
            h.Roster = AfkFakeHost.RosterOf(10, 20); m.Update(h, 1500);
            check(m.LastJoin is { JoinedCount: 1, Revision: 1 }, "observed leave/rejoin missed");
        });
        test("AFK unreadability preserves the join baseline and catches real arrivals after recovery", () => {
            var (m, h) = Start(); h.Readable = false; h.SelfId = 0; m.Update(h, 500);
            check(m.WatchedCharacterId == 10 && m.LastJoin == null, "temporary loading lost session");
            h.Readable = true; h.SelfId = 10; m.Update(h, 1000);
            check(m.LastJoin == null, "unchanged recovery sent join");
            h.Readable = false; m.Update(h, 1500);
            h.Roster = AfkFakeHost.RosterOf(10, 20, 30); h.Readable = true; m.Update(h, 2000);
            check(m.LastJoin is { JoinedCount: 1, MemberCount: 3 }, "real arrival during gap missed");
        });
        test("AFK join option resets baseline while other preferences preserve it", () => {
            var (m, h) = Start();
            m.Configure(m.Settings with { RefreshEnabled = true, PeriodMinutes = 55 }, 100);
            h.Roster = AfkFakeHost.RosterOf(10, 20, 30); m.Update(h, 500);
            check(m.LastJoin?.Revision == 1, "unrelated option reset baseline");
            m.Configure(m.Settings with { JoinNotificationsEnabled = false }, 600);
            check(m.LastJoin == null, "disabled option retained delivery");
            h.Roster = AfkFakeHost.RosterOf(10, 20, 30, 40); m.Update(h, 1000);
            m.Configure(m.Settings with { JoinNotificationsEnabled = true }, 1100); m.Update(h, 1500);
            check(m.LastJoin == null, "re-enable replayed existing arrivals");
            h.Roster = AfkFakeHost.RosterOf(10, 20, 30, 40, 50); m.Update(h, 2000);
            check(m.LastJoin is { Revision: 2, JoinedCount: 1 }, "new episode not delivered");
        });
        test("AFK stop logout and character switch discard join events and rebaseline", () => {
            foreach (var mode in new[] { 0, 1, 2 }) {
                var (m, h) = Start(); h.Roster = AfkFakeHost.RosterOf(10, 20, 30); m.Update(h, 500);
                if (mode == 0) m.SetEnabled(false, h, 600);
                else { if (mode == 1) h.LoggedIn = false; else h.SelfId = 99; m.BeforeRead(h, 600); }
                check(!m.Enabled && m.WatchedCharacterId == 0 && m.LastJoin == null, "old character retained event");
                h.LoggedIn = true; h.SelfId = 10; m.SetEnabled(true, h, 1000); m.Update(h, 1000);
                check(m.LastJoin == null, "AFK restart replayed arrival");
                h.Roster = AfkFakeHost.RosterOf(10, 20, 30, 40); m.Update(h, 1500);
                check(m.LastJoin?.Revision == 2, "revision reused across AFK sessions");
            }
        });
        test("AFK last-seat join is detected even when full recruitment ends before another query", () => {
            var (m, h) = Start(); h.Blocked = null; h.RegisteredCapacity = 3;
            m.Update(h, 500); h.Complete(); m.Update(h, 1000);
            h.Roster = AfkFakeHost.RosterOf(10, 20, 30); m.Update(h, 1500);
            check(m.RecruitmentComplete && m.LastJoin is { JoinedCount: 1, MemberCount: 3 } && h.Queries == 1,
                "full-roster early return swallowed join");
        });
        test("AFK join counting covers light full alliance and friendly rosters", () => {
            foreach (var count in new[] { 4, 8, 18, 24, 48 }) {
                var (m, h) = Start();
                // Existing fixture includes CID20; use a known solo baseline.
                m.SetEnabled(false, h, 100); h.Roster = AfkFakeHost.RosterOf(10);
                m.SetEnabled(true, h, 200); m.Update(h, 200);
                h.Roster = AfkFakeHost.RosterOf(Enumerable.Range(10, count).Select(i => (ulong)i).ToArray()); m.Update(h, 500);
                check(m.LastJoin is { } notice && notice.JoinedCount == count - 1 && notice.MemberCount == count, "layout " + count);
            }
        });
        test("AFK join preference persists without enabling AFK or altering saved tokens and channels", () => {
            var c = JsonSerializer.Deserialize<RelayConfiguration>("{\"EncryptedDiscordToken\":\"cipher\",\"DiscordNotificationChannelId\":800}")!;
            check(!c.ReadAfkSettings().JoinNotificationsEnabled, "old config opted in");
            c.WriteAfkSettings(new(true, 55, false, 23, true, true, 30, true));
            var json = JsonSerializer.Serialize(c); var restored = JsonSerializer.Deserialize<RelayConfiguration>(json)!;
            check(restored.ReadAfkSettings() == c.ReadAfkSettings() && restored.EncryptedDiscordToken == "cipher" &&
                restored.DiscordNotificationChannelId == 800 && !json.Contains("AfkEnabled"), "settings round trip lost preferences");
        });
    }
}
