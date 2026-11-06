namespace PartyViewKR;

internal interface IAfkRefreshHost
{
    bool AfkEnabled { get; }
    bool LoggedIn { get; }
    ulong SelfId { get; }
    bool RecruitmentKnown { get; }
    bool RosterReadable { get; }
    bool RecruitmentComplete { get; }
    string? RefreshBlockReason(bool ownQueuedRefresh);
    void WakeReader();
}

// Framework-thread scheduling only. Each run still queries and validates the
// current own listing through ListingRefresh before opening or submitting it.
internal sealed class AfkRefreshScheduler(ListingRefresh refresh)
{
    private readonly ListingRefreshGuard guard = new();
    private AfkSettings settings = new();
    private bool scheduled, observedFinished;
    private ulong self;
    private long nextDue, ownedRevision = -1, observedRevision = -1;
    private int requests, completed, unconfirmed, stopped;
    internal bool InFlight => ownedRevision >= 0 && ownedRevision == refresh.RequestRevision && refresh.Busy;
    internal string Status { get; private set; } = "주기적 모집 갱신이 꺼져 있습니다.";
    internal string LastResult { get; private set; } = "";
    internal long? NextDue => scheduled ? nextDue : null;
    internal string Diagnostic => $"autoRefreshEnabled={settings.RefreshEnabled}; autoRefreshMinutes={settings.RefreshMinutes}; " +
        $"autoRefreshScheduled={scheduled}; autoRefreshInFlight={InFlight}; autoRefreshRequests={requests}; " +
        $"autoRefreshCompleted={completed}; autoRefreshUnconfirmed={unconfirmed}; autoRefreshStopped={stopped}; " +
        $"autoRefreshOwnedRevision={ownedRevision}";

    internal void Configure(AfkSettings value, long now)
    {
        value = value.Normalize();
        if (settings.RefreshEnabled != value.RefreshEnabled || settings.RefreshMinutes != value.RefreshMinutes)
            nextDue = now + value.RefreshMilliseconds;
        settings = value;
        if (!scheduled && !InFlight)
            Status = settings.RefreshEnabled ? "AFK 모드를 켜면 주기적으로 모집을 갱신합니다." : "주기적 모집 갱신이 꺼져 있습니다.";
    }

    internal void Postpone(long now) => nextDue = now + settings.RefreshMilliseconds;

    // Called before ListingReader.Read/Refresh.Update, including the queued
    // frame. A disabled schedule may finish an already opened editor, but it
    // cannot start another run or open an editor that has not been opened yet.
    internal void BeforeRead(IAfkRefreshHost host, long now)
    {
        Synchronize(host, now);
        if (!InFlight) return;
        var keepRun = host.LoggedIn && (scheduled || refresh.EditorStarted);
        if (guard.BeforeRead(host.SelfId, refresh.RequestRevision, refresh.Busy, keepRun,
                refresh.Stage == RefreshStage.Queued, now) ||
            !refresh.EditorStarted && host.RecruitmentComplete ||
            refresh.Stage == RefreshStage.Queued && host.RefreshBlockReason(true) != null)
            refresh.Cancel();
    }

    internal void Update(IAfkRefreshHost host, long now)
    {
        Synchronize(host, now);
        ObserveRefresh(now);
        if (InFlight)
        {
            Status = (scheduled ? "자동 갱신 중 · " : "진행 중인 갱신을 마무리한 뒤 꺼집니다 · ") + refresh.Status;
            return;
        }
        if (!scheduled)
        {
            Status = settings.RefreshEnabled ? "AFK 모드를 켜면 주기적으로 모집을 갱신합니다." : "주기적 모집 갱신이 꺼져 있습니다.";
            return;
        }
        if (!host.LoggedIn || host.SelfId == 0 || host.SelfId != self)
        { Status = "자동 갱신 대기 · 접속 캐릭터를 확인하는 중입니다."; return; }
        if (!host.RosterReadable || !host.RecruitmentKnown)
        { Status = "자동 갱신 대기 · 현재 명단과 본인 모집정보가 확인되어야 합니다."; return; }
        if (host.RecruitmentComplete)
        { Status = "모집이 완료되어 자동 갱신을 기다립니다."; return; }
        if (now < nextDue)
        {
            var seconds = (nextDue - now + 999) / 1000;
            Status = $"다음 자동 갱신까지 {seconds / 60:00}:{seconds % 60:00}";
            return;
        }
        if (refresh.Busy || refresh.CoolingDown(now))
        { Status = "자동 갱신 대기 · 진행 중인 갱신이 끝나기를 기다립니다."; return; }
        if (host.RefreshBlockReason(false) is { } reason)
        { Status = "자동 갱신 대기 · " + reason; return; }

        // Never consume PendingChanges or reuse a cached edit target. Request
        // only the existing one-shot refresh, which gets fresh conditions first.
        var before = refresh.RequestRevision;
        nextDue = now + settings.RefreshMilliseconds;
        refresh.Request(now);
        if (refresh.RequestRevision == before)
        { LastResult = "자동 갱신을 시작하지 못했습니다. 다음 주기에 다시 확인합니다."; return; }
        requests++;
        ownedRevision = observedRevision = refresh.RequestRevision; observedFinished = false;
        guard.Started(self, ownedRevision, now);
        Status = "내 모집글을 새로 조회한 뒤 기존 조건으로 자동 갱신합니다.";
        host.WakeReader();
    }

    private void Synchronize(IAfkRefreshHost host, long now)
    {
        var enabled = host.AfkEnabled && settings.RefreshEnabled && host.LoggedIn;
        if (!enabled) { scheduled = false; return; }
        if (scheduled || host.SelfId == 0) return;
        scheduled = true; self = host.SelfId; nextDue = now + settings.RefreshMilliseconds;
        observedRevision = refresh.RequestRevision; observedFinished = !refresh.Busy;
    }

    private void ObserveRefresh(long now)
    {
        if (ownedRevision >= 0 && ownedRevision != refresh.RequestRevision)
        { ownedRevision = -1; guard.Clear(); } // A newer manual request is never cancelled.
        if (!scheduled && ownedRevision < 0) return;
        if (observedRevision != refresh.RequestRevision)
        { observedRevision = refresh.RequestRevision; observedFinished = false; }
        if (observedFinished || refresh.Busy) return;
        observedFinished = true;
        // Manual work (even cancelled/uncertain work) also restarts the interval
        // so becoming idle never immediately causes a second automatic submit.
        nextDue = now + settings.RefreshMilliseconds;
        if (ownedRevision != refresh.RequestRevision) return;
        LastResult = refresh.Status;
        if (refresh.Stage == RefreshStage.Completed) completed++;
        else if (refresh.Stage == RefreshStage.Unconfirmed) unconfirmed++;
        else stopped++;
        ownedRevision = -1; guard.Clear();
    }
}
