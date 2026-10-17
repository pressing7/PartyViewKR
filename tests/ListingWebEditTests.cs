using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using PartyViewKR;

static class ListingWebEditTests
{
    internal static byte[] Body(string revision, params object[] changes) => JsonSerializer.SerializeToUtf8Bytes(new { revision, changes });
    internal static (ListingWebBridge Bridge, WebEditFakeHost Host, WebView View) Ready(long now = 1000)
    {
        var bridge = new ListingWebBridge("0.3.32.0"); var host = new WebEditFakeHost { Tick = now };
        bridge.Update(host, now); return (bridge, host, ListingWebTests.View(bridge));
    }
    private static void Step(ListingWebBridge b, WebEditFakeHost h, long tick) { h.Advance(tick); b.Update(h, tick); }
    private static WebOperation Finish(ListingWebBridge b, WebEditFakeHost h, long tick = 1500)
    {
        for (var i = 0; i < 160; i++, tick += 500) {
            Step(b, h, tick); var operation = ListingWebTests.View(b).Operation!;
            if (operation.State is not ("queued" or "running")) return operation;
        }
        throw new Exception("web edit did not finish");
    }
    internal static void Run(Action<string, Action> test, Action<bool, string> check)
    {
        test("web edit source revision ignores time but changes with members or conditions", () => {
            var (b, h, view) = Ready(); var revision = view.Editor!.Revision;
            h.Native.Target = h.Native.Target with { RemainingSeconds = 1700 }; b.Update(h, 1500);
            check(ListingWebTests.View(b).Editor!.Revision == revision, "timer invalidated a draft");
            var rows = h.Native.Target.Slots.Slots.ToArray(); rows[1] = rows[1] with { MemberContentId = 20, JobId = 19 };
            h.Native.Target = h.Native.Target with { Slots = h.Native.Target.Slots with { Slots = rows, SlotsFilled = 2 } }; b.Update(h, 2000);
            check(ListingWebTests.View(b).Editor!.Revision != revision, "participant change retained old draft");
        });
        test("web edit models expose ordered job IDs only for empty registered slots including friendly spectators", () => {
            foreach (var (count, friendly) in new[] { (4, false), (24, false), (48, false), (18, true) }) {
                var target = WebEditFakeHost.Target(count, friendly); var model = ListingWebEdit.Model(target, "revision");
                check(model.Slots.Length == count - 1 && !model.Slots.Any(s => s.NativeIndex == 0), "occupied or missing slots");
                check(model.Jobs.Select(j => j.Id).SequenceEqual(target.Slots.JobRules.Choices.Select(j => j.Id)), "job order changed");
                if (friendly) check(!model.Slots.Any(s => new[] { 5, 6, 7, 13, 14, 15 }.Contains(s.NativeIndex)) && model.Slots[^1].NativeIndex == 23, "friendly padding edited");
            }
        });
        test("web decoder accepts all existing settings, leading-zero PIN and native slot IDs", () => {
            var t = WebEditFakeHost.Target(18, true);
            var body = Body("r", new { field = "description", value = "새 소개말" }, new { field = "objective", value = 3 },
                new { field = "beginner", value = true }, new { field = "uniqueJobs", value = true },
                new { field = "private", value = true, password = "0000" }, new { field = "completion", value = 4 },
                new { field = "itemLevel", value = 999 }, new { field = "unrestricted", value = 1 },
                new { field = "minimum_item_level", value = 1 }, new { field = "silence_echo", value = 1 },
                new { field = "loot", value = 2 }, new { field = "job", slot = 23, jobs = new[] { 19 } });
            check(ListingWebEdit.Decode(body, t, "r", out var changes, out var error), error);
            check(changes.Length == 12 && changes[^1].NativeIndex == 23 && ((ListingJobEdit)changes[^1].Edit).Position == 18, "field or sparse mapping lost");
            check(changes.Select(c => c.Edit).OfType<ListingPrivateEdit>().Single().Password == "0000", "PIN zero lost");
        });
        test("web decoder rejects unknown, duplicate, malformed or oversized changes atomically", () => {
            var t = WebEditFakeHost.Target();
            foreach (var patch in new object[][] {
                [new { field = "callback", value = 11 }], [new { field = "objective", value = 4 }],
                [new { field = "itemLevel", value = 1000 }], [new { field = "itemLevel", value = -1 }],
                [new { field = "completion", value = 8 }], [new { field = "loot", value = 3 }],
                [new { field = "private", value = true, password = "123" }], [new { field = "private", value = true, password = "１２３４" }],
                [new { field = "description", value = new string('한', 65) }], [new { field = "description", value = "x\u0000y" }],
                [new { field = "job", slot = 0, jobs = new[] { 19 } }], [new { field = "job", slot = 3, jobs = new[] { 999 } }],
                [new { field = "job", slot = 3, jobs = new[] { 19, 19 } }], [new { field = "job", slot = 3, jobs = Array.Empty<int>() }],
                [new { field = "beginner", value = true }, new { field = "beginner", value = false }],
                [new { field = "description", value = "valid" }, new { field = "unrestricted", value = 7 }],
                [new { field = "itemLevel", value = "555" }], [new { field = "description", value = "x", address = 123 }]
            }) check(!ListingWebEdit.Decode(Body("r", patch), t, "r", out var changes, out _) && changes.Length == 0, "malformed mutation admitted");
            foreach (var raw in new[] { "null", "[]", "{", "{\"revision\":\"r\",\"revision\":\"r\",\"changes\":[]}",
                "{\"revision\":\"r\",\"changes\":[{\"field\":\"description\",\"value\":null}]}",
                "{\"revision\":\"r\",\"changes\":[{\"field\":\"objective\",\"value\":1,\"value\":2}]}" })
                check(!ListingWebEdit.Decode(Encoding.UTF8.GetBytes(raw), t, "r", out _, out _), "malformed JSON accepted");
            check(!ListingWebEdit.Decode(new byte[65537], t, "r", out _, out _), "oversized body");
            check(!ListingWebEdit.Decode(Body("old"), t, "r", out _, out _), "stale revision accepted");
        });
        test("web friendly unregistered slots cannot be requested, unchanged fields become plain refresh", () => {
            var t = WebEditFakeHost.Target(18, true);
            check(!ListingWebEdit.Decode(Body("r", new { field = "job", slot = 5, jobs = new[] { 19 } }), t, "r", out _, out _), "padding editable");
            check(ListingWebEdit.Decode(Body("r", new { field = "description", value = t.Description },
                new { field = "private", value = false }), t, "r", out var changes, out _) && changes.Length == 0, "no-op not removed");
        });
        test("web writes stay on framework thread, execute once and never consume local pending changes", () => {
            var (b, h, v) = Ready(); var id = Guid.NewGuid().ToString(); var local = new ListingPendingChanges();
            local.Queue(ListingBatchTests.Description(h.Native.Target, "local draft"), out _);
            var body = Body(v.Editor!.Revision, new { field = "description", value = "browser draft" });
            var replies = Task.Run(() => new[] { b.QueueApply(id, v.Session, body, 1100).Status, b.QueueApply(id.ToUpperInvariant(), v.Session, body, 1101).Status }).GetAwaiter().GetResult();
            check(replies.SequenceEqual(new[] { 202, 200 }) && h.StartCalls == 0, "dispatch off framework or duplicate not recognized");
            check(b.QueueApply(Guid.NewGuid().ToString(), v.Session, body, 1102).Status == 409 && b.QueueQuery(Guid.NewGuid().ToString(), v.Session, 1102).Status == 409, "concurrent accepted");
            var result = Finish(b, h);
            check(result.State == "complete" && h.StartCalls == 1 && h.Native.SubmitCalls == 1 && h.Native.EditCalls == 1, "not one refresh");
            check(h.Native.SubmittedDescription == "browser draft" && local.Items.Count == 1, "local draft consumed");
            check(b.QueueApply(id, v.Session, body, h.Tick).Status == 200 && h.StartCalls == 1, "completed replay ran again");
        });
        test("web batch skips an unsupported rule, applies other edits and reports partial result", () => {
            var (b, h, v) = Ready(); h.Native.RuleEditAllowed = false;
            b.QueueApply(Guid.NewGuid().ToString(), v.Session, Body(v.Editor!.Revision,
                new { field = "unrestricted", value = 1 }, new { field = "description", value = "반영할 문구" },
                new { field = "job", slot = 7, jobs = new[] { 19 } }), 1100);
            var result = Finish(b, h);
            check(result.State == "partial" && result.Submitted && h.Native.SubmitCalls == 1 && h.Native.EditCalls == 1, "partial batch aborted");
            check(result.Results.Count(r => r.Applied == true) == 2 && result.Results[0].Applied == false && h.Native.SubmittedDescription == "반영할 문구", "per-item result wrong");
        });
        test("web plain refresh has one submit and is bound to the displayed source", () => {
            foreach (var changed in new[] { false, true }) {
                var (b, h, v) = Ready(); b.QueueApply(Guid.NewGuid().ToString(), v.Session, Body(v.Editor!.Revision), 1100);
                if (changed) h.Native.Target = h.Native.Target with { Description = "다른 모집글" };
                var result = Finish(b, h);
                check(changed ? result.State == "failed" && h.Native.EditCalls == 0 && h.Native.SubmitCalls == 0 :
                    result.State == "complete" && h.Native.SubmitCalls == 1 && result.Results.Length == 0, "plain refresh binding failed");
            }
        });
        test("web batch rechecks source after its fresh query before editing", () => {
            var (b, h, v) = Ready(); b.QueueApply(Guid.NewGuid().ToString(), v.Session, Body(v.Editor!.Revision, new { field = "objective", value = 3 }), 1100);
            Step(b, h, 1500); h.Native.Target = h.Native.Target with { Description = "바뀐 글" };
            check(Finish(b, h, 2000).State == "failed" && h.Native.EditCalls == 0, "stale batch edited");
        });
        test("web 48-person job batch keeps every native slot and still submits just once", () => {
            var (b, h, _) = Ready(); h.Native.Target = WebEditFakeHost.Target(48); b.Update(h, 1001);
            var v = ListingWebTests.View(b); var edits = v.Editor!.Slots.Select(s => (object)new { field = "job", slot = s.NativeIndex, jobs = new[] { 25 } }).ToArray();
            check(b.QueueApply(Guid.NewGuid().ToString(), v.Session, Body(v.Editor.Revision, edits), 1100).Status == 202, "large batch rejected");
            var result = Finish(b, h);
            check(h.Native.SubmitCalls == 1 && result.Results.Length == 47 && result.Results.All(r => r.Applied == true) &&
                h.Native.SubmittedTarget!.Slots.Slots[^1].NativeIndex == 47, "large batch truncated or repeated");
        });
        test("web simple refresh remains available when only job editing is unsupported", () => {
            var (b, h, _) = Ready(); h.Native.Target = h.Native.Target with { JobEditingAllowed = false }; b.Update(h, 1001);
            var v = ListingWebTests.View(b); check(v.CanRefresh && v.Editor!.Supported.Length == 0, "plain refresh disabled by editor support");
        });
        test("web unavailable preflight, stale publication and expired request never start", () => {
            foreach (var mode in new[] { "blocked", "expired", "publication" }) {
                var (b, h, v) = Ready(); var body = Body(v.Editor!.Revision);
                check(b.QueueApply(Guid.NewGuid().ToString(), v.Session, body, 6001).Status == 409, "stale publication accepted");
                b.QueueApply(Guid.NewGuid().ToString(), v.Session, body, 1100);
                if (mode == "blocked") h.Blocked = "게임 수정창 열림";
                if (mode == "publication") b.PublicationFailed();
                b.Update(h, mode == "expired" ? 6101 : 1500);
                check(h.StartCalls == 0 && ListingWebTests.View(b).Operation?.State == "failed", "blocked write executed");
            }
        });
        test("web character switch and shutdown discard queued bodies and credentials", () => {
            foreach (var mode in new[] { "logout", "switch", "stop" }) {
                var (b, h, v) = Ready(); var id = Guid.NewGuid().ToString(); b.QueueApply(id, v.Session, Body(v.Editor!.Revision, new { field = "private", value = true, password = "9876" }), 1100);
                if (mode == "stop") b.Stop(); else h.Native.SelfId = mode == "logout" ? 0ul : 20ul;
                b.Update(h, 1500); var view = ListingWebTests.View(b);
                check(h.StartCalls == 0 && view.Editor == null && view.Operation == null && view.Listing == null, "old character write leaked");
                check(b.QueueApply(Guid.NewGuid().ToString(), v.Session, Body(v.Editor.Revision), 1600).Status is 409 or 503, "old session accepted");
            }
        });
        test("web PIN never appears in snapshots, progress, HTTP replies or completed results", () => {
            var (b, h, v) = Ready(); var id = Guid.NewGuid().ToString(); const string pin = "9876";
            var reply = b.QueueApply(id, v.Session, Body(v.Editor!.Revision, new { field = "private", value = true, password = pin }), 1100);
            check(!JsonSerializer.Serialize(reply).Contains(pin), "queue response contains PIN");
            for (var tick = 1500; tick < 20000; tick += 500) {
                Step(b, h, tick);
                check(!Encoding.UTF8.GetString(b.ReadState()).Contains(pin) && !JsonSerializer.Serialize(b.ReadOperation(id)).Contains(pin), "PIN leaked");
            }
            check(h.Native.PrivateExpected != null && ListingWebTests.View(b).Operation!.Results.Single().Applied == true, "private not applied");
        });
        test("web terminal unconfirmed status remains honest even when fields match", () => {
            var (b, h, v) = Ready(); h.Native.Target = h.Native.Target with { RemainingSeconds = 3598 }; b.Update(h, 1001); v = ListingWebTests.View(b);
            b.QueueApply(Guid.NewGuid().ToString(), v.Session, Body(v.Editor!.Revision, new { field = "description", value = "new" }), 1100);
            var result = Finish(b, h); check(result.State == "unconfirmed" && result.Results.Single().Applied == true, "timer confirmation invented");
        });
        test("web history eviction does not make an old write executable again", () => {
            var (b, h, _) = Ready(); string first = ""; long tick = 1000;
            for (var i = 0; i < 35; i++) {
                b.Update(h, tick); var v = ListingWebTests.View(b); var id = Guid.NewGuid().ToString(); if (i == 0) first = id;
                check(b.QueueApply(id, v.Session, Body(v.Editor!.Revision), tick + 1).Status == 202, "queue failed");
                h.Blocked = "preflight"; b.Update(h, tick + 100); h.Blocked = null; tick += 1000;
            }
            b.Update(h, tick); var last = ListingWebTests.View(b);
            check(b.ReadOperation(first).Status == 409 && b.QueueApply(first, last.Session, Body(last.Editor!.Revision), tick + 1).Status == 409 && h.StartCalls == 0, "expired write replayed");
        });
        test("web refresh guard permits long batches, stops stalls, and never cancels newer local work", () => {
            var guard = new ListingWebRefreshGuard(); guard.Started(10, 5, 1000);
            for (long tick = 1500; tick <= 180000; tick += 500) check(!guard.BeforeRead(10, 5, true, true, false, tick), "normal long batch cancelled");
            check(guard.BeforeRead(10, 5, true, true, false, 200000), "stalled batch resumed");
            guard.Started(10, 6, 1000); check(guard.BeforeRead(10, 6, true, true, true, 6001), "expired queued refresh executed");
            check(!guard.BeforeRead(20, 7, true, false, true, 10000), "new local run cancelled");
            guard.Started(10, 8, 1000); check(guard.BeforeRead(20, 8, true, true, false, 1500), "changed character accepted");
        });
        test("web apply authenticates and handles a fragmented bounded JSON body with one execution", () => Http(check).GetAwaiter().GetResult());
    }

