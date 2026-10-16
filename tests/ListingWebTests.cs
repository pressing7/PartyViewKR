using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using PartyViewKR;

static class ListingWebTests
{
    internal static readonly JsonSerializerOptions JsonOptions = ListingWebBridge.JsonOptions;
    internal static WebView View(ListingWebBridge bridge) => JsonSerializer.Deserialize<WebView>(bridge.ReadState(), JsonOptions)!;
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
    private static (ListingWebBridge Bridge, WebFakeHost Host, string Session) Ready(long now = 1000)
    {
        var bridge = new ListingWebBridge("0.3.31.0"); var host = new WebFakeHost(); bridge.Update(host, now);
        return (bridge, host, View(bridge).Session);
    }
    internal static void Run(Action<string, Action> test, Action<bool, string> check)
    {
        test("web DTO shows registered jobs and preserves native slots for 4, 8, 24 and 48", () => {
            foreach (var count in new[] { 4, 8, 24, 48 }) {
                var view = ListingWebView.From(Sample(count), true, false)!;
                check(view.Groups.Length == (count > 8 ? count / 8 : 1) && view.Groups.Sum(g => g.Slots.Length) == count, "wrong count");
                check(view.Groups.SelectMany(g => g.Slots).Select(s => s.NativeIndex).SequenceEqual(Enumerable.Range(0, count)), "wrong native mapping");
                check(view.Groups[0].Slots[0].Job == "흑마도사" && view.Groups[0].Slots[0].HasMember, "registered job lost");
            }
        });
        test("web DTO friendly match has 5, 5, 8 rows and all spectators", () => {
            var view = ListingWebView.From(Sample(18, true), true, false)!;
            check(view.Groups.Select(g => g.Slots.Length).SequenceEqual(new[] { 5, 5, 8 }), "padding displayed");
            check(view.Groups.Select(g => g.Name).SequenceEqual(new[] { "별빛팀", "그림자팀", "관전" }), "wrong labels");
            check(view.Groups[1].Slots[0].NativeIndex == 8 && view.Groups[2].Slots[^1].NativeIndex == 23 &&
                view.Groups[2].Slots[^1].Position == "관전 8", "truncated spectator or renumbered offset");
        });
        test("web DTO excludes sparse unregistered tails while keeping group numbers", () => {
            var s = Sample(48); var removed = new[] { 15, 16, 17, 18, 19, 20, 21, 22, 23 };
            var sparse = s.Slots with { TotalSlots = 39, Slots = s.Slots.Slots.Where(x => !removed.Contains(x.NativeIndex))
                .Select((x, i) => x with { Position = i + 1 }).ToArray(), UnregisteredNativeIndices = removed };
            var view = ListingWebView.From(s with { Slots = sparse }, true, false)!;
            check(view.Total == 39 && view.Groups.Length == 5 && view.Groups[2].Name == "D파티", "groups compacted incorrectly");
        });
        test("web DTO lists allowed jobs in existing order and requires complete anyone rules", () => {
            var s = Sample(); var view = ListingWebView.From(s, true, false)!;
            check(view.Groups[0].Slots[1].AllowedJobs.SequenceEqual(new[] { "나이트", "백마도사" }) && !view.Groups[0].Slots[1].Anyone, "restricted jobs lost");
            check(view.Groups[0].Slots[2].Anyone, "complete anyone missing");
            var rows = s.Slots.Slots.ToArray(); rows[2] = rows[2] with { AllowedJobs = new(true, ["나이트"], false) };
            view = ListingWebView.From(s with { Slots = s.Slots with { Slots = rows } }, true, false)!;
            check(!view.Groups[0].Slots[2].Anyone && !view.Groups[0].Slots[2].RulesComplete, "unknown rules became anyone");
        });
        test("web DTO never serializes content IDs, diagnostic notes or draft credentials", () => {
            var json = JsonSerializer.Serialize(ListingWebView.From(Sample(), true, false), JsonOptions);
            foreach (var hidden in new[] { "112233445566778899", "viewerContentId", "memberContentId", "slotFlags", "private diagnostic", "private note", "password", "pendingChanges" })
                check(!json.Contains(hidden, StringComparison.OrdinalIgnoreCase), "unexpected exported field: " + hidden);
            var view = ListingWebView.From(Sample(), true, false)!;
            check(view.Settings.Length == 10, "missing existing settings");
        });
        test("web hides foreign and unverified listings and labels cached results", () => {
            foreach (var owner in new[] { ListingOwner.Other, ListingOwner.Unknown, ListingOwner.PartyLeader })
                check(ListingWebView.From(Sample() with { Owner = owner }, true, false) == null, "not an own listing");
            check(ListingWebView.From(Sample(), false, false) == null && ListingWebView.From(Sample() with { ReadAt = null }, true, false) == null, "unread data shown");
            check(!ListingWebView.From(Sample() with { IsLive = false }, true, false)!.Live && !ListingWebView.From(Sample(), true, true)!.Live, "stale query appears live");
        });
        test("web marks missing and inconsistent slot data without guessing positions", () => {
            var s = Sample();
            var unreadable = ListingWebView.From(s with { Slots = s.Slots with { TotalSlots = 7 } }, true, false)!;
            check(!unreadable.SlotsReadable && unreadable.Total == null && unreadable.Groups.Length == 0, "bad mapping rendered");
            var inconsistent = ListingWebView.From(s with { Slots = s.Slots with { SlotsFilled = 2 } }, true, false)!;
            check(inconsistent.SlotsReadable && !inconsistent.CountsAgree, "inconsistent count hidden");
        });
        test("web HTTP thread can only queue/read managed data and never call game host", () => {
            var (b, h, session) = Ready(); var id = Guid.NewGuid().ToString();
            Task.Run(() => { check(b.QueueQuery(id, session, 1100).Status == 202, "not queued"); b.ReadState(); b.ReadQuery(id); }).GetAwaiter().GetResult();
            check(h.Calls == 0 && View(b).Query?.State == "queued", "game called on HTTP thread");
            b.Update(h, 1500); check(h.Calls == 1 && View(b).Query?.State == "running", "not executed by framework update");
        });
        test("web completion requires a fresh own live detail response", () => {
            var (b, h, session) = Ready(); b.QueueQuery(Guid.NewGuid().ToString(), session, 1100); b.Update(h, 1500);
            h.QueryBusy = false; h.QuerySucceeded = true; b.Update(h, 2000);
            check(View(b).Query?.State == "running", "old revision confirmed");
            h.DetailRevision++; h.Listing = Sample() with { Owner = ListingOwner.Other }; b.Update(h, 2500);
            check(View(b).Query?.State == "running" && View(b).Listing == null, "other listing confirmed");
            h.Listing = Sample() with { IsLive = false }; b.Update(h, 3000); check(View(b).Query?.State == "running", "cache confirmed");
            h.Listing = Sample(); b.Update(h, 3500); check(View(b).Query?.State == "complete" && h.Calls == 1, "fresh own result missing");
        });
        test("web duplicate ID is idempotent and second concurrent request is rejected", () => {
            var (b, h, session) = Ready(); var id = Guid.NewGuid().ToString();
            check(b.QueueQuery(id, session, 1100).Status == 202 && b.QueueQuery(id, session, 1101).Status == 200, "dedup failed");
            check(b.QueueQuery(Guid.NewGuid().ToString(), session, 1102).Status == 409, "concurrent accepted");
            b.Update(h, 1500); h.Finish(); b.Update(h, 2000);
            check(b.QueueQuery(id, session, 5000).Status == 200, "completed duplicate lost"); b.Update(h, 5001); check(h.Calls == 1, "duplicate queried again");
        });
        test("web request shape, old session, stale publication and rapid repeat are rejected", () => {
            var (b, h, session) = Ready();
            check(b.QueueQuery("bad", session, 1100).Status == 400 && b.QueueQuery(Guid.NewGuid().ToString(), "old", 1100).Status == 409, "invalid accepted");
            check(b.QueueQuery(Guid.NewGuid().ToString(), session, 6001).Status == 409, "stale game accepted");
            b.Update(h, 7000); b.QueueQuery(Guid.NewGuid().ToString(), session, 7001); b.Update(h, 7100); h.Finish(); b.Update(h, 7200);
            check(b.QueueQuery(Guid.NewGuid().ToString(), session, 7300).Status == 429, "rate limit missing");
        });
        test("web rechecks game editing and other work immediately before executing", () => {
            var (b, h, session) = Ready(); b.QueueQuery(Guid.NewGuid().ToString(), session, 1100); h.Blocked = "게임 수정창 열림"; b.Update(h, 1500);
            check(h.Calls == 0 && View(b).Query?.State == "failed" && View(b).Query?.Message == h.Blocked, "opened editor ignored");
        });
        test("web queued requests expire and never replay after a game stall", () => {
            var (b, h, session) = Ready(); b.QueueQuery(Guid.NewGuid().ToString(), session, 1100); b.Update(h, 6101); b.Update(h, 7000);
            check(h.Calls == 0 && View(b).Query?.State == "failed", "expired work executed");
        });
        test("web logout or character change clears requests and cannot expose old cached data", () => {
            foreach (var newSelf in new ulong[] { 0, 20 }) {
                var (b, h, session) = Ready(); var id = Guid.NewGuid().ToString(); b.QueueQuery(id, session, 1100);
                h.CurrentSelf = newSelf; b.Update(h, 1500);
                var view = View(b); check(view.Session != session && view.Query == null && view.Listing == null && h.Calls == 0, "old session leaked");
                check(b.ReadQuery(id).Status == 404 && b.QueueQuery(Guid.NewGuid().ToString(), session, 1600).Status == 409, "old token session usable");
            }
        });
        test("web stop discards pending work and publication failures remove data", () => {
            var (b, h, session) = Ready(); b.QueueQuery(Guid.NewGuid().ToString(), session, 1100); b.PublicationFailed();
            check(!View(b).CanQuery && View(b).Listing == null && View(b).Query?.State == "failed", "failure retained unsafe view");
            b.Stop(); b.Update(h, 1500); check(h.Calls == 0 && View(b).Listing == null && b.QueueQuery(Guid.NewGuid().ToString(), session, 2000).Status == 503, "stopped bridge executed");
        });
        test("web query errors and timeout are reported without retry", () => {
            foreach (var failure in new[] { "refuse", "throw", "timeout", "no-listing" }) {
                var (b, h, session) = Ready(); h.Failure = failure; b.QueueQuery(Guid.NewGuid().ToString(), session, 1100); b.Update(h, 1500);
                if (failure == "no-listing") { h.QueryBusy = false; h.QuerySucceeded = false; }
                b.Update(h, 17000); b.Update(h, 18000);
                check(h.Calls == 1 && View(b).Query?.State == "failed", "failed operation retried or confirmed: " + failure);
            }
        });
        test("web request history is bounded", () => {
            var (b, h, session) = Ready(); var first = "";
            for (var i = 0; i < 40; i++) {
                var now = 1000 + i * 3000; b.Update(h, now); var id = Guid.NewGuid().ToString(); if (i == 0) first = id;
                b.QueueQuery(id, session, now + 1); b.Update(h, now + 100); h.Finish(); b.Update(h, now + 200);
            }
            check(h.Calls == 40 && b.ReadQuery(first).Status == 404, "unbounded history or missing queries");
        });
        test("web native dispatch is cancelled when character, editor or enabled state changes in the next frame", () => {
            foreach (var (self, enabled, blocked) in new[] { (0ul, true, false), (20ul, true, false), (10ul, false, false), (10ul, true, true) }) {
                var guard = new ListingWebRequestGuard(); guard.Started(10, 7, 1000);
                check(guard.BeforeRead(self, 7, true, enabled, blocked, 1500), "deferred dispatch admitted");
            }
        });
        test("web cancellation ownership never cancels a later local query or refresh", () => {
            var guard = new ListingWebRequestGuard(); guard.Started(10, 7, 1000);
            check(!guard.Owns(8, true) && !guard.BeforeRead(20, 8, true, false, true, 9000), "new local request cancelled");
            check(!guard.Owns(7, false), "completed request cancelled");
            check(guard.Owns(7, true), "own request not cancellable"); guard.Clear(); check(!guard.Owns(7, true), "cleared request still owned");
        });
        test("web request flag expires before first native dispatch and bounds already started queries", () => {
            var guard = new ListingWebRequestGuard(); guard.Started(10, 7, 1000);
            check(guard.BeforeRead(10, 7, true, true, false, 6001), "stalled flag dispatched late");
            guard.Started(10, 8, 1000);
            check(!guard.BeforeRead(10, 8, true, true, false, 1500) && !guard.BeforeRead(10, 8, true, true, false, 9000), "normal async response cancelled");
            check(guard.BeforeRead(10, 8, true, true, false, 16001), "stalled started request continued");
        });
        test("web HTTP parser rejects ambiguous framing, duplicate headers and control bytes", () => {
            var valid = "GET /api/state HTTP/1.1\r\nHost: 127.0.0.1:1234";
            check(ListingWebServer.TryParse(Encoding.ASCII.GetBytes(valid), out _), "valid GET rejected");
            foreach (var raw in new[] { valid + "\r\nHOST: another", valid + "\r\nContent-Length: 1", valid + "\r\nTransfer-Encoding: chunked", valid + "\r\nExpect: 100-continue",
                valid + "\r\n bad: folded", valid + "\r\nX-Test: x\ny", valid + "\r\nContent-Length: 0\r\nContent-Length: 0",
                "POST /api/query HTTP/1.1\r\nHost: x\r\nContent-Length: -1", "GET http://example.com/ HTTP/1.1\r\nHost: x",
                "GET /bad\tpath HTTP/1.1\r\nHost: x", "GET / HTTP/1.1\r\nX: \u0000" })
                check(!ListingWebServer.TryParse(Encoding.ASCII.GetBytes(raw), out _), "ambiguous request admitted");
        });
        test("web real listener authenticates queries, rejects other origins and serves embedded assets", () => HttpChecks(check).GetAwaiter().GetResult());
        test("web listener stop interrupts unfinished connections and restart rotates its key", () => StopChecks(check).GetAwaiter().GetResult());
    }

