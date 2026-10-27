using Discord;

namespace PartyViewKR;

internal sealed partial class ListingDiscordSession
{
    internal const string ChatSendPrefix = "pvkr:send:";
    private readonly Dictionary<string, (ChatSendDraft Draft, long Expires)> chatDrafts = new(StringComparer.Ordinal);
    private int handlingChatSend;

    private async Task SendChat(DiscordInput input, IDiscordResponse response)
    {
        if (Interlocked.CompareExchange(ref handlingChatSend, 1, 0) != 0) {
            await SafeScreen(response, Message("앞선 채팅 전송을 처리 중입니다.")); return;
        }
        var deferred = false;
        try {
            if (!Remember(input.Id)) { await SafeScreen(response, Message("이미 접수한 요청입니다.")); return; }
            if (chatSending == null) { await SafeScreen(response, Message("채팅 전송 연결을 준비 중입니다.")); return; }
            foreach (var expired in chatDrafts.Where(p => p.Value.Expires < Now).Select(p => p.Key).ToArray()) chatDrafts.Remove(expired);
            if (input.Command == "pvkr-send") {
                if (input.SendKind is not { } kind || !Enum.IsDefined(kind)) { await SafeScreen(response, Message("/pvkr-send에서 채팅 종류를 선택해 주세요.")); return; }
                var state = chatSending.Read(Now);
                if (state.Reason.Length > 0 || state.Self == 0) { await SafeScreen(response, Message(state.Reason.Length > 0 ? state.Reason : "게임에 로그인해 주세요.")); return; }
                if (kind == GameChatSendKind.Reply && state.Reply == null) {
                    await SafeScreen(response, Message("디스코드 연결 후 받은 귓속말이 없습니다. 새 귓속말을 받은 뒤 답장해 주세요.")); return;
                }
                var draft = new ChatSendDraft(kind, state.Session, state.Self, kind == GameChatSendKind.Reply ? state.Reply : null);
                var id = ChatSendPrefix + Guid.NewGuid().ToString("N");
                var modal = new ModalBuilder().WithCustomId(id).WithTitle(GameChatSendText.Label(kind) + " 보내기");
                if (kind == GameChatSendKind.Tell)
                    modal.AddTextInput("대상 · 이름@서버", "target", TextInputStyle.Short, maxLength: 105, required: true, placeholder: "모험가@펜리르");
                var label = draft.Reply is { } target ? $"{target.Name}@{target.World}에게 답장" : "내용";
                if (label.Length > 45) label = label[..44] + "…";
                modal.AddTextInput(label, "message", TextInputStyle.Short, maxLength: 500, required: true);
                while (chatDrafts.Count >= 16) chatDrafts.Remove(chatDrafts.MinBy(p => p.Value.Expires).Key);
                chatDrafts[id] = (draft, Now + 15 * 60_000);
                try { await response.Modal(modal.Build()); }
                catch { chatDrafts.Remove(id); throw; }
                return;
            }
            if (!chatDrafts.Remove(input.CustomId, out var opened) || opened.Expires < Now) {
                await SafeScreen(response, Message("입력창이 만료되었거나 이미 전송했습니다. /pvkr-send로 다시 작성해 주세요.")); return;
            }
            var keys = opened.Draft.Kind == GameChatSendKind.Tell ? new[] { "message", "target" } : ["message"];
            if (input.Fields.Count != keys.Length || keys.Any(k => !input.Fields.ContainsKey(k)) || input.Checks.Count != 0 || input.Choices.Count != 0 || input.Values.Length != 0) {
                await SafeScreen(response, Message("채팅 입력 항목을 확인하지 못했습니다. /pvkr-send로 다시 작성해 주세요.")); return;
            }
            var request = new ChatSendRequest(opened.Draft, input.Fields["message"], input.Fields.GetValueOrDefault("target", ""));
            if (!GameChatSendText.TryBuild(request, out _, out var invalid)) { await SafeScreen(response, Message(invalid)); return; }
            // No game action before a successful Discord ACK.
            await response.Defer(); deferred = true;
            using var budget = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            budget.CancelAfter(TimeSpan.FromSeconds(10));
            var result = await chatSending.Queue(request, Now, budget.Token).WaitAsync(budget.Token);
            await response.Screen(Message(result.Message), false, true);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (OperationCanceledException) { await SafeScreen(response, Message("채팅 전송 응답을 확인하지 못했습니다. 자동 재전송하지 않습니다."), deferred); }
        catch (Exception) { await SafeScreen(response, Message("채팅 전송 요청의 응답을 확인하지 못했습니다. 자동 재전송하지 않습니다."), deferred); }
        finally { Volatile.Write(ref handlingChatSend, 0); }
    }
}
