namespace PartyViewKR;

internal sealed partial class ListingDiscordSession
{
    private int closing;
    private readonly object seenGate = new();

    private bool Remember(ulong id)
    {
        lock (seenGate)
        {
            if (!seen.Add(id)) return false;
            seenOrder.Enqueue(id);
            while (seenOrder.Count > 1024) seen.Remove(seenOrder.Dequeue());
            return true;
        }
    }

    private async Task CloseEditor(DiscordInput input, IDiscordResponse response)
    {
        if (Interlocked.CompareExchange(ref closing, 1, 0) != 0)
        { await SafeScreen(response, Message("수정창을 닫고 있습니다.")); return; }
        var deferred = false;
        try
        {
            if (!Remember(input.Id)) { await SafeScreen(response, Message("이미 접수한 요청입니다.")); return; }
            var session = input.CustomId[ListingDiscordPanel.ClosePrefix.Length..];
            if (!Guid.TryParseExact(session, "N", out _) || session != bridge.ReadView().Session)
            { await SafeScreen(response, Message("접속 상태가 바뀌었습니다. /pvkr로 다시 열어 주세요.")); return; }
            await response.Defer(); deferred = true;
            cancellation.ThrowIfCancellationRequested();
            var id = ListingDiscordPanel.RequestId(input.Id);
            var reply = bridge.QueueCloseEditor(id, session, Now);
            if (Pending(reply))
            {
                await response.Screen(ListingDiscordPanel.RecoveryMessage("진행을 중단하고 수정창을 닫는 중입니다.", session), true, true);
                var started = Environment.TickCount64;
                do {
                    await Task.Delay(200, cancellation);
                    reply = bridge.ReadRecovery(id);
                    if (Environment.TickCount64 - started >= 15000) {
                        await response.Screen(Message("게임 응답을 확인하지 못했습니다. /pvkr로 다시 확인해 주세요."), true, true);
                        return;
                    }
                } while (Pending(reply));
            }
            await response.Screen(Message(Describe(reply)), true, true);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (Exception) { await SafeScreen(response, Message("닫기 요청의 응답을 확인하지 못했습니다. /pvkr로 상태를 확인해 주세요."), deferred); }
        finally { Volatile.Write(ref closing, 0); }
    }
}
