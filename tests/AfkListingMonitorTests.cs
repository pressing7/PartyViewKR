using PartyViewKR;

static class AfkListingMonitorTests
{
    internal static void Run(Action<string, Action> test, Action<bool, string> check)
    {
        (AfkListingMonitor m, AfkFakeHost h) Start(bool complete = false)
        {
            var h = new AfkFakeHost(); var m = new AfkListingMonitor();
            check(m.SetEnabled(true, h, 0), "enable"); m.Update(h, 0);
            if (complete) { h.Complete(); m.Update(h, 1000); }
            return (m, h);
        }

        test("AFK disabled does not read a roster or query", () => {
            var h = new AfkFakeHost(); var m = new AfkListingMonitor();
            m.BeforeRead(h, 100000); m.Update(h, 100000);
            check(h.RosterReads == 0 && h.Queries == 0, "background work while disabled");
        });
        test("AFK enable requires a loaded character", () => {
            var h = new AfkFakeHost { SelfId = 0 }; var m = new AfkListingMonitor();
            check(!m.SetEnabled(true, h, 0) && !m.Enabled, "enabled without identity");
        });
        test("AFK enabling queries once and requires a fresh own listing", () => {
            var (m, h) = Start(); m.Update(h, 500);
            check(h.Queries == 1 && m.InFlight && m.Pending, "duplicate query or early completion");
            h.Complete(); m.Update(h, 1000);
            check(!m.Pending && !m.InFlight && m.Enabled, "fresh listing not accepted");
        });
        test("AFK identity ignores member order and supports solo regular and full alliances", () => {
            check(PartyRoster.TryCreate(10, [20, 10], out var a) &&
                PartyRoster.TryCreate(10, [10, 20], out var b) && a == b, "order treated as membership");
            foreach (var count in new[] { 1, 4, 8, 18, 24, 48 })
                check(PartyRoster.TryCreate(10, Enumerable.Range(10, count).Select(n => (ulong)n), out var roster) &&
                    roster!.Count == count, "layout coverage " + count);
        });
        test("AFK rejects partial zero duplicate foreign or oversized rosters", () => {
            foreach (var ids in new ulong[][] { [], [10, 0], [10, 10], [20, 30], Enumerable.Range(1, 49).Select(n => (ulong)n).ToArray() })
                check(!PartyRoster.TryCreate(10, ids, out _), "invalid roster accepted");
        });
        test("AFK stable identities do not cause repeated immediate queries", () => {
            var (m, h) = Start(true);
            h.Roster = AfkFakeHost.RosterOf(20, 10);
            for (var now = 1500; now < 60000; now += 500) m.Update(h, now);
            check(h.Queries == 1 && !m.Pending, "unchanged identities caused query");
        });
        test("AFK join triggers a new query", () => {
            var (m, h) = Start(true); h.Roster = AfkFakeHost.RosterOf(10, 20, 30); m.Update(h, 3000);
            check(h.Queries == 2 && m.Pending, "join not queried");
            h.Complete(); m.Update(h, 3500); check(!m.Pending, "join result not confirmed");
        });
        test("AFK same count replacement triggers a new query", () => {
            var (m, h) = Start(true); h.Roster = AfkFakeHost.RosterOf(10, 30); m.Update(h, 3000);
            check(h.Queries == 2 && m.Pending, "replacement missed");
        });
        test("AFK returning to the original roster keeps an observed departure pending", () => {
            var (m, h) = Start(true); h.Blocked = "editor";
            h.Roster = AfkFakeHost.RosterOf(10); m.Update(h, 3000);
            h.Roster = AfkFakeHost.RosterOf(10, 20); m.Update(h, 3500);
            check(m.Pending && h.Queries == 1, "departure was forgotten");
            h.Blocked = null; m.Update(h, 4000); check(h.Queries == 2, "return to original cancelled query");
        });
        test("AFK changes during a query require another query even after returning", () => {
            var (m, h) = Start(); h.Roster = AfkFakeHost.RosterOf(10); m.Update(h, 500);
            h.Roster = AfkFakeHost.RosterOf(10, 20); m.Update(h, 1000);
            h.Complete(); m.Update(h, 1500);
            check(m.Pending && !m.InFlight, "inflight change lost");
            m.Update(h, 3499); check(h.Queries == 1, "missing query spacing");
            m.Update(h, 3500); h.Complete(); m.Update(h, 4000);
            check(h.Queries == 2 && !m.Pending, "followup not completed");
        });
        test("AFK supplement catches unobserved leave and rejoin with unchanged identities", () => {
            var (m, h) = Start(true);
            m.Update(h, 60999); check(h.Queries == 1, "early supplement");
            m.Update(h, 61000); check(h.Queries == 2 && m.Pending, "missing periodic supplement");
            h.Complete(); m.Update(h, 61500); m.Update(h, 121499);
            check(h.Queries == 2, "supplement not reset by success");
            m.Update(h, 121500); check(h.Queries == 3, "second supplement missing");
        });
        test("AFK failed query remains pending without another roster change", () => {
            var (m, h) = Start(); h.QueryBusy = false; h.QuerySucceeded = false; m.Update(h, 500);
            check(m.Pending && !m.InFlight, "failed request forgotten");
            m.Update(h, 5499); check(h.Queries == 1, "retried without delay");
            m.Update(h, 5500); check(h.Queries == 2, "no retry on unchanged roster");
            h.Complete(); m.Update(h, 6000); check(!m.Pending, "retry success not consumed");
        });
        test("AFK repeated start failures back off and retain pending work", () => {
            var h = new AfkFakeHost { Reject = true }; var m = new AfkListingMonitor(); m.SetEnabled(true, h, 0);
            m.Update(h, 0); m.Update(h, 4999); check(h.Queries == 1, "first backoff");
            m.Update(h, 5000); m.Update(h, 14999); check(h.Queries == 2, "second backoff");
            m.Update(h, 15000); check(h.Queries == 3 && m.Pending, "retry pending lost");
        });
        test("AFK editor or manual work defers pending queries", () => {
            var h = new AfkFakeHost { Blocked = "busy" }; var m = new AfkListingMonitor(); m.SetEnabled(true, h, 0);
            m.Update(h, 0); m.Update(h, 60000); check(h.Queries == 0 && m.Pending, "query bypassed block");
            h.Blocked = null; m.Update(h, 60500); check(h.Queries == 1, "pending not resumed");
        });
        test("AFK queued dispatch is cancelled if editor opens before read", () => {
            var (m, h) = Start(); h.Blocked = "editor"; m.BeforeRead(h, 500);
            check(h.Cancels == 1 && !m.InFlight && m.Pending, "deferred request escaped guard");
            h.Blocked = null; m.Update(h, 5500); check(h.Queries == 2, "guard cancellation not retried");
        });
        test("AFK unreadable observations preserve membership and resume pending work", () => {
            var (m, h) = Start(true); h.Readable = false; m.Update(h, 3000); m.Update(h, 60000);
            check(h.Queries == 1 && m.Pending && m.Diagnostic.Contains("afkMemberCount=2"), "invalid read replaced roster");
            h.Readable = true; m.Update(h, 60500); check(h.Queries == 2, "recovery lost work");
        });
        test("AFK incomplete observation during a query prevents false completion", () => {
            var (m, h) = Start(); h.Readable = false; m.Update(h, 500); h.Complete(); m.Update(h, 1000);
            check(m.InFlight && m.Pending, "unreadable request marked complete");
            h.Readable = true; m.Update(h, 1500); check(m.Pending && !m.InFlight, "uncertain interval forgotten");
        });
        test("AFK old detail revision cannot finish a request", () => {
            var (m, h) = Start(); h.Complete(); h.DetailRevision--; m.Update(h, 1000);
            check(m.InFlight && m.Pending, "old detail accepted");
        });
        test("AFK unverified owner or mismatched listing membership stays pending", () => {
            var (m, h) = Start(); h.Complete(); h.Listed = AfkFakeHost.RosterOf(10, 30); m.Update(h, 1000);
            check(m.InFlight && m.Pending, "other members accepted");
            h.Listed = h.Roster; h.OwnReady = false; m.Update(h, 1500);
            check(m.InFlight && m.Pending, "unverified owner accepted");
            h.OwnReady = true; m.Update(h, 2000); check(!m.Pending, "valid detail not accepted");
        });
        test("AFK no own listing times out and retries instead of clearing pending", () => {
            var (m, h) = Start(); h.Complete(); h.OwnReady = false; m.Update(h, 15000);
            check(!m.InFlight && m.Pending, "missing listing did not timeout");
            m.Update(h, 20000); check(h.Queries == 2, "missing listing not retried");
        });
        test("AFK temporary player unavailability does not disable the mode", () => {
            var (m, h) = Start(); h.SelfId = 0; h.Readable = false; h.Blocked = "player unloaded";
            m.BeforeRead(h, 500); m.Update(h, 500);
            check(m.Enabled && m.Pending && h.Cancels == 1, "temporary state treated as logout");
            h.SelfId = 10; h.Readable = true; h.Blocked = null; m.Update(h, 5500);
            check(h.Queries == 2, "did not resume");
        });
        test("AFK stopping cancels only its own active request", () => {
            var (m, h) = Start(); m.SetEnabled(false, h, 500); m.Update(h, 60000);
            check(!m.Enabled && !m.Pending && h.Cancels == 1 && h.Queries == 1, "stop retained work");
        });
        test("AFK stopping cannot cancel a newer manual query", () => {
            var (m, h) = Start(); h.RequestRevision++; m.SetEnabled(false, h, 500);
            check(h.Cancels == 0 && h.QueryBusy, "cancelled another owner");
        });
        test("AFK replaced request cannot consume a newer query result", () => {
            var (m, h) = Start(); h.RequestRevision++; h.Complete(); m.Update(h, 500);
            check(m.Pending && !m.InFlight && h.Cancels == 0, "accepted another query result");
        });
        test("AFK logout or character switch disables session and cannot resume later", () => {
            foreach (var logout in new[] { true, false })
            {
                var (m, h) = Start(); if (logout) h.LoggedIn = false; else h.SelfId = 99;
                m.BeforeRead(h, 500); h.LoggedIn = true; h.SelfId = 10; m.Update(h, 100000);
                check(!m.Enabled && h.Queries == 1 && h.Cancels == 1, "old session resumed");
            }
        });
        test("AFK request exception after acceptance waits without duplicate dispatch", () => {
            var h = new AfkFakeHost { ThrowAfter = true }; var m = new AfkListingMonitor(); m.SetEnabled(true, h, 0);
            m.Update(h, 0); m.Update(h, 500); check(m.InFlight && h.Queries == 1, "accepted request repeated");
            h.Complete(); m.Update(h, 1000); check(!m.Pending, "accepted exception result lost");
        });
    }
}

