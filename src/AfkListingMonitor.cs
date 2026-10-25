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
    int RegisteredCapacity { get; }
    bool RequestQuery(out string error);
    void CancelQuery(long revision);
}

// Framework-thread controller. This schedules detail reads only; there is no
// editing/submit/refresh capability in its host contract.
internal sealed class AfkListingMonitor
{
    private const long QueryTimeout = 15000;
    private const long MinimumQueryInterval = 2000;
    private readonly ListingWebRequestGuard ownedQuery = new();
    private PartyRoster? observed;
    private ulong self;
    private long generation, requestedGeneration, requestRevision = -1, detailBefore;
    private long started, nextAttempt, nextSupplement;
    private bool readable;
    private bool failed;
    private long confirmedGeneration, failedGeneration;
    internal AfkSettings Settings { get; private set; } = new();
    private int failures, queries, successes, changes;
    internal bool Enabled { get; private set; }
    internal ulong MonitoredSelfId => Enabled ? self : 0;
    internal bool Pending { get; private set; }
    internal bool InFlight => requestRevision >= 0;
    internal int RecruitmentCapacity { get; private set; }
    internal long CompletionRevision { get; private set; }
    internal bool RecruitmentComplete => Enabled && readable && complete;
    private bool complete;
    private long capacityRequestRevision, capacityDetailRevision;
    internal string Status { get; private set; } = "AFK 모드가 꺼져 있습니다.";
    internal string Diagnostic => $"afkEnabled={Enabled}; afkPending={Pending}; afkInFlight={InFlight}; " +
        $"afkRosterReadable={readable}; afkMemberCount={observed?.Count ?? 0}; afkChanges={changes}; " +
        $"afkQueryCalls={queries}; afkSuccesses={successes}; afkFailures={failures}; " +
        $"afkGeneration={generation}; afkRequestedGeneration={requestedGeneration}; " +
        $"afkPeriodicEnabled={Settings.PeriodicEnabled}; afkPeriodMinutes={Settings.PeriodMinutes}; " +
        $"afkRetryEnabled={Settings.RetryEnabled}; afkRetrySeconds={Settings.RetrySeconds}; afkFailed={failed}; " +
        $"afkRecruitmentCapacity={RecruitmentCapacity}; afkRecruitmentComplete={RecruitmentComplete}; afkCompletionRevision={CompletionRevision}";

    internal void Configure(AfkSettings settings, long now)
    {
        settings = settings.Normalize();
        if (settings.PeriodicEnabled != Settings.PeriodicEnabled || settings.PeriodMinutes != Settings.PeriodMinutes)
            nextSupplement = now + settings.PeriodMilliseconds;
        if (failed && (settings.RetryEnabled != Settings.RetryEnabled || settings.RetrySeconds != Settings.RetrySeconds))
        {
            nextAttempt = now + settings.RetryMilliseconds;
            Status = settings.RetryEnabled ? $"조회 실패 · {settings.RetrySeconds}초 후 재시도합니다." : "조회 실패 · 자동 재시도가 꺼져 있습니다.";
        }
        Settings = settings;
    }

    private string Watching => $"명단 감시 중 · {observed!.Count}명" +
        (Settings.PeriodicEnabled ? $" · {Settings.PeriodMinutes}분마다 추가 조회" : " · 주기 조회 꺼짐");

    internal bool SetEnabled(bool enabled, IAfkListingHost host, long now)
    {
        if (enabled == Enabled) return true;
        if (enabled && (!host.LoggedIn || host.SelfId == 0))
        { Status = "캐릭터에 접속한 뒤 AFK 모드를 켜 주세요."; return false; }
        CancelOwned(host);
        Enabled = enabled; Pending = enabled; self = enabled ? host.SelfId : 0;
        observed = null; readable = false; generation = requestedGeneration = 0;
        confirmedGeneration = failedGeneration = 0; failed = false;
        RecruitmentCapacity = 0; complete = false;
        capacityRequestRevision = host.RequestRevision; capacityDetailRevision = host.DetailRevision;
        failures = queries = successes = changes = 0;
        nextAttempt = now; nextSupplement = now + Settings.PeriodMilliseconds;
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
        var periodicDue = Settings.PeriodicEnabled && now >= nextSupplement;
        if (failed && generation != failedGeneration) { failed = false; nextAttempt = now; }
        Pending = InFlight || generation != confirmedGeneration || failed || periodicDue;

        // A manual/Discord own-query may confirm a newly created recruitment
        // while the previous full state suppresses automatic queries. Adopt only
        // a NEW successful own response whose entire roster still matches.
        if (hasRoster && !host.QueryBusy && host.QuerySucceeded && host.RequestRevision != capacityRequestRevision &&
            host.DetailRevision > capacityDetailRevision && host.OwnListingMatches(current!)) RememberCapacity(host);

        // A full recruitment disappears from Party Finder automatically. Detect
        // completion from the current CID roster and the last verified capacity,
        // BEFORE waiting for or requesting another recruitment detail response.
        if (hasRoster && CheckCompletion(current!.Count))
        {
            CancelOwned(host); Pending = false; failed = false;
            Status = $"파티 모집 완료 · {RecruitmentCapacity}/{RecruitmentCapacity}명";
            return;
        }

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
                successes++; failures = 0; failed = false; nextSupplement = now + Settings.PeriodMilliseconds;
                confirmedGeneration = requestedGeneration;
                nextAttempt = now + MinimumQueryInterval;
                // A -> B -> A while the request was running is still a change.
                Pending = generation != requestedGeneration;
                Status = Pending ? "조회 중 명단이 바뀌어 다시 확인합니다." : Watching;
                RememberCapacity(host);
                if (CheckCompletion(observed!.Count)) {
                    Pending = false;
                    Status = $"파티 모집 완료 · {RecruitmentCapacity}/{RecruitmentCapacity}명";
                }
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
        if (!Pending) { Status = Watching; return; }
        if (failed && !Settings.RetryEnabled && !periodicDue)
        { Status = "조회 실패 · 자동 재시도 꺼짐 · " + (Settings.PeriodicEnabled ? "새 명단 변화 또는 주기 조회 대기" : "새 명단 변화 대기"); return; }
        if (now < nextAttempt && !periodicDue) return;
        if (host.QueryBlockReason(false) is { } reason)
        { Status = "자동 조회 대기 · " + reason; return; }

        detailBefore = host.DetailRevision; requestedGeneration = generation; started = now;
        nextSupplement = now + Settings.PeriodMilliseconds;
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

    private bool CheckCompletion(int members)
    {
        // More members than the cached recruitment can hold means that capacity
        // no longer describes this party. Require another successful own read.
        if (members > RecruitmentCapacity) RecruitmentCapacity = 0;
        var full = RecruitmentCapacity > 0 && members == RecruitmentCapacity;
        if (full && !complete) CompletionRevision++;
        complete = full;
        return full;
    }

    private void RememberCapacity(IAfkListingHost host)
    {
        var capacity = host.RegisteredCapacity;
        RecruitmentCapacity = capacity is >= 1 and <= 48 ? capacity : 0;
        capacityRequestRevision = host.RequestRevision; capacityDetailRevision = host.DetailRevision;
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
        failed = true; failedGeneration = generation;
        failures = Math.Min(failures + 1, 5);
        var wait = Settings.RetryMilliseconds;
        nextAttempt = now + wait;
        nextSupplement = now + Settings.PeriodMilliseconds;
        Status = Settings.RetryEnabled ? $"{message} ({wait / 1000}초 후 재시도)" : "조회 실패 · 자동 재시도가 꺼져 있습니다.";
    }
}
