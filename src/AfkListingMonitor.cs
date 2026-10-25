namespace PartyViewKR;

internal interface IAfkListingHost
{
    bool LoggedIn { get; }
    ulong SelfId { get; }
    bool TryReadRoster(out PartyRoster? roster);
    string? QueryBlockReason(bool ownQueryInProgress);
    bool QueryBusy { get; }
    bool QuerySucceeded { get; }
    long RequestRevision { get; }
    long DetailRevision { get; }
    bool OwnListingMatches(PartyRoster roster);
    bool RequestQuery(out string error);
    void CancelQuery(long revision);
}

// Framework-thread controller. This schedules detail reads only; there is no
// editing/submit/refresh capability in its host contract.
internal sealed class AfkListingMonitor
{
    internal const long SupplementInterval = 60000;
    private const long QueryTimeout = 15000;
    private const long MinimumQueryInterval = 2000;
    private readonly ListingWebRequestGuard ownedQuery = new();
    private PartyRoster? observed;
    private ulong self;
    private long generation, requestedGeneration, requestRevision = -1, detailBefore;
    private long started, nextAttempt, nextSupplement;
    private bool readable;
    private int failures, queries, successes, changes;
    internal bool Enabled { get; private set; }
    internal bool Pending { get; private set; }
    internal bool InFlight => requestRevision >= 0;
    internal string Status { get; private set; } = "AFK 모드가 꺼져 있습니다.";
    internal string Diagnostic => $"afkEnabled={Enabled}; afkPending={Pending}; afkInFlight={InFlight}; " +
        $"afkRosterReadable={readable}; afkMemberCount={observed?.Count ?? 0}; afkChanges={changes}; " +
        $"afkQueryCalls={queries}; afkSuccesses={successes}; afkFailures={failures}; " +
        $"afkGeneration={generation}; afkRequestedGeneration={requestedGeneration}; afkSupplementMs={SupplementInterval}";

    internal bool SetEnabled(bool enabled, IAfkListingHost host, long now)
    {
        if (enabled == Enabled) return true;
        if (enabled && (!host.LoggedIn || host.SelfId == 0))
        { Status = "캐릭터에 접속한 뒤 AFK 모드를 켜 주세요."; return false; }
        CancelOwned(host);
        Enabled = enabled; Pending = enabled; self = enabled ? host.SelfId : 0;
        observed = null; readable = false; generation = requestedGeneration = 0;
        failures = queries = successes = changes = 0;
        nextAttempt = now; nextSupplement = now + SupplementInterval;
        Status = enabled ? "명단을 읽고 내 모집글을 조회합니다." : "AFK 모드가 꺼져 있습니다.";
        return true;
    }

    // Run before ListingReader.Read: RequestOwn defers native dispatch until then.
    internal void BeforeRead(IAfkListingHost host, long now)
    {
        if (!CheckSession(host)) return;
        if (InFlight && ownedQuery.BeforeRead(host.SelfId, host.RequestRevision, host.QueryBusy,
            Enabled, host.QueryBlockReason(true) != null, now))
        {
            CancelOwned(host);
            Retry(now, "조회 가능한 상태가 되면 다시 시도합니다.");
        }
    }

    internal void Update(IAfkListingHost host, long now)
    {
        if (!CheckSession(host)) return;
        var hasRoster = host.TryReadRoster(out var current) && current != null && current.SelfId == self;
        if (hasRoster)
        {
            if (!readable || observed != current)
            {
                generation++; Pending = true;
                if (observed != null && observed != current) changes++;
            }
            observed = current;
        }
        else if (readable) { generation++; Pending = true; }
        readable = hasRoster;
        if (now >= nextSupplement) Pending = true;

        if (InFlight)
        {
            if (host.RequestRevision != requestRevision)
            {
                // Another request owns the reader now. Never cancel it or accept
                // its result as completion of this AFK request.
                CancelOwned(host);
                Retry(now, "다른 조회가 끝나면 다시 확인합니다.");
            }
            else if (!host.QueryBusy && !host.QuerySucceeded)
            {
                CancelOwned(host);
                Retry(now, "모집글 조회에 실패했습니다. 자동으로 다시 시도합니다.");
            }
            else if (!host.QueryBusy && host.QuerySucceeded && host.DetailRevision > detailBefore &&
                readable && host.OwnListingMatches(observed!))
            {
                ownedQuery.Clear(); requestRevision = -1;
                successes++; failures = 0; nextSupplement = now + SupplementInterval;
                nextAttempt = now + MinimumQueryInterval;
                // A -> B -> A while the request was running is still a change.
                Pending = generation != requestedGeneration;
                Status = Pending ? "조회 중 명단이 바뀌어 다시 확인합니다." : $"명단 감시 중 · {observed!.Count}명 · 60초마다 보완 조회";
            }
            else if (now - started >= QueryTimeout)
            {
                CancelOwned(host);
                Retry(now, "새 모집정보를 확인하지 못했습니다. 자동으로 다시 시도합니다.");
            }
            else Status = "새 본인 모집정보를 기다리는 중입니다.";
            return;
        }

        if (!readable) { Status = "명단을 읽을 수 없어 마지막 정상 명단을 유지합니다."; return; }
        if (!Pending) { Status = $"명단 감시 중 · {observed!.Count}명 · 60초마다 보완 조회"; return; }
        if (now < nextAttempt) return;
        if (host.QueryBlockReason(false) is { } reason)
        { Status = "자동 조회 대기 · " + reason; return; }

        detailBefore = host.DetailRevision; requestedGeneration = generation; started = now;
        nextAttempt = now + MinimumQueryInterval; queries++;
        var before = host.RequestRevision;
        try
        {
            if (host.RequestQuery(out var error))
            {
                if (host.RequestRevision == before)
                { Retry(now, "조회 요청을 시작하지 못했습니다."); return; }
                requestRevision = host.RequestRevision;
                ownedQuery.Started(self, requestRevision, now);
                Status = "내 모집글을 자동 조회하는 중입니다.";
            }
            else Retry(now, error.Length > 0 ? error : "조회 요청을 시작하지 못했습니다.");
        }
        catch
        {
            // The request may already have been accepted before an exception.
            if (host.RequestRevision != before)
            {
                requestRevision = host.RequestRevision;
                ownedQuery.Started(self, requestRevision, now);
                Status = "조회 요청 결과를 확인하는 중입니다.";
            }
            else Retry(now, "조회 요청 중 오류가 발생했습니다.");
        }
    }

    private bool CheckSession(IAfkListingHost host)
    {
        if (!Enabled) return false;
        // A temporarily unavailable PlayerState is not a logout. The existing
        // query guards handle it, without a second territory-state machine here.
        if (!host.LoggedIn || host.SelfId != 0 && host.SelfId != self)
        {
            SetEnabled(false, host, 0);
            Status = "접속 캐릭터가 바뀌어 AFK 모드를 껐습니다.";
            return false;
        }
        return true;
    }

    private void CancelOwned(IAfkListingHost host)
    {
        if (InFlight) host.CancelQuery(requestRevision);
        requestRevision = -1; ownedQuery.Clear();
    }

    private void Retry(long now, string message)
    {
        Pending = true;
        failures = Math.Min(failures + 1, 5);
        var wait = Math.Min(60000, 5000L << (failures - 1));
        nextAttempt = now + wait;
        Status = $"{message} ({wait / 1000}초 후 재시도)";
    }
}