sealed class AfkFakeHost : IAfkListingHost
{
    public bool LoggedIn { get; set; } = true;
    public ulong SelfId { get; set; } = 10;
    internal PartyRoster Roster { get; set; } = RosterOf(10, 20);
    internal PartyRoster? Listed { get; set; }
    internal bool Readable = true, OwnReady, Reject, ThrowAfter;
    internal string? Blocked;
    internal int RosterReads, Queries, Cancels;
    public bool QueryBusy { get; set; }
    public bool QuerySucceeded { get; set; }
    public long RequestRevision { get; set; }
    public long DetailRevision { get; set; }
    public bool TryReadRoster(out PartyRoster? roster) { RosterReads++; roster = Readable ? Roster : null; return Readable; }
    public string? QueryBlockReason(bool ownQueryInProgress) => Blocked ?? (QueryBusy && !ownQueryInProgress ? "query busy" : null);
    public bool OwnListingMatches(PartyRoster roster) => OwnReady && Listed == roster;
    public bool RequestQuery(out string error)
    {
        Queries++; error = Reject ? "rejected" : "";
        if (Reject) return false;
        RequestRevision++; QueryBusy = true; QuerySucceeded = false; OwnReady = false;
        if (ThrowAfter) throw new InvalidOperationException("accepted before exception");
        return true;
    }
    public void CancelQuery(long revision) { if (revision == RequestRevision && QueryBusy) { QueryBusy = false; QuerySucceeded = false; Cancels++; } }
    internal void Complete() { QueryBusy = false; QuerySucceeded = true; OwnReady = true; DetailRevision++; Listed = Roster; }
    internal static PartyRoster RosterOf(params ulong[] ids) { if (!PartyRoster.TryCreate(10, ids, out var roster)) throw new Exception("bad fixture"); return roster!; }
}
