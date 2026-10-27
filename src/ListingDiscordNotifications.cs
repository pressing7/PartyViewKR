namespace PartyViewKR;

internal sealed record DiscordNotificationTarget(ulong ApplicationId, ulong GuildId, ulong ChannelId)
{
    internal bool ForApplication(ulong app) => app != 0 && ApplicationId == app && GuildId != 0 && ChannelId != 0;
}

internal interface IDiscordNotificationTransport
{
    bool Connected { get; }
    Task<string?> CheckChannel(DiscordNotificationTarget target, CancellationToken cancellation);
    Task SendCompletion(DiscordNotificationTarget target, CancellationToken cancellation, bool excludesSingleJob = false);
}

// Receives only managed AFK observations. No game memory or interaction token
// is retained here; alerts are new bot messages to an explicitly selected channel.
internal sealed class ListingDiscordNotifications : IDisposable
{
    internal const string CompletionText = "@everyone 파티모집완료";
    internal const string SingleJobCompletionText = "@everyone 단일 직업 빈자리를 제외한 파티모집완료";
    private readonly object gate = new();
    private readonly ulong applicationId;
    private readonly IDiscordNotificationTransport transport;
    private readonly Func<DiscordNotificationTarget, CancellationToken, Task> save;
    private readonly CancellationTokenSource stop;
    private readonly SemaphoreSlim selecting = new(1);
    private DiscordNotificationTarget? target;
    private CancellationTokenSource? sending;
    private bool watching, ready, excludesSingleJob, disposed;
    private ulong self, attemptedSelf;
    private long revision, attemptedRevision = -1;
    private bool attemptedPost;
    private DiscordNotificationTarget? attemptedTarget;
    private string status;
    internal string Status { get { lock (gate) return status; } }
    internal Task Delivery { get; private set; } = Task.CompletedTask;

    internal ListingDiscordNotifications(ulong app, IDiscordNotificationTransport transport,
        DiscordNotificationTarget? saved, Func<DiscordNotificationTarget, CancellationToken, Task> save,
        CancellationToken cancellation)
    {
        applicationId = app; this.transport = transport; this.save = save;
        stop = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        target = saved?.ForApplication(app) == true ? saved : null;
        status = target == null ? "서버 조회 카드에서 ‘알림 채널로 설정’을 눌러 주세요." : $"알림 채널 <#{target.ChannelId}> · AFK 모집 완료 감시 대기";
    }

    internal async Task<string> SelectChannel(ulong guildId, ulong channelId, CancellationToken cancellation)
    {
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(stop.Token, cancellation);
        budget.CancelAfter(TimeSpan.FromSeconds(15));
        var ct = budget.Token;
        if (guildId == 0 || channelId == 0) return "@everyone 알림은 서버의 텍스트 채널에서 설정해 주세요. DM에는 보낼 수 없습니다.";
        await selecting.WaitAsync(ct);
        try
        {
            var selected = new DiscordNotificationTarget(applicationId, guildId, channelId);
            var error = await transport.CheckChannel(selected, ct);
            if (error != null) return error;
            ct.ThrowIfCancellationRequested();
            await save(selected, ct).WaitAsync(ct);
            ct.ThrowIfCancellationRequested();
            lock (gate)
            {
                if (disposed) return "디스코드 연결이 종료됐습니다.";
                if (target != selected) sending?.Cancel();
                if (!attemptedPost) attemptedRevision = -1;
                target = selected;
                status = $"알림 채널 <#{channelId}> · AFK 모집 완료 알림을 보냅니다. 단일 직업 빈자리 제외 조건은 인게임 AFK 설정에서 켤 수 있습니다.";
                return status;
            }
        }
        catch (OperationCanceledException) when (!stop.IsCancellationRequested && !cancellation.IsCancellationRequested)
        { return "알림 채널 설정 시간이 초과됐습니다. 다시 시도해 주세요."; }
        catch (Exception e) when (e is not OperationCanceledException)
        { return "알림 채널을 확인하거나 저장하지 못했습니다. · " + e.GetType().Name; }
        finally { selecting.Release(); }
    }

    internal void Observe(bool afkEnabled, ulong selfId, long completionRevision, bool isReady, bool excludesSingleJob = false)
    {
        lock (gate)
        {
            if (disposed) return;
            var active = afkEnabled && selfId != 0;
            if (!active || !isReady || self != selfId || revision != completionRevision || this.excludesSingleJob != excludesSingleJob) sending?.Cancel();
            watching = active; self = selfId; revision = completionRevision; ready = isReady; this.excludesSingleJob = excludesSingleJob;
            if (!watching || !ready || revision <= 0 || target == null || !transport.Connected || sending != null) return;
            if (attemptedSelf == self && attemptedRevision == revision && attemptedTarget == target) return;
            var selected = target; var observedSelf = self; var observedRevision = revision;
            var budget = CancellationTokenSource.CreateLinkedTokenSource(stop.Token);
            budget.CancelAfter(TimeSpan.FromSeconds(15)); sending = budget;
            status = "파티 모집 완료 알림을 보내는 중입니다.";
            Delivery = Task.Run(() => Deliver(selected, observedSelf, observedRevision, excludesSingleJob, budget));
        }
    }

    private async Task Deliver(DiscordNotificationTarget selected, ulong observedSelf, long observedRevision, bool excludingSingleJob,
        CancellationTokenSource budget)
    {
        try
        {
            var error = await transport.CheckChannel(selected, budget.Token);
            budget.Token.ThrowIfCancellationRequested();
            lock (gate)
            {
                if (disposed || !watching || !ready || self != observedSelf || revision != observedRevision || target != selected || excludesSingleJob != excludingSingleJob) return;
                // Consume immediately before dispatch (or a definite permission
                // failure). Cancellation during preflight can safely retry after
                // the roster becomes readable again; an uncertain POST cannot.
                attemptedSelf = observedSelf; attemptedRevision = observedRevision; attemptedTarget = selected;
                attemptedPost = error == null;
                if (error != null) { status = "모집 완료 알림 실패 · " + error; return; }
            }
            await transport.SendCompletion(selected, budget.Token, excludingSingleJob);
            SetResult((excludingSingleJob ? "단일 직업 빈자리 제외 모집완료" : "파티모집완료") + " · everyone 알림 전송 완료", selected, observedSelf, observedRevision);
        }
        catch (OperationCanceledException)
        {
            SetResult("모집 완료 알림 전송이 중단됐거나 시간이 초과됐습니다. 중복 멘션 방지를 위해 같은 완료 알림을 자동 재전송하지 않습니다.", selected, observedSelf, observedRevision);
        }
        catch (Exception e)
        {
            SetResult("모집 완료 알림 전송 실패 · " + e.GetType().Name + " · 같은 완료 알림은 자동 재전송하지 않습니다.", selected, observedSelf, observedRevision);
        }
        finally
        {
            lock (gate) if (ReferenceEquals(sending, budget)) sending = null;
            budget.Dispose();
        }
    }

    private void SetResult(string message, DiscordNotificationTarget selected, ulong observedSelf, long observedRevision)
    {
        lock (gate)
            if (!disposed && watching && ready && target == selected && self == observedSelf && revision == observedRevision) status = message;
    }

    public void Dispose()
    {
        lock (gate) { if (disposed) return; disposed = true; watching = false; sending?.Cancel(); }
        stop.Cancel();
        // SelectChannel may still be unwinding. Do not dispose its semaphore or
        // cancellation source while an async caller still owns their handles.
    }
}
