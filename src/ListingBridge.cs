using System.Text.Json;

namespace PartyViewKR;

internal interface IListingQueryHost
{
    ulong SelfId { get; }
    string? QueryBlockReason { get; }
    bool QueryBusy { get; }
    bool QuerySucceeded { get; }
    long QueryTimeoutMilliseconds => 15000;
    long DetailRevision { get; }
    ListingSnapshot Listing { get; }
    bool RequestQuery(out string error);
}

internal sealed record RemoteReply(int Status, object Body);

// Discord callbacks only read managed snapshots or queue bounded requests. Update is
// called exclusively by Framework.Update; every host/game access stays there.
internal sealed partial class ListingBridge
{
    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly object gate = new();
    private readonly Dictionary<string, RemoteQuery> history = new(StringComparer.Ordinal);
    private readonly Queue<string> historyOrder = new();
    private readonly string version;
    private RemoteView view;
    private bool stopped;
    private ulong self;
    private long publishedTick, requestedTick, startedTick, expectedRevision, nextQueryTick;

    internal ListingBridge(string version)
    {
        this.version = version;
        view = Empty("게임 상태를 확인하는 중입니다.");
    }

    private RemoteView Empty(string reason) => new(2, version, Guid.NewGuid().ToString("N"), false, false,
        reason, reason, DateTimeOffset.UtcNow, null, null);
    private bool Active => view.Query?.State is "queued" or "running";
    internal RemoteView ReadView() { lock (gate) return view; }
    internal RemoteReply ReadQuery(string id)
    {
        if (Guid.TryParseExact(id, "D", out var parsed)) id = parsed.ToString("D");
        lock (gate) return history.TryGetValue(id, out var query) ? new(200, query) : Error(404, "해당 조회 요청을 찾지 못했습니다.");
    }

    internal RemoteReply QueueQuery(string id, string session, long now, long? recoveryRevision = null)
    {
        lock (gate)
        {
            if (stopped) return Error(503, "디스코드 연결이 꺼져 있습니다.");
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
            return new(202, view.Query!);
        }
    }

    internal void Update(IListingQueryHost host, long now)
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
                    SetQuery(pending with { State = "running", Message = "현재 파티의 모집글을 확인하는 중입니다." });
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
                    ListingPartyScope.ReadableOwner(listing.Owner) && listing.IsLive)
                    SetQuery(running with { State = "complete", Message = listing.Owner == ListingOwner.Self ? "내 모집글을 조회했습니다." : "참가한 파티의 모집글을 조회했습니다." });
                else if (!host.QueryBusy && !host.QuerySucceeded || now - startedTick > host.QueryTimeoutMilliseconds)
                    SetQuery(running with { State = "failed", Message = "현재 파티의 모집글을 확인하지 못했습니다. 모집이 종료되었거나 파티가 변경되었을 수 있습니다." });
            }
            UpdateOperation(host as IListingEditHost, now);
            var reason = RecoveryActive ? "수정창을 닫는 중입니다." : host.QueryBlockReason ?? (WriteActive ? "갱신 요청을 처리하는 중입니다." : Active ? "조회 요청을 처리하는 중입니다." : "");
            var loggedIn = self != 0;
            if (!loggedIn) reason = "게임에 로그인해 주세요.";
            view = view with {
                LoggedIn = loggedIn, CanQuery = loggedIn && reason.Length == 0,
                Reason = reason, Status = loggedIn ? listing.Status : "게임 로그인 대기",
                UpdatedAt = DateTimeOffset.UtcNow, Listing = ListingViewProjection.From(listing, loggedIn, host.QueryBusy)
            };
            PublishEditor(host as IListingEditHost, listing);
            publishedTick = now;
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
        }
    }
    internal void Stop()
    {
        lock (gate)
        {
            stopped = true; self = 0; history.Clear(); historyOrder.Clear();
            ClearEdits();
            ClearRecovery();
            view = Empty("디스코드 연결이 꺼져 있습니다.");
        }
    }
    private void SetQuery(RemoteQuery query)
    {
        if (!history.ContainsKey(query.Id)) historyOrder.Enqueue(query.Id);
        history[query.Id] = query;
        while (historyOrder.Count > 32) history.Remove(historyOrder.Dequeue());
        view = view with { Query = query };
    }
    internal static RemoteReply Error(int status, string message) => new(status, new { error = message });
}
