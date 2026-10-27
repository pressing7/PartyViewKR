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
        (AfkListingMonitor m, AfkFakeHost h) SingleStart(ListingSlots slots, bool enabled = true)
        {
            var m = new AfkListingMonitor(); var h = new AfkFakeHost(); SetSlots(h, slots);
            m.Configure(new(SingleJobCompletionEnabled: enabled), 0); m.SetEnabled(true, h, 0);
            m.Update(h, 0); h.Complete(); m.Update(h, 500); return (m, h);
        }
        test("single-job completion waits for actual open seats instead of subtracting a reserved count", () => {
            var (m, h) = SingleStart(Slots([4], [2, 3], [3]));
            check(!m.SingleJobRecruitmentComplete && m.CompletionRevision == 0, "open ordinary seat ignored");
            // Joining the single-job seat still leaves the ordinary seat empty.
            SetSlots(h, Slots([4], [2], [])); m.Update(h, 3000);
            check(!m.SingleJobRecruitmentComplete, "roster count alone triggered completion");
            h.Complete(); m.Update(h, 3500);
            check(!m.SingleJobRecruitmentComplete && m.CompletionRevision == 0, "reserved join falsely filled another seat");
            // A later verified recruitment has only the single-job seat vacant.
            SetSlots(h, Slots([4], [3], [3])); h.RequestQuery(out _); h.Complete(); m.Update(h, 4000);
            check(m.SingleJobRecruitmentComplete && !m.RecruitmentComplete && m.CompletionRevision == 1, "single-job vacancy not recognized");
        });
        test("single-job completion supports sparse alliances and friendly spectators using registered slots", () => {
            foreach (var groups in new[] { new[] { 4 }, [8], [7, 8, 8], [8, 8, 8, 8, 8, 7], [5, 5, 8] }) {
                var last = (groups.Length - 1) * 8 + groups[^1] - 1;
                var (m, _) = SingleStart(Slots(groups, [last], [last]));
                check(m.SingleJobRecruitmentComplete && m.CompletionRevision == 1, "sparse or spectator seat lost");
            }
        });
        test("single-job alerts keep querying and do not re-notify after repeated reads or zoning", () => {
            var (m, h) = SingleStart(Slots([4], [3], [3]));
            m.Configure(new(PeriodicEnabled: true, SingleJobCompletionEnabled: true), 1000);
            h.Readable = false; m.Update(h, 1500); check(!m.SingleJobRecruitmentComplete, "stale completion exposed");
            h.Readable = true; m.Update(h, 3000);
            check(m.InFlight && !m.SingleJobRecruitmentComplete, "partial completion suppressed queries or used old detail");
            h.Complete(); m.Update(h, 3500);
            check(m.SingleJobRecruitmentComplete && m.CompletionRevision == 1, "zoning duplicated alert");
            m.Update(h, 63500); check(m.InFlight && h.Queries == 3, "periodic queries stopped at partial completion");
            h.Complete(); m.Update(h, 64000); check(m.CompletionRevision == 1, "same successful read rearmed notification");
        });
        test("single-job completion rearms after a verified ordinary vacancy and still allows full alerts", () => {
            var (m, h) = SingleStart(Slots([4], [3], [3]));
            SetSlots(h, Slots([4], [2, 3], [3])); m.Update(h, 3000); h.Complete(); m.Update(h, 3500);
            check(!m.SingleJobRecruitmentComplete, "ordinary vacancy still complete");
            SetSlots(h, Slots([4], [3], [3])); m.Update(h, 6000); h.Complete(); m.Update(h, 6500);
            check(m.SingleJobRecruitmentComplete && m.CompletionRevision == 2, "new partial completion suppressed");
            h.Roster = AfkFakeHost.RosterOf(10, 11, 12, 13); h.OwnReady = false; h.Reject = true; m.Update(h, 7000);
            check(m.RecruitmentComplete && m.CompletionRevision == 3, "full completion waited for disappeared listing");
        });
        test("single-job completion requires opt-in and fresh matching readable job rules", () => {
            var slots = Slots([4], [3], [3]);
            var (m, h) = SingleStart(slots, false);
            check(!m.SingleJobRecruitmentComplete && m.CompletionRevision == 0, "new condition silently enabled");
            m.Configure(new(SingleJobCompletionEnabled: true), 1000); m.Update(h, 3000);
            check(m.InFlight && !m.SingleJobRecruitmentComplete, "enabled option used stale detail");
            h.Complete(); h.Listed = AfkFakeHost.RosterOf(10, 20); m.Update(h, 3500);
            check(!m.SingleJobRecruitmentComplete, "other roster accepted");
            h.Listed = h.Roster; h.RegisteredSlots = slots with { JobRules = ListingJobRules.Unavailable }; m.Update(h, 4000);
            check(!m.SingleJobRecruitmentComplete && m.CompletionRevision == 0, "unknown jobs guessed single");
            foreach (var bad in new[] { slots with { SlotsFilled = 1 }, slots with { Readable = false },
                slots with { Slots = slots.Slots.Select(s => s.HasMember ? s : s with { SlotFlags = s.SlotFlags | (1UL << 60) }).ToArray() } }) {
                var (other, _) = SingleStart(bad); check(!other.SingleJobRecruitmentComplete, "invalid or unknown slot flags accepted");
            }
        });
        test("unreadable single-job masks hide completion without rearming a duplicate alert", () => {
            var slots = Slots([4], [3], [3]); var (m, h) = SingleStart(slots);
            h.RegisteredSlots = slots with { Slots = slots.Slots.Select(s => s.HasMember ? s : s with { SlotFlags = s.SlotFlags | (1UL << 60) }).ToArray() };
            h.RequestQuery(out _); h.Complete(); m.Update(h, 1000);
            check(!m.SingleJobRecruitmentComplete && m.CompletionRevision == 1, "unknown mask confirmed a new state");
            h.RegisteredSlots = slots; h.RequestQuery(out _); h.Complete(); m.Update(h, 1500);
            check(m.SingleJobRecruitmentComplete && m.CompletionRevision == 1, "unknown mask rearmed old completion");
        });
    }

    private static ListingSlots Slots(int[] groups, int[] vacant, int[] single)
    {
        var nativeCount = groups.Length == 1 ? groups[0] : groups.Length * 8;
        var jobs = new byte[nativeCount]; var cids = new ulong[nativeCount]; var flags = new ulong[nativeCount];
        var rules = ListingJobRules.Read(new RecruitmentJob[] { new(19, "나이트"), new(24, "백마도사"), new(25, "흑마도사") });
        var only = rules.Choices.Single(j => j.Id == 19).Flag;
        for (var group = 0; group < groups.Length; group++) for (var member = 0; member < groups[group]; member++) {
            var i = group * 8 + member;
            flags[i] = single.Contains(i) ? only : rules.AllFlags;
            if (!vacant.Contains(i)) { jobs[i] = 19; cids[i] = (ulong)(10 + i); }
        }
        return ListingSlots.Read(true, groups.Sum(), cids.Count(c => c != 0), groups.Length, groups.Length > 1,
            jobs, cids, flags, _ => "나이트", rules, groups.SequenceEqual([5, 5, 8]) ? (byte)0x13 : (byte)0);
    }
    private static void SetSlots(AfkFakeHost host, ListingSlots slots)
    {
        host.RegisteredSlots = slots; host.RegisteredCapacity = slots.TotalSlots;
        host.Roster = AfkFakeHost.RosterOf(slots.Slots.Where(s => s.HasMember).Select(s => s.MemberContentId).ToArray());
    }
}