    private static async Task Http(Action<bool, string> check)
    {
        var (b, h, v) = Ready(Environment.TickCount64); h.CheckThread = false;
        using var server = new ListingWebServer(b); var uri = new Uri(server.BaseUrl); var key = new Uri(server.ConnectionUrl).Fragment[5..];
        var body = Body(v.Editor!.Revision, new { field = "description", value = "웹 소개말" }); var id = Guid.NewGuid().ToString();
        using var http = new HttpClient { BaseAddress = uri };
        using (var denied = await http.PostAsync("/api/apply", new StringContent(Encoding.UTF8.GetString(body), Encoding.UTF8, "application/json")))
            check(denied.StatusCode == HttpStatusCode.Unauthorized, "anonymous mutation accepted");
        http.DefaultRequestHeaders.Authorization = new("Bearer", key);
        foreach (var raw in new[] { "text/plain", "application/x-www-form-urlencoded" }) {
            using var response = await http.PostAsync("/api/apply", new StringContent("{}", Encoding.UTF8, raw));
            check(response.StatusCode == HttpStatusCode.BadRequest, "simple cross-origin body accepted");
        }
        using (var socket = new TcpClient()) {
            await socket.ConnectAsync(uri.Host, uri.Port); var stream = socket.GetStream();
            var headers = $"POST /api/apply HTTP/1.1\r\nHost: {uri.Authority}\r\nAuthorization: Bearer {key}\r\nOrigin: {server.BaseUrl}\r\nX-PVKR-Session: {v.Session}\r\nX-PVKR-Request-Id: {id}\r\nContent-Type: application/json\r\nContent-Length: {body.Length}\r\n\r\n";
            await stream.WriteAsync(Encoding.ASCII.GetBytes(headers).Concat(body.Take(3)).ToArray());
            await stream.WriteAsync(body.AsMemory(3, 7)); await stream.WriteAsync(body.AsMemory(10));
            using var reader = new StreamReader(stream); var response = await reader.ReadToEndAsync().WaitAsync(TimeSpan.FromSeconds(5));
            check(response.StartsWith("HTTP/1.1 202"), "fragmented body rejected");
        }
        check(h.StartCalls == 0 && ListingWebTests.View(b).Operation?.State == "queued", "HTTP executed native command");
        using (var result = await http.GetAsync("/api/operations/" + id)) check(result.StatusCode == HttpStatusCode.OK, "write result missing");
        using var repeat = new HttpRequestMessage(HttpMethod.Post, "/api/apply") { Content = new StringContent(Encoding.UTF8.GetString(body), Encoding.UTF8, "application/json") };
        repeat.Headers.Add("X-PVKR-Session", v.Session); repeat.Headers.Add("X-PVKR-Request-Id", id);
        using (var response = await http.SendAsync(repeat)) check(response.StatusCode == HttpStatusCode.OK, "duplicate write not recognized");
        var prefix = "POST /api/apply HTTP/1.1\r\nContent-Type: application/json\r\n";
        foreach (var invalid in new[] { "Content-Length: 0", "Content-Length: 65537", "Content-Length: +2", "Content-Length: 2\r\nTransfer-Encoding: chunked", "Content-Length: 2\r\nContent-Length: 2", "Expect: 100-continue" })
            check(!ListingWebServer.TryParse(Encoding.ASCII.GetBytes(prefix + invalid), out _), "ambiguous framing accepted");
        server.Dispose(); await server.Completion.WaitAsync(TimeSpan.FromSeconds(3));
    }
}

