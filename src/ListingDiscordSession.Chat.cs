namespace PartyViewKR;

internal sealed partial class ListingDiscordSession
{
    private async Task ConfigureChat(DiscordInput input, IDiscordResponse response)
    {
        var deferred = false;
        try {
            if (!Remember(input.Id)) { await SafeScreen(response, Message("이미 접수한 요청입니다.")); return; }
            if (chat == null || input.ChatCommand == null) {
                await SafeScreen(response, Message("/pvkr-chat 명령의 종류와 옵션을 다시 선택해 주세요.")); return;
            }
            await response.Defer(); deferred = true;
            cancellation.ThrowIfCancellationRequested();
            var result = await chat.Configure(input.ChatCommand, input.GuildId, cancellation);
            await response.Screen(Message(result), false, true);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (Exception) { await SafeScreen(response, Message("채팅 설정 응답을 확인하지 못했습니다. /pvkr-chat 상태로 확인해 주세요."), deferred); }
    }
}
