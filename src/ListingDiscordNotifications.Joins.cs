namespace PartyViewKR;

internal sealed partial class ListingDiscordNotifications
{
    internal const int MaxPendingJoins = 64;
    private readonly Queue<PartyJoinNotice> pendingJoins = new();
    private CancellationTokenSource? joinSending;
    private bool joinWatching;
    private ulong joinSelf;
    private long joinSeenRevision, joinEpoch;
    private int joinDropped;
    private string joinStatus = "";
    internal Task JoinDelivery { get; private set; } = Task.CompletedTask;

    internal static string JoinText(PartyJoinNotice notice) =>
        $"@everyone 파티원 {notice.JoinedCount}명이 참가했습니다. (확인 인원: {notice.MemberCount}명)";

    internal void ObserveJoins(bool enabled, ulong selfId, PartyJoinNotice? notice)
    {
        lock (gate)
        {
            if (disposed) return;
            if (!enabled || selfId == 0 || !transport.Connected)
            {
                if (joinWatching) ResetJoinWatching();
                return;
            }
            if (!joinWatching || joinSelf != selfId)
            {
                ResetJoinWatching();
                joinWatching = true; joinSelf = selfId;
                // Connecting or enabling establishes a baseline, never a replay
                // of joins observed before this notifier started watching.
                joinSeenRevision = notice?.Revision ?? 0;
                joinStatus = "파티원 참가 알림 감시 중";
                return;
            }
            if (notice == null || notice.Revision <= joinSeenRevision) return;
            joinSeenRevision = notice.Revision;
            if (notice.JoinedCount is < 1 or > 47 || notice.MemberCount is < 2 or > 48 ||
                notice.JoinedCount >= notice.MemberCount) return;
            if (target == null)
            {
                joinStatus = "파티원 참가 알림을 보내려면 조회 카드에서 알림 채널을 지정해 주세요.";
                return;
            }
            if (pendingJoins.Count == MaxPendingJoins) { pendingJoins.Dequeue(); joinDropped++; }
            pendingJoins.Enqueue(notice);
            StartJoinDelivery();
        }
    }

    // All helpers touching notification state run under gate.
    private void ResetJoinWatching()
    {
        joinWatching = false; joinSelf = 0; joinSeenRevision = 0; joinEpoch++;
        pendingJoins.Clear(); joinSending?.Cancel(); joinStatus = ""; joinDropped = 0;
    }

    private void StartJoinDelivery()
    {
        if (disposed || !joinWatching || !transport.Connected || target == null ||
            joinSending != null || pendingJoins.Count == 0) return;
        var selected = target; var observedSelf = joinSelf; var epoch = joinEpoch;
        var lifetime = CancellationTokenSource.CreateLinkedTokenSource(stop.Token);
        joinSending = lifetime;
        JoinDelivery = Task.Run(() => DeliverJoins(selected, observedSelf, epoch, lifetime));
    }

    private bool JoinContextMatches(DiscordNotificationTarget selected, ulong observedSelf, long epoch) =>
        !disposed && joinWatching && transport.Connected && target == selected && joinSelf == observedSelf && joinEpoch == epoch;

    private async Task DeliverJoins(DiscordNotificationTarget selected, ulong observedSelf, long epoch,
        CancellationTokenSource lifetime)
    {
        try
        {
            while (!lifetime.IsCancellationRequested)
            {
                PartyJoinNotice notice;
                lock (gate)
                {
                    if (!JoinContextMatches(selected, observedSelf, epoch) || pendingJoins.Count == 0) return;
                    notice = pendingJoins.Dequeue();
                    joinStatus = "파티원 참가 알림을 보내는 중입니다.";
                }
                using var budget = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
                budget.CancelAfter(TimeSpan.FromSeconds(15));
                try
                {
                    var error = await transport.CheckChannel(selected, budget.Token);
                    budget.Token.ThrowIfCancellationRequested();
                    lock (gate)
                    {
                        if (!JoinContextMatches(selected, observedSelf, epoch)) return;
                        if (error != null) { joinStatus = "파티원 참가 알림 실패 · " + error; continue; }
                    }
                    // Each revision is consumed once. Never retry an uncertain
                    // POST; later distinct joins still get their own alert.
                    await transport.SendPartyJoin(selected, notice, budget.Token);
                    SetJoinResult($"파티원 {notice.JoinedCount}명 참가 · everyone 알림 전송 완료", selected, observedSelf, epoch);
                }
                catch (OperationCanceledException)
                { SetJoinResult("파티원 참가 알림 전송이 중단됐거나 시간이 초과됐습니다. 같은 알림은 자동 재전송하지 않습니다.", selected, observedSelf, epoch); }
                catch (Exception e)
                { SetJoinResult("파티원 참가 알림 전송 실패 · " + e.GetType().Name + " · 같은 알림은 자동 재전송하지 않습니다.", selected, observedSelf, epoch); }
            }
        }
        finally
        {
            lock (gate)
            {
                if (ReferenceEquals(joinSending, lifetime))
                {
                    joinSending = null;
                    // A new character/channel can begin watching while the old
                    // cancelled worker is unwinding. Its queue must still drain.
                    StartJoinDelivery();
                }
            }
            lifetime.Dispose();
        }
    }

    private void SetJoinResult(string message, DiscordNotificationTarget selected, ulong observedSelf, long epoch)
    {
        lock (gate) if (JoinContextMatches(selected, observedSelf, epoch)) joinStatus = message;
    }
}