    private static async Task HttpChecks(Action<bool, string> check)
    {
        var (b, _, session) = Ready(Environment.TickCount64); using var server = new ListingWebServer(b);
        using var client = new HttpClient { BaseAddress = new(server.BaseUrl), Timeout = TimeSpan.FromSeconds(5) };
        var key = new Uri(server.ConnectionUrl).Fragment[5..];
        check(client.BaseAddress.Host == "127.0.0.1", "not loopback");
        using var page = await client.GetAsync("/"); var html = await page.Content.ReadAsStringAsync();
        check(page.StatusCode == HttpStatusCode.OK && html.Contains("내 모집글") && !html.Contains(key), "bad page or embedded key");
        check(page.Headers.Contains("Content-Security-Policy") && page.Headers.CacheControl?.NoStore == true, "missing response policy");
        foreach (var path in new[] { "/app.js", "/style.css" }) { using var asset = await client.GetAsync(path); check(asset.IsSuccessStatusCode, "missing embedded asset"); }
        using (var denied = await client.GetAsync("/api/state")) check(denied.StatusCode == HttpStatusCode.Unauthorized, "anonymous data allowed");
        client.DefaultRequestHeaders.Authorization = new("Bearer", key);
        using (var state = await client.GetAsync("/api/state")) check(state.StatusCode == HttpStatusCode.OK, "authenticated data missing");
        foreach (var (header, value) in new[] { ("Host", "evil.example"), ("Origin", "https://evil.example"), ("Sec-Fetch-Site", "cross-site") }) {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/state"); request.Headers.TryAddWithoutValidation(header, value);
            using var response = await client.SendAsync(request); check(response.StatusCode == HttpStatusCode.Forbidden, "foreign context accepted: " + header);
        }
        var id = Guid.NewGuid().ToString();
        using var query = new HttpRequestMessage(HttpMethod.Post, "/api/query");
        query.Headers.Add("X-PVKR-Request-Id", id); query.Headers.Add("X-PVKR-Session", session); query.Headers.Add("Origin", server.BaseUrl);
        using (var queued = await client.SendAsync(query)) check(queued.StatusCode == HttpStatusCode.Accepted, "query not queued");
        using (var result = await client.GetAsync("/api/queries/" + id)) check(result.StatusCode == HttpStatusCode.OK, "query result missing");
        foreach (var path in new[] { "/api/refresh", "/api/edit", "/private.json" }) {
            using var forbidden = await client.PostAsync(path, null); check(forbidden.StatusCode == HttpStatusCode.NotFound, "unexpected write endpoint");
        }
        using (var body = await client.PostAsync("/api/query", new StringContent("{}"))) check(body.StatusCode == HttpStatusCode.BadRequest, "body accepted");
        client.DefaultRequestHeaders.Authorization = new("Bearer", new string('0', 64));
        using (var wrong = await client.GetAsync("/api/state")) check(wrong.StatusCode == HttpStatusCode.Unauthorized, "wrong key accepted");
        server.Dispose(); await server.Completion.WaitAsync(TimeSpan.FromSeconds(3));
    }
    private static async Task StopChecks(Action<bool, string> check)
    {
        var (b, h, session) = Ready(Environment.TickCount64); using var server = new ListingWebServer(b); var oldKey = new Uri(server.ConnectionUrl).Fragment;
        using var slow = new TcpClient(); await slow.ConnectAsync("127.0.0.1", new Uri(server.BaseUrl).Port);
        await slow.GetStream().WriteAsync("GET / HTTP/1.1\r\n"u8.ToArray());
        b.QueueQuery(Guid.NewGuid().ToString(), session, Environment.TickCount64);
        server.Dispose(); await server.Completion.WaitAsync(TimeSpan.FromSeconds(3));
        check(!server.Running && View(b).Query == null && h.Calls == 0, "stop left work alive");
        using var next = new ListingWebServer(new ListingWebBridge("test"));
        check(new Uri(next.ConnectionUrl).Fragment != oldKey, "key reused"); next.Dispose(); await next.Completion.WaitAsync(TimeSpan.FromSeconds(3));
    }
}

sealed class WebFakeHost : IListingWebHost
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
    public ListingSnapshot Listing { get; set; } = ListingWebTests.Sample();
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
