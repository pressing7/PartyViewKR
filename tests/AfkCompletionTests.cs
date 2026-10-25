using PartyViewKR;

static class AfkCompletionTests
{
    internal static void Run(Action<string, Action> test, Action<bool, string> check)
    {
        (AfkListingMonitor Monitor, AfkFakeHost Host) Armed(int capacity = 8)
        {
            var m = new AfkListingMonitor(); var h = new AfkFakeHost { RegisteredCapacity = capacity };
            m.SetEnabled(true, h, 0); m.Update(h, 0); h.Complete(); m.Update(h, 500);
            check(m.RecruitmentCapacity == capacity && !m.RecruitmentComplete, "not armed from confirmed recruitment");
            return (m, h);
        }
        PartyRoster Roster(int count) => AfkFakeHost.RosterOf(Enumerable.Range(10, count).Select(x => (ulong)x).ToArray());
        test("AFK full roster notifies even after recruitment automatically ends and cannot be queried", () => {
            var (m, h) = Armed(); h.Roster = Roster(8); h.Reject = true; h.OwnReady = false; h.QuerySucceeded = false;
            m.Update(h, 1000);
            check(m.RecruitmentComplete && m.CompletionRevision == 1 && h.Queries == 1 && !m.Pending, "waited for vanished full listing");
        });
        test("AFK remembers registered capacity for ordinary sparse alliance and friendly spectator rosters", () => {
            foreach (var capacity in new[] { 4, 8, 17, 18, 23, 24, 47, 48 }) {
                var (m, h) = Armed(capacity); h.Roster = Roster(capacity - 1); h.Reject = true; m.Update(h, 3000);
                check(!m.RecruitmentComplete, "completed before all registered seats including spectators filled");
                h.Roster = Roster(capacity); m.Update(h, 3500);
                check(m.RecruitmentComplete && m.CompletionRevision == 1, "wrong registered capacity " + capacity);
            }
        });
        test("AFK failed or stale initial queries cannot arm completion from roster count alone", () => {
            var m = new AfkListingMonitor(); var h = new AfkFakeHost { Roster = Roster(8), Reject = true };
            m.SetEnabled(true, h, 0); m.Update(h, 0); m.Update(h, 5000);
            check(m.RecruitmentCapacity == 0 && m.CompletionRevision == 0 && !m.RecruitmentComplete, "guessed capacity from current party");
        });
        test("AFK full roster cancels only its in flight query before a failed result", () => {
            var (m, h) = Armed(); h.Roster = Roster(7); m.Update(h, 3000); check(m.InFlight, "query not started");
            h.Roster = Roster(8); m.Update(h, 3500);
            check(m.RecruitmentComplete && h.Cancels == 1 && !m.InFlight && m.CompletionRevision == 1, "inflight query blocked completion");
        });
        test("AFK full recruitment suppresses duplicate alerts and periodic queries of the ended listing", () => {
            var (m, h) = Armed(); m.Configure(new(true), 500); h.Roster = Roster(8); m.Update(h, 1000);
            foreach (var tick in new[] { 1500L, 61000, 4_000_000 }) m.Update(h, tick);
            check(m.CompletionRevision == 1 && h.Queries == 1 && !m.Pending, "full state repeated notification or absent listing query");
        });
        test("AFK unreadable roster does not rearm completion but a verified vacancy does", () => {
            var (m, h) = Armed(); h.Roster = Roster(8); m.Update(h, 1000);
            h.Readable = false; m.Update(h, 1500); check(!m.RecruitmentComplete, "stale roster treated as fresh");
            h.Readable = true; m.Update(h, 2000); check(m.CompletionRevision == 1, "loading caused duplicate");
            h.Roster = Roster(7); h.Reject = true; m.Update(h, 2500); h.Roster = Roster(8); m.Update(h, 3000);
            check(m.CompletionRevision == 2 && m.RecruitmentComplete, "real vacancy did not rearm");
        });
        test("AFK reset or character switch discards prior recruitment capacity", () => {
            foreach (var change in new[] { false, true }) {
                var (m, h) = Armed();
                if (change) { h.SelfId = 99; m.Update(h, 1000); } else m.SetEnabled(false, h, 1000);
                check(!m.Enabled && m.RecruitmentCapacity == 0 && !m.RecruitmentComplete, "old character capacity retained");
            }
        });
        test("AFK newer manual own query can replace an ended recruitment capacity", () => {
            var (m, h) = Armed(4); h.Roster = Roster(4); m.Update(h, 1000);
            check(m.RecruitmentComplete && m.CompletionRevision == 1, "initial completion missing");
            h.RequestQuery(out _); h.RegisteredCapacity = 8; h.Complete(); m.Update(h, 1500);
            check(m.RecruitmentCapacity == 8 && !m.RecruitmentComplete, "new recruitment hidden behind old full latch");
            h.Roster = Roster(8); h.Reject = true; m.Update(h, 2000);
            check(m.RecruitmentComplete && m.CompletionRevision == 2, "new capacity not used");
        });
        test("AFK unverified manual detail cannot replace remembered recruitment capacity", () => {
            var (m, h) = Armed(4); h.RequestRevision++; h.DetailRevision++; h.RegisteredCapacity = 8;
            h.OwnReady = false; h.Roster = Roster(4); m.Update(h, 1000);
            check(m.RecruitmentCapacity == 4 && m.RecruitmentComplete, "unverified detail changed capacity");
        });
        test("AFK capacity is not inferred from oversized roster or invalid recruitment size", () => {
            var (m, h) = Armed(4); h.Roster = Roster(8); h.Reject = true; m.Update(h, 3000);
            check(m.RecruitmentCapacity == 0 && m.CompletionRevision == 0, "oversized party falsely complete");
            foreach (var invalid in new[] { 0, 49 }) {
                var n = new AfkListingMonitor(); var g = new AfkFakeHost { RegisteredCapacity = invalid };
                n.SetEnabled(true, g, 0); n.Update(g, 0); g.Complete(); n.Update(g, 500);
                check(n.RecruitmentCapacity == 0 && n.CompletionRevision == 0, "invalid capacity accepted");
            }
        });
    }
}
