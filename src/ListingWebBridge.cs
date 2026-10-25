using System.Text.Json;

namespace PartyViewKR;

internal interface IListingWebHost
{
    ulong SelfId { get; }
    string? QueryBlockReason { get; }
    bool QueryBusy { get; }
    bool QuerySucceeded { get; }
    long DetailRevision { get; }
    ListingSnapshot Listing { get; }
    bool RequestQuery(out string error);
}

internal sealed record WebReply(int Status, object Body);

// HTTP only reads a serialized snapshot or queues a bounded request. Update is
// called exclusively by Framework.Update; every host/game access stays there.
internal sealed partial class ListingWebBridge
{
    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly object gate = new();
    private readonly Dictionary<string, WebQuery> history = new(StringComparer.Ordinal);
    private readonly Queue<string> historyOrder = new();
    private readonly string version;
    private WebView view;
    private byte[] json;
    private bool stopped;
    private ulong self;
    private long publishedTick, requestedTick, startedTick, expectedRevision, nextQueryTick;

    internal ListingWebBridge(string version)
    {
        this.version = version;
        view = Empty("게임 상태를 확인하는 중입니다.");
        json = JsonSerializer.SerializeToUtf8Bytes(view, JsonOptions);
    }

    private WebView Empty(string reason) => new(2, version, Guid.NewGuid().ToString("N"), false, false,
        reason, reason, DateTimeOffset.UtcNow, null, null);
    private bool Active => view.Query?.State is "queued" or "running";
    internal byte[] ReadState() { lock (gate) return json; }
    internal WebView ReadView() { lock (gate) return view; }
    internal WebReply ReadQuery(string id)
    {
        if (Guid.TryParseExact(id, "D", out var parsed)) id = parsed.ToString("D");
        lock (gate) return history.TryGetValue(id, out var query) ? new(200, query) : Error(404, "해당 조회 요청을 찾지 못했습니다.");
    }

    internal WebReply QueueQuery(string id, string session, long now, long? recoveryRevision = null)
    {
        lock (gate)
        {
            if (stopped) return Error(503, "브라우저 조회가 꺼져 있습니다.");
            if (!Guid.TryParseExact(id, "D", out var parsed)) return Error(400, "조회 요청 번호가 올바르지 않습니다.");
            id = parsed.ToString("D");
            if (session != view.Session) return Error(409, "게임 접속 상태가 바뀌었습니다. 화면을 다시 확인해 주세요.");
            if (history.TryGetValue(id, out var existing)) return new(200, existing);
            if (recoveryRevision.HasValue && recoveryRevision != this.recoveryRevision) return Error(409, "수정창 닫기 요청으로 이전 작업을 중단했습니다.");
            if (now - publishedTick > 5000) return Error(409, "게임 응답을 기다리는 중입니다.");
            if (!view.CanQuery || Active || WriteActive || RecoveryActive) return Error(409, view.Reason.Length > 0 ? view.Reason : "다른 작업을 처리하는 중입니다.");
            if (now < nextQueryTick) return Error(429, "잠시 후 다시 조회해 주세요.");
            requestedTick = now; nextQueryTick = now + 2000;
            SetQuery(new(id, "queued", "게임에 조회를 요청하는 중입니다."));
            view = view with { CanQuery = false, CanRefresh = false, Reason = "조회 요청을 처리하는 중입니다.", RefreshReason = "조회 요청을 처리하는 중입니다." };
            Serialize();
            return new(202, view.Query!);
        }
    }

