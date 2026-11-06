using System.Text;
using System.Text.Json;
using PartyViewKR;

static class ListingBridgeTests
{
    internal static readonly JsonSerializerOptions JsonOptions = ListingBridge.JsonOptions;
    internal static RemoteView View(ListingBridge bridge) => bridge.ReadView();
    internal static ListingSnapshot Sample(int count = 8, bool friendly = false)
    {
        var jobs = ListingJobRules.Read(new RecruitmentJob[] {
            new(25, "흑마도사"), new(19, "나이트"), new(24, "백마도사"), new(21, "전사"), new(28, "학자")
        });
        var extent = friendly ? 24 : count;
        var flags = Enumerable.Repeat(jobs.AllFlags, extent).ToArray();
        var members = new ulong[extent]; var ids = new byte[extent];
        ids[0] = 25; members[0] = 112233445566778899; flags[0] = 0x4000;
        flags[1] = jobs.Choices.Where(j => j.Id is 19 or 24).Aggregate(0ul, (a, j) => a | j.Flag);
        if (friendly) foreach (var i in new[] { 5, 6, 7, 13, 14, 15 }) flags[i] = 0;
        var slots = ListingSlots.Read(true, friendly ? 18 : count, 1, friendly ? 3 : count > 8 ? count / 8 : 1,
            friendly || count > 8, ids, members, flags, _ => "흑마도사", jobs, friendly ? (byte)0x13 : (byte)0);
        return new("상세 창에서 읽은 모집글", ListingOwner.Self, "테스트 모집자", "테스트 모집 · 파티 구성 확인",
            "즐겁게 함께 공략하실 분을 모집합니다.\n직업 제한과 모집 조건을 확인해 주세요.", "59분 42초", DateTime.Now, true, "private note", "private diagnostic") {
            ViewerContentId = 10, Slots = slots, Objective = 1, Completion = 1, ItemLevel = 0,
            BeginnerFriendly = true, UniqueJobs = false, Private = false, DutyFlags = 0, LootRule = 0
        };
    }
    private static (ListingBridge Bridge, BridgeFakeHost Host, string Session) Ready(long now = 1000)
    {
        var bridge = new ListingBridge("0.3.31.0"); var host = new BridgeFakeHost(); bridge.Update(host, now);
        return (bridge, host, View(bridge).Session);
    }
    internal static void Run(Action<string, Action> test, Action<bool, string> check)
    {
        test("bridge DTO shows registered jobs and preserves native slots for 4, 8, 24 and 48", () => {
            foreach (var count in new[] { 4, 8, 24, 48 }) {
                var view = ListingViewProjection.From(Sample(count), true, false)!;
                check(view.Groups.Length == (count > 8 ? count / 8 : 1) && view.Groups.Sum(g => g.Slots.Length) == count, "wrong count");
                check(view.Groups.SelectMany(g => g.Slots).Select(s => s.NativeIndex).SequenceEqual(Enumerable.Range(0, count)), "wrong native mapping");
                check(view.Groups[0].Slots[0].Job == "흑마도사" && view.Groups[0].Slots[0].HasMember, "registered job lost");
            }
        });
        test("bridge DTO friendly match has 5, 5, 8 rows and all spectators", () => {
            var view = ListingViewProjection.From(Sample(18, true), true, false)!;
            check(view.Groups.Select(g => g.Slots.Length).SequenceEqual(new[] { 5, 5, 8 }), "padding displayed");
            check(view.Groups.Select(g => g.Name).SequenceEqual(new[] { "별빛팀", "그림자팀", "관전" }), "wrong labels");
            check(view.Groups[1].Slots[0].NativeIndex == 8 && view.Groups[2].Slots[^1].NativeIndex == 23 &&
                view.Groups[2].Slots[^1].Position == "관전 8", "truncated spectator or renumbered offset");
        });
        test("bridge DTO excludes sparse unregistered tails while keeping group numbers", () => {
            var s = Sample(48); var removed = new[] { 15, 16, 17, 18, 19, 20, 21, 22, 23 };
            var sparse = s.Slots with { TotalSlots = 39, Slots = s.Slots.Slots.Where(x => !removed.Contains(x.NativeIndex))
                .Select((x, i) => x with { Position = i + 1 }).ToArray(), UnregisteredNativeIndices = removed };
            var view = ListingViewProjection.From(s with { Slots = sparse }, true, false)!;
            check(view.Total == 39 && view.Groups.Length == 5 && view.Groups[2].Name == "D파티", "groups compacted incorrectly");
        });
        test("bridge DTO lists allowed jobs in existing order and requires complete anyone rules", () => {
            var s = Sample(); var view = ListingViewProjection.From(s, true, false)!;
            check(view.Groups[0].Slots[1].AllowedJobs.SequenceEqual(new[] { "나이트", "백마도사" }) && !view.Groups[0].Slots[1].Anyone, "restricted jobs lost");
            check(view.Groups[0].Slots[2].Anyone, "complete anyone missing");
            var rows = s.Slots.Slots.ToArray(); rows[2] = rows[2] with { AllowedJobs = new(true, ["나이트"], false) };
            view = ListingViewProjection.From(s with { Slots = s.Slots with { Slots = rows } }, true, false)!;
            check(!view.Groups[0].Slots[2].Anyone && !view.Groups[0].Slots[2].RulesComplete, "unknown rules became anyone");
        });
        test("bridge DTO never serializes content IDs, diagnostic notes or draft credentials", () => {
            var json = JsonSerializer.Serialize(ListingViewProjection.From(Sample(), true, false), JsonOptions);
            foreach (var hidden in new[] { "112233445566778899", "viewerContentId", "memberContentId", "slotFlags", "private diagnostic", "private note", "password", "pendingChanges" })
                check(!json.Contains(hidden, StringComparison.OrdinalIgnoreCase), "unexpected exported field: " + hidden);
            var view = ListingViewProjection.From(Sample(), true, false)!;
            check(view.Settings.Length == 10, "missing existing settings");
        });
        test("bridge hides foreign and unverified listings and labels cached results", () => {
            foreach (var owner in new[] { ListingOwner.Other, ListingOwner.Unknown })
                check(ListingViewProjection.From(Sample() with { Owner = owner }, true, false) == null, "not an own listing");
            check(ListingViewProjection.From(Sample(), false, false) == null && ListingViewProjection.From(Sample() with { ReadAt = null }, true, false) == null, "unread data shown");
            check(!ListingViewProjection.From(Sample() with { IsLive = false }, true, false)!.Live && !ListingViewProjection.From(Sample(), true, true)!.Live, "stale query appears live");
        });
        test("bridge marks missing and inconsistent slot data without guessing positions", () => {
            var s = Sample();
            var unreadable = ListingViewProjection.From(s with { Slots = s.Slots with { TotalSlots = 7 } }, true, false)!;
            check(!unreadable.SlotsReadable && unreadable.Total == null && unreadable.Groups.Length == 0, "bad mapping rendered");
            var inconsistent = ListingViewProjection.From(s with { Slots = s.Slots with { SlotsFilled = 2 } }, true, false)!;
            check(inconsistent.SlotsReadable && !inconsistent.CountsAgree, "inconsistent count hidden");
        });
        test("bridge Discord callback thread can only queue/read managed data and never call game host", () => {
            var (b, h, session) = Ready(); var id = Guid.NewGuid().ToString();
            Task.Run(() => { check(b.QueueQuery(id, session, 1100).Status == 202, "not queued"); b.ReadView(); b.ReadQuery(id); }).GetAwaiter().GetResult();
            check(h.Calls == 0 && View(b).Query?.State == "queued", "game called on Discord callback thread");
            b.Update(h, 1500); check(h.Calls == 1 && View(b).Query?.State == "running", "not executed by framework update");
        });
        test("bridge completion requires a fresh own live detail response", () => {
            var (b, h, session) = Ready(); b.QueueQuery(Guid.NewGuid().ToString(), session, 1100); b.Update(h, 1500);
            h.QueryBusy = false; h.QuerySucceeded = true; b.Update(h, 2000);
            check(View(b).Query?.State == "running", "old revision confirmed");
            h.DetailRevision++; h.Listing = Sample() with { Owner = ListingOwner.Other }; b.Update(h, 2500);
            check(View(b).Query?.State == "running" && View(b).Listing == null, "other listing confirmed");
            h.Listing = Sample() with { IsLive = false }; b.Update(h, 3000); check(View(b).Query?.State == "running", "cache confirmed");
            h.Listing = Sample(); b.Update(h, 3500); check(View(b).Query?.State == "complete" && h.Calls == 1, "fresh own result missing");
        });
        test("bridge duplicate ID is idempotent and second concurrent request is rejected", () => {
            var (b, h, session) = Ready(); var id = Guid.NewGuid().ToString();
            check(b.QueueQuery(id, session, 1100).Status == 202 && b.QueueQuery(id, session, 1101).Status == 200, "dedup failed");
            check(b.QueueQuery(Guid.NewGuid().ToString(), session, 1102).Status == 409, "concurrent accepted");
            b.Update(h, 1500); h.Finish(); b.Update(h, 2000);
            check(b.QueueQuery(id, session, 5000).Status == 200, "completed duplicate lost"); b.Update(h, 5001); check(h.Calls == 1, "duplicate queried again");
        });
        test("bridge request shape, old session, stale publication and rapid repeat are rejected", () => {
            var (b, h, session) = Ready();
            check(b.QueueQuery("bad", session, 1100).Status == 400 && b.QueueQuery(Guid.NewGuid().ToString(), "old", 1100).Status == 409, "invalid accepted");
            check(b.QueueQuery(Guid.NewGuid().ToString(), session, 6001).Status == 409, "stale game accepted");
            b.Update(h, 7000); b.QueueQuery(Guid.NewGuid().ToString(), session, 7001); b.Update(h, 7100); h.Finish(); b.Update(h, 7200);
            check(b.QueueQuery(Guid.NewGuid().ToString(), session, 7300).Status == 429, "rate limit missing");
        });
        test("bridge rechecks game editing and other work immediately before executing", () => {
            var (b, h, session) = Ready(); b.QueueQuery(Guid.NewGuid().ToString(), session, 1100); h.Blocked = "게임 수정창 열림"; b.Update(h, 1500);
            check(h.Calls == 0 && View(b).Query?.State == "failed" && View(b).Query?.Message == h.Blocked, "opened editor ignored");
        });
        test("bridge queued requests expire and never replay after a game stall", () => {
            var (b, h, session) = Ready(); b.QueueQuery(Guid.NewGuid().ToString(), session, 1100); b.Update(h, 6101); b.Update(h, 7000);
            check(h.Calls == 0 && View(b).Query?.State == "failed", "expired work executed");
        });
        test("bridge logout or character change clears requests and cannot expose old cached data", () => {
            foreach (var newSelf in new ulong[] { 0, 20 }) {
                var (b, h, session) = Ready(); var id = Guid.NewGuid().ToString(); b.QueueQuery(id, session, 1100);
                h.CurrentSelf = newSelf; b.Update(h, 1500);
                var view = View(b); check(view.Session != session && view.Query == null && view.Listing == null && h.Calls == 0, "old session leaked");
                check(b.ReadQuery(id).Status == 404 && b.QueueQuery(Guid.NewGuid().ToString(), session, 1600).Status == 409, "old token session usable");
            }
        });
        test("bridge stop discards pending work and publication failures remove data", () => {
            var (b, h, session) = Ready(); b.QueueQuery(Guid.NewGuid().ToString(), session, 1100); b.PublicationFailed();
            check(!View(b).CanQuery && View(b).Listing == null && View(b).Query?.State == "failed", "failure retained unsafe view");
            b.Stop(); b.Update(h, 1500); check(h.Calls == 0 && View(b).Listing == null && b.QueueQuery(Guid.NewGuid().ToString(), session, 2000).Status == 503, "stopped bridge executed");
        });
        test("bridge query errors and timeout are reported without retry", () => {
            foreach (var failure in new[] { "refuse", "throw", "timeout", "no-listing" }) {
                var (b, h, session) = Ready(); h.Failure = failure; b.QueueQuery(Guid.NewGuid().ToString(), session, 1100); b.Update(h, 1500);
                if (failure == "no-listing") { h.QueryBusy = false; h.QuerySucceeded = false; }
                b.Update(h, 17000); b.Update(h, 18000);
                check(h.Calls == 1 && View(b).Query?.State == "failed", "failed operation retried or confirmed: " + failure);
            }
        });
        test("bridge request history is bounded", () => {
            var (b, h, session) = Ready(); var first = "";
            for (var i = 0; i < 40; i++) {
                var now = 1000 + i * 3000; b.Update(h, now); var id = Guid.NewGuid().ToString(); if (i == 0) first = id;
                b.QueueQuery(id, session, now + 1); b.Update(h, now + 100); h.Finish(); b.Update(h, now + 200);
            }
            check(h.Calls == 40 && b.ReadQuery(first).Status == 404, "unbounded history or missing queries");
        });
        test("bridge native dispatch is cancelled when character, editor or enabled state changes in the next frame", () => {
            foreach (var (self, enabled, blocked) in new[] { (0ul, true, false), (20ul, true, false), (10ul, false, false), (10ul, true, true) }) {
                var guard = new ListingRequestGuard(); guard.Started(10, 7, 1000);
                check(guard.BeforeRead(self, 7, true, enabled, blocked, 1500), "deferred dispatch admitted");
            }
        });
        test("bridge cancellation ownership never cancels a later local query or refresh", () => {
            var guard = new ListingRequestGuard(); guard.Started(10, 7, 1000);
            check(!guard.Owns(8, true) && !guard.BeforeRead(20, 8, true, false, true, 9000), "new local request cancelled");
            check(!guard.Owns(7, false), "completed request cancelled");
            check(guard.Owns(7, true), "own request not cancellable"); guard.Clear(); check(!guard.Owns(7, true), "cleared request still owned");
        });
        test("bridge request flag expires before first native dispatch and bounds already started queries", () => {
            var guard = new ListingRequestGuard(); guard.Started(10, 7, 1000);
            check(guard.BeforeRead(10, 7, true, true, false, 6001), "stalled flag dispatched late");
            guard.Started(10, 8, 1000);
            check(!guard.BeforeRead(10, 8, true, true, false, 1500) && !guard.BeforeRead(10, 8, true, true, false, 9000), "normal async response cancelled");
            check(guard.BeforeRead(10, 8, true, true, false, 16001), "stalled started request continued");
        });
    }
}

sealed class BridgeFakeHost : IListingQueryHost
{
    private readonly int threadId = Environment.CurrentManagedThreadId;
    public bool CheckThread { get; set; } = true;
    public ulong CurrentSelf { get; set; } = 10;
    public ulong SelfId { get { Guard(); return CurrentSelf; } }
    public string? Blocked { get; set; }
    public string? QueryBlockReason { get { Guard(); return Blocked ?? (QueryBusy ? "조회 중" : null); } }
    public bool QueryBusy { get; set; }
    public bool QuerySucceeded { get; set; }
    public long DetailRevision { get; set; }
    public ListingSnapshot Listing { get; set; } = ListingBridgeTests.Sample();
    public int Calls { get; private set; }
    public string Failure { get; set; } = "";
    private void Guard() { if (CheckThread && Environment.CurrentManagedThreadId != threadId) throw new Exception("game access off framework thread"); }
    public bool RequestQuery(out string error) {
        Guard(); Calls++; error = "조회 실패";
        if (Failure == "throw") throw new InvalidOperationException("host failure");
        if (Failure == "refuse") return false;
        QueryBusy = true; QuerySucceeded = false; return true;
    }
    internal void Finish() { QueryBusy = false; QuerySucceeded = true; DetailRevision++; }
}
