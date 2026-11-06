using Discord;
using PartyViewKR;

static class PartyMembershipTests
{
    internal static PartyRoster Roster(int count = 2, string key = "cross:20", ulong leader = 20)
    {
        var ids = new[] { 10ul }.Concat(Enumerable.Range(20, count - 1).Select(i => (ulong)i));
        if (!PartyRoster.TryCreate(10, ids, out var roster)) throw new Exception("fixture roster");
        return roster! with { PartyKey = key, LeaderIds = leader.ToString() };
    }

    internal static ListingSnapshot Listing(PartyRoster roster, int capacity = 8, bool singleVacancy = false)
    {
        var sample = ListingBridgeTests.Sample(capacity, capacity == 18);
        var ids = roster.Fingerprint.Split(',').Select(ulong.Parse).ToArray();
        var rules = sample.Slots.JobRules;
        var rows = sample.Slots.Slots.Select((s, i) => {
            var flags = singleVacancy && i >= ids.Length ? rules.Choices.Single(j => j.Id == 19).Flag : rules.AllFlags;
            return s with { MemberContentId = i < ids.Length ? ids[i] : 0, JobId = (byte)(i < ids.Length ? 25 : 0),
                Job = i < ids.Length ? "흑마도사" : "", SlotFlags = flags, AllowedJobs = rules.Describe(flags) };
        }).ToArray();
        return sample with { Owner = roster.LeaderIds == "10" ? ListingOwner.Self : ListingOwner.PartyLeader,
            LeaderContentId = ulong.Parse(roster.LeaderIds), PartyKey = roster.PartyKey,
            Slots = sample.Slots with { Slots = rows, SlotsFilled = ids.Length } };
    }