    internal void Update(IListingWebHost host, long now)
    {
        lock (gate)
        {
            if (stopped) return;
            var currentSelf = host.SelfId;
            if (self != currentSelf)
            {
                self = currentSelf;
                history.Clear(); historyOrder.Clear(); nextQueryTick = 0;
                ClearEdits();
                ClearRecovery();
                view = Empty("게임 접속 상태가 바뀌었습니다.");
            }
            ProcessRecovery(host as IListingRecoveryHost, now);
            var listing = self != 0 && host.Listing.ViewerContentId == self ? host.Listing : ListingSnapshot.Empty;
            // Do not execute an old request after the game has been paused or
            // its framework thread has stalled. Character changes clear it too.
            if (view.Query is { State: "queued" } pending)
            {
                if (now - requestedTick > 5000) SetQuery(pending with { State = "failed", Message = "조회 요청 대기 시간이 지났습니다. 다시 눌러 주세요." });
                else if (host.QueryBlockReason is { } blocked) SetQuery(pending with { State = "failed", Message = blocked });
                else
                {
                    expectedRevision = host.DetailRevision; startedTick = now;
                    SetQuery(pending with { State = "running", Message = "내 모집글을 확인하는 중입니다." });
                    try
                    {
                        if (!host.RequestQuery(out var error)) SetQuery(pending with { State = "failed", Message = error });
                    }
                    catch (Exception) { SetQuery(pending with { State = "failed", Message = "게임에 조회를 요청하지 못했습니다." }); }
                }
            }
            else if (view.Query is { State: "running" } running)
            {
                if (!host.QueryBusy && host.QuerySucceeded && host.DetailRevision > expectedRevision &&
                    listing.Owner == ListingOwner.Self && listing.IsLive)
                    SetQuery(running with { State = "complete", Message = "내 모집글을 조회했습니다." });
                else if (!host.QueryBusy && !host.QuerySucceeded || now - startedTick > 15000)
                    SetQuery(running with { State = "failed", Message = "내 모집글 조회를 확인하지 못했습니다. 게임의 모집 상태를 확인해 주세요." });
            }
            UpdateOperation(host as IListingWebEditHost, now);
            var reason = RecoveryActive ? "수정창을 닫는 중입니다." : host.QueryBlockReason ?? (WriteActive ? "갱신 요청을 처리하는 중입니다." : Active ? "조회 요청을 처리하는 중입니다." : "");
            var loggedIn = self != 0;
            if (!loggedIn) reason = "게임에 로그인해 주세요.";
            view = view with {
                LoggedIn = loggedIn, CanQuery = loggedIn && reason.Length == 0,
                Reason = reason, Status = loggedIn ? listing.Status : "게임 로그인 대기",
                UpdatedAt = DateTimeOffset.UtcNow, Listing = ListingWebView.From(listing, loggedIn, host.QueryBusy)
            };
            PublishEditor(host as IListingWebEditHost, listing);
            publishedTick = now; Serialize();
        }
    }

    internal void PublicationFailed()
    {
        lock (gate)
        {
            if (stopped) return;
            if (view.Query is { } query && Active) SetQuery(query with { State = "failed", Message = "게임 상태를 읽지 못했습니다." });
            if (view.Operation is { State: "queued" } pending) {
                pendingWrite = null;
                SetOperation(pending with { State = "failed", Message = "게임 상태를 읽지 못해 요청을 취소했습니다." });
            }
            source = null;
            view = view with { CanQuery = false, CanRefresh = false, Editor = null, RefreshReason = "게임 상태를 읽지 못했습니다.", Reason = "게임 상태를 읽지 못했습니다.", Listing = null };
            Serialize();
        }
    }
    internal void Stop()
    {
        lock (gate)
        {
            stopped = true; self = 0; history.Clear(); historyOrder.Clear();
            ClearEdits();
            ClearRecovery();
            view = Empty("브라우저 조회가 꺼져 있습니다."); Serialize();
        }
    }
    private void SetQuery(WebQuery query)
    {
        if (!history.ContainsKey(query.Id)) historyOrder.Enqueue(query.Id);
        history[query.Id] = query;
        while (historyOrder.Count > 32) history.Remove(historyOrder.Dequeue());
        view = view with { Query = query };
    }
    private void Serialize() => json = JsonSerializer.SerializeToUtf8Bytes(view, JsonOptions);
    internal static WebReply Error(int status, string message) => new(status, new { error = message });
}