sealed class WebEditFakeHost : IListingWebEditHost
{
    private readonly int thread = Environment.CurrentManagedThreadId;
    public bool CheckThread = true, Live = true;
    public BatchFakeHost Native { get; } = new() { Target = Target() };
    public ListingRefresh Flow { get; }
    public long Tick;
    public string? Blocked;
    public int StartCalls;
    internal WebRefreshProgress? ReportOverride;
    private long ownedRevision = -1;
    public WebEditFakeHost() { Flow = new(Native); }
    public ulong SelfId { get { Guard(); return Native.SelfId; } }
    public string? QueryBlockReason { get { Guard(); return Blocked ?? (Flow.Busy || Native.QueryBusy ? "처리 중" : null); } }
    public string? RefreshBlockReason => QueryBlockReason ?? (Flow.CoolingDown(Tick) ? "잠시 후 갱신" : null);
    public bool QueryBusy => Native.QueryBusy;
    public bool QuerySucceeded => Native.QuerySucceeded;
    public long DetailRevision => Native.DetailRevision;
    public ListingSnapshot Listing => ListingWebTests.Sample() with {
        ViewerContentId = Native.Target.SelfId, Description = Native.Target.Description, Slots = Native.Target.Slots,
        IsLive = Live && !Native.EditorVisible, ReadAt = DateTime.Now, Objective = ListingObjectives.Read(Native.Target.Criteria),
        BeginnerFriendly = ListingBeginners.Read(Native.Target.Criteria), UniqueJobs = ListingUniqueJobs.Read(Native.Target.ExtraCriteria),
        Private = ListingPrivate.Read(Native.Target.ExtraCriteria), Completion = ListingCompletions.Read(Native.Target.Criteria),
        ItemLevel = ListingItemLevels.Read(Native.Target.ExtraCriteria), DutyFlags = byte.Parse(Native.Target.Criteria.Split('/')[5]),
        LootRule = byte.Parse(Native.Target.Criteria.Split('/')[6])
    };
    public RefreshTarget? CaptureEditTarget() { Guard(); return Flow.Busy || QueryBusy || !Live ? null : Native.Target; }
    public bool RequestQuery(out string error) { Guard(); Native.QueryOwn(); error = ""; return true; }
    public bool StartRefresh(RefreshTarget source, ListingChange[] changes, out string error)
    {
        Guard(); StartCalls++; error = RefreshBlockReason ?? ""; if (error.Length > 0) return false;
        if (changes.Length == 0) Flow.Request(Tick, source: source); else Flow.RequestBatch(Tick, changes);
        ownedRevision = Flow.RequestRevision; return true;
    }
    public WebRefreshProgress? ReadRefresh() { Guard(); return ReportOverride ?? (ownedRevision == Flow.RequestRevision ? WebRefreshProgress.From(Flow) : null); }
    public void Advance(long now)
    {
        Tick = now;
        if (Native.QueryBusy) {
            if (Flow.Stage == RefreshStage.Verifying && Native.SubmittedTarget != null) Native.Target = Native.SubmittedTarget with { RemainingSeconds = 3598 };
            Native.FinishQuery();
        }
        Flow.Update(now);
    }
    private void Guard() { if (CheckThread && Environment.CurrentManagedThreadId != thread) throw new Exception("off framework thread"); }
    internal static RefreshTarget Target(int count = 8, bool friendly = false)
    {
        var sample = ListingWebTests.Sample(count, friendly); var slots = sample.Slots;
        var flags = new ulong[slots.NativeSlotCount]; foreach (var slot in slots.Slots) flags[slot.NativeIndex] = slot.SlotFlags;
        return new(10, 123, 321, "기존 소개말", "4/321/1/0/1/0/0/1/" + Convert.ToHexString(MemoryMarshal.AsBytes(flags.AsSpan())), 1800, slots.TotalSlots) {
            Slots = slots, ExtraCriteria = $"0/{(friendly ? 16 : 0)}/{slots.IsAlliance}/{slots.NumberOfParties}", JobEditingAllowed = true
        };
    }
}
