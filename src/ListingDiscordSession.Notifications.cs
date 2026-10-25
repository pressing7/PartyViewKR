namespace PartyViewKR;

internal sealed partial class ListingDiscordSession
{
    private async Task SelectNotificationChannel(DiscordInput input, IDiscordResponse response)
    {
        var deferred = false;
        try
        {
            var current = bridge.ReadView();
            var session = input.CustomId[ListingDiscordPanel.NotificationPrefix.Length..];
            if (!Guid.TryParseExact(session, "N", out _) || session != current.Session)
            { await SafeScreen(response, Message("접속 상태가 바뀌었습니다. /pvkr로 다시 열어 주세요.")); return; }
            if (!Remember(input.Id)) { await SafeScreen(response, Message("이미 접수한 요청입니다.")); return; }
            if (notifications == null) { await SafeScreen(response, Message("디스코드 알림 연결을 준비하지 못했습니다.")); return; }
            await response.Defer(); deferred = true;
            cancellation.ThrowIfCancellationRequested();
            var message = await notifications.SelectChannel(input.GuildId, input.ChannelId, cancellation);
            await response.Screen(Message(message), false, true);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (Exception) { await SafeScreen(response, Message("알림 채널 설정 응답을 확인하지 못했습니다. /pvkr로 상태를 확인해 주세요."), deferred); }
    }
}