    internal static void Run(Action<string, Action> test, Action<bool, string> check)
    {
        test("member listing scope requires self membership current leader and current party identity", () => {
            var r = Roster(); var s = Listing(r);
            check(ListingPartyScope.BelongsTo(s, r) && ListingPartyScope.MatchesRoster(s, r), "member party rejected");
            foreach (var other in new[] { s with { ViewerContentId = 99 }, s with { LeaderContentId = 99 },
                s with { PartyKey = "old" }, s with { Owner = ListingOwner.Other }, s with { Owner = ListingOwner.Unknown },
                s with { Slots = s.Slots with { Slots = s.Slots.Slots.Select(x => x.MemberContentId == 10 ? x with { MemberContentId = 99 } : x).ToArray() } } })
                check(!ListingPartyScope.BelongsTo(other, r), "unrelated listing was shared");
            check(!ListingPartyScope.MatchesRoster(s, Roster(3)), "stale partial roster armed completion");
            check(!ListingPartyScope.MatchesRoster(s with { IsLive = false }, r), "closed cached detail armed completion");
        });
        test("member query confirmation never grants own refresh success or accepts a wrong detail event", () => {
            var r = Roster(); var s = Listing(r);
            check(ListingPartyScope.CanConfirm(20, false, s, r), "requested member listing not confirmed");
            check(!ListingPartyScope.CanConfirm(20, true, s, r), "member read became own refresh evidence");
            check(!ListingPartyScope.CanConfirm(21, false, s, r) && !ListingPartyScope.CanConfirm(20, false, s, null), "unbound response accepted");
            check(!ListingPartyScope.CanConfirm(20, false, s, r with { PartyKey = "new" }), "late response from old party accepted");
        });
        test("member read projection preserves all light full alliance and friendly registered slots", () => {
            foreach (var n in new[] { 4, 8, 18, 24, 48 }) {
                var r = Roster(2); var s = Listing(r, n); var view = ListingViewProjection.From(s, true, false)!;
                check(view.Groups.Sum(g => g.Slots.Length) == n && view.Groups[0].Slots[0].Job == "흑마도사", "member read lost roster " + n);
                check(ListingPartyScope.MatchesRoster(s, r), "full context did not match " + n);
            }
        });
        test("member Discord query completes with a readonly card and retains job detail navigation", () => {
            var h = new MembershipHost(); var b = new ListingBridge("test"); b.Update(h, 1000);
            var id = Guid.NewGuid().ToString(); var session = b.ReadView().Session;
            check(b.QueueQuery(id, session, 1100).Status == 202, "query not available");
            b.Update(h, 1500); h.Complete(); b.Update(h, 2000);
            var v = b.ReadView();
            check(v.Query is { State: "complete" } && v.Query.Message.Contains("참가한") && v.Listing != null, "member query did not finish");
            check(v.CanQuery && !v.CanRefresh && v.Editor == null && h.EditReads == 0, "member got edit authority");
            check(b.QueueApply(Guid.NewGuid().ToString(), session, "{}"u8.ToArray(), 2100).Status == 409 && h.Writes == 0, "member could refresh");
            var panel = new ListingDiscordPanel(b, 500, 600); var card = panel.Complete(2100).Screen!;
            var buttons = ListingDiscordCard.Walk(card.Components.Components).OfType<ButtonComponent>().ToArray();
            check(buttons.Single(x => x.Label == "조회").IsDisabled == false && buttons.Single(x => x.Label == "갱신").IsDisabled && buttons.Single(x => x.Label == "수정").IsDisabled, "readonly controls wrong");
            var detail = buttons.Single(x => x.Label == "직업 상세");
            var opened = panel.Handle(new(1, 500, 600, "", detail.CustomId, [], []), 2200);
            check(opened.Screen!.Text.Contains("등록 직업") && opened.Work == null, "member details unusable");
        });
        test("leaving a party removes its Discord listing and rejects the pending old response", () => {
            var h = new MembershipHost(); var b = new ListingBridge("test"); b.Update(h, 1000);
            b.QueueQuery(Guid.NewGuid().ToString(), b.ReadView().Session, 1100); b.Update(h, 1500);
            h.Roster = Roster(2, "regular:other"); h.Complete(); b.Update(h, 2000);
            check(b.ReadView().Listing == null && b.ReadView().Editor == null && b.ReadView().Query?.State == "running", "old party was published or confirmed");
            b.Update(h, 17000); check(b.ReadView().Query?.State == "failed" && h.Writes == 0, "old response kept pending forever");
        });
        test("member AFK detects last-seat joins and full recruitment without another listing response", () => {
            foreach (var capacity in new[] { 4, 8, 18, 24, 48 }) {
                var h = new MembershipHost { Snapshot = Listing(Roster(), capacity) };
                var m = new AfkListingMonitor(); m.Configure(new(JoinNotificationsEnabled: true), 0);
                m.SetEnabled(true, h, 0); m.Update(h, 0); h.Complete(); m.Update(h, 500);
                check(m.RecruitmentCapacity == capacity, "member recruitment did not arm " + capacity);
                h.Roster = Roster(capacity); h.Reject = true; h.QuerySucceeded = false; m.Update(h, 1000);
                check(m.RecruitmentComplete && m.CompletionRevision == 1 && m.LastJoin?.JoinedCount == capacity - 2 && h.Queries == 1,
                    "full member party waited on ended recruitment " + capacity);
            }
        });
        test("member AFK single-job exclusion requires a new matching member listing", () => {
            var h = new MembershipHost { Roster = Roster(3), Snapshot = Listing(Roster(3), 4, true) };
            var m = new AfkListingMonitor(); m.Configure(new(SingleJobCompletionEnabled: true), 0);
            m.SetEnabled(true, h, 0); m.Update(h, 0); h.Complete(); m.Update(h, 500);
            check(m.SingleJobRecruitmentComplete && !m.RecruitmentComplete && m.CompletionRevision == 1, "member single-job completion failed");
            h.Roster = Roster(4); h.Reject = true; h.QuerySucceeded = false; m.Update(h, 1000);
            check(m.RecruitmentComplete && m.CompletionRevision == 2, "member full completion did not follow single-job alert");
        });
        test("new party or leader change clears remembered capacity and does not announce existing members", () => {
            foreach (var next in new[] { Roster(4, "regular:new"), Roster(4) with { LeaderIds = "21" }, Roster(1, "solo", 10) }) {
                var h = new MembershipHost { Snapshot = Listing(Roster(), 4) };
                var m = new AfkListingMonitor(); m.Configure(new(JoinNotificationsEnabled: true), 0);
                m.SetEnabled(true, h, 0); m.Update(h, 0); h.Complete(); m.Update(h, 500);
                var context = m.PartyContextRevision;
                h.Roster = next; h.Blocked = "no listing"; m.Update(h, 1000);
                check(m.RecruitmentCapacity == 0 && !m.RecruitmentComplete && m.LastJoin == null && m.PartyContextRevision > context,
                    "old party evidence survived change");
            }
        });
        test("joining another party from solo uses a baseline but first arrival to own recruitment still alerts", () => {
            foreach (var owns in new[] { false, true }) {
                var solo = Roster(1, "solo", 10);
                var h = new MembershipHost { Roster = solo, Snapshot = Listing(solo, 4) };
                var m = new AfkListingMonitor(); m.Configure(new(JoinNotificationsEnabled: true), 0);
                m.SetEnabled(true, h, 0); m.Update(h, 0); h.Complete(); m.Update(h, 500);
                h.Roster = Roster(2, owns ? "cross:10" : "cross:20", owns ? 10ul : 20ul); m.Update(h, 1000);
                check(owns ? m.LastJoin?.JoinedCount == 1 && m.RecruitmentCapacity == 4 : m.LastJoin == null && m.RecruitmentCapacity == 0,
                    "solo transition attributed to wrong party");
            }
        });
        test("member roster read gaps retain capacity without duplicate joins on recovery", () => {
            var h = new MembershipHost(); var m = new AfkListingMonitor(); m.Configure(new(JoinNotificationsEnabled: true), 0);
            m.SetEnabled(true, h, 0); m.Update(h, 0); h.Complete(); m.Update(h, 500);
            var context = m.PartyContextRevision; h.Readable = false; m.Update(h, 1000);
            h.Readable = true; m.Update(h, 1500);
            check(m.PartyContextRevision == context && m.RecruitmentCapacity == 8 && m.LastJoin == null, "read gap changed parties");
        });
        test("multi-leader read budget remains bounded without extending queued stale work", () => {
            var g = new ListingRequestGuard(); g.Started(10, 1, 0);
            check(!g.BeforeRead(10, 1, true, true, false, 500, 55000) && !g.BeforeRead(10, 1, true, true, false, 16000, 55000), "candidate search cancelled at old limit");
            check(g.BeforeRead(10, 1, true, true, false, 55001, long.MaxValue), "unbounded candidate search");
            g.Started(10, 2, 0); check(g.BeforeRead(10, 2, true, true, false, 5001, 55000), "stale deferred dispatch allowed");
        });
    }

    private sealed class MembershipHost : IListingEditHost, IAfkListingHost
    {
        internal PartyRoster Roster = PartyMembershipTests.Roster();
        internal ListingSnapshot Snapshot = PartyMembershipTests.Listing(PartyMembershipTests.Roster());
        internal bool Readable = true, Reject;
        internal string? Blocked;
        internal int Queries, Writes, EditReads;
        public bool LoggedIn => true;
        public ulong SelfId => 10;
        public string? QueryBlockReason => Blocked;
        string? IAfkListingHost.QueryBlockReason(bool ownQueryInProgress) => Blocked;
        public string? RefreshBlockReason => null; // The bridge must enforce ownership itself.
        public bool QueryBusy { get; set; }
        public bool QuerySucceeded { get; set; }
        public long RequestRevision { get; set; }
        public long DetailRevision { get; set; }
        public ListingSnapshot Listing => Readable && ListingPartyScope.BelongsTo(Snapshot, Roster) ? Snapshot : ListingSnapshot.Empty;
        public int RegisteredCapacity => Listing.Slots.TotalSlots;
        public ListingSlots RegisteredSlots => Listing.Slots;
        public bool CurrentListingMatches(PartyRoster roster) => ListingPartyScope.MatchesRoster(Listing, roster);
        public bool TryReadRoster(out PartyRoster? roster) { roster = Readable ? Roster : null; return roster != null; }
        public bool RequestQuery(out string error) { Queries++; error = "no listing"; if (Reject) return false; RequestRevision++; QueryBusy = true; QuerySucceeded = false; return true; }
        internal void Complete() { QueryBusy = false; QuerySucceeded = true; DetailRevision++; }
        public void CancelQuery(long revision) { if (revision == RequestRevision) { QueryBusy = false; QuerySucceeded = false; } }
        public RefreshTarget? CaptureEditTarget() { EditReads++; return null; }
        public bool StartRefresh(RefreshTarget source, ListingChange[] changes, out string error) { Writes++; error = "readonly"; return false; }
        public RemoteRefreshProgress? ReadRefresh() => null;
    }
}
