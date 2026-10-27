using System.Globalization;
using System.Text;

namespace PartyViewKR;

internal enum GameChatSendKind { Party, Alliance, Tell, Reply }
internal sealed record ChatReplyTarget(string Name, string World);
internal sealed record ChatSendState(string Session, ulong Self, ChatReplyTarget? Reply, string Reason);
internal sealed record ChatSendDraft(GameChatSendKind Kind, string Session, ulong Self, ChatReplyTarget? Reply);
internal sealed record ChatSendRequest(ChatSendDraft Draft, string Text, string Target = "");
internal sealed record ChatSendResult(bool Requested, string Message);

internal interface IGameChatSendHost
{
    ulong SelfId { get; }
    string? BlockReason { get; }
    // Called only from Framework.Update. True means handed to the game, not a server acknowledgement.
    bool TrySend(string command, ChatReplyTarget? reply, out string error);
}

internal static class GameChatSendText
{
    private static readonly UTF8Encoding Utf8 = new(false, true);
    internal static string Label(GameChatSendKind kind) => kind switch {
        GameChatSendKind.Party => "파티", GameChatSendKind.Alliance => "연합파티",
        GameChatSendKind.Tell => "귓속말", GameChatSendKind.Reply => "귓속말 답장", _ => ""
    };

    internal static bool TryTarget(string text, out ChatReplyTarget target)
    {
        target = null!;
        var parts = text.Trim().Split('@');
        if (parts.Length != 2) return false;
        var name = parts[0]; var world = parts[1];
        if (name.Length is < 1 or > 64 || world.Length is < 1 or > 40 || name != name.Trim() || world != world.Trim() ||
            !name.Any(char.IsLetter) || !world.Any(char.IsLetter) ||
            name.Any(c => !char.IsLetter(c) && c is not (' ' or '\'' or '-')) ||
            world.Any(c => !char.IsLetterOrDigit(c) && c != '-')) return false;
        target = new(name, world); return true;
    }

    internal static bool TryBuild(ChatSendRequest request, out string command, out string error)
    {
        command = ""; error = "";
        var text = request.Text;
        if (string.IsNullOrWhiteSpace(text)) { error = "보낼 내용을 입력해 주세요."; return false; }
        if (text.Any(c => char.IsControl(c) || char.GetUnicodeCategory(c) is UnicodeCategory.Format or UnicodeCategory.LineSeparator or UnicodeCategory.ParagraphSeparator)) {
            error = "채팅은 줄바꿈과 제어문자 없이 한 줄로 입력해 주세요."; return false;
        }
        var prefix = request.Draft.Kind switch {
            GameChatSendKind.Party => "/p ", GameChatSendKind.Alliance => "/a ", GameChatSendKind.Reply => "/r ", _ => ""
        };
        if (request.Draft.Kind == GameChatSendKind.Tell) {
            if (!TryTarget(request.Target, out var target)) { error = "귓속말 대상은 이름@서버 형식으로 입력해 주세요."; return false; }
            prefix = $"/tell {target.Name}@{target.World} ";
        }
        if (prefix.Length == 0) { error = "지원하지 않는 채팅 종류입니다."; return false; }
        var built = prefix + text;
        try {
            if (Utf8.GetByteCount(built) > 500) { error = "채팅 명령과 상대 이름을 포함해 UTF-8 500바이트까지 보낼 수 있습니다. 내용을 줄여 주세요."; return false; }
        }
        catch (EncoderFallbackException) { error = "전송할 수 없는 문자가 포함되어 있습니다."; return false; }
        command = built; return true;
    }
}

// Discord only reads snapshots and queues one bounded request. Native access,
// identity checks and the one-shot send all run on the game's framework thread.
internal sealed class GameChatSendBridge
{
    private readonly object gate = new();
    private ChatSendState state = new(Guid.NewGuid().ToString("N"), 0, null, "게임 상태를 확인하는 중입니다.");
    private long published, nextSend;
    private bool stopped;
    private Pending? pending;
    private sealed record Pending(ChatSendRequest Request, string Command, long At, CancellationToken Cancellation,
        TaskCompletionSource<ChatSendResult> Result);

    internal ChatSendState Read(long now)
    {
        lock (gate) return !stopped && now - published > 5000
            ? state with { Reason = "게임 응답을 기다리고 있습니다. 잠시 후 다시 시도해 주세요." } : state;
    }

    // Observe all incoming tells while the bot is connected, even when Discord
    // forwarding is disabled. Never guess a previous recipient after a read error.
    internal void ObserveTell(ulong self, GameChatMessage message)
    {
        lock (gate) {
            if (stopped || self == 0 || self != state.Self || message.Kind != GameChatKind.Tell) return;
            state = state with { Reply = GameChatSendText.TryTarget(message.Name + "@" + message.World, out var target) ? target : null };
        }
    }
    internal void InvalidateReply() { lock (gate) state = state with { Reply = null }; }

    internal Task<ChatSendResult> Queue(ChatSendRequest request, long now, CancellationToken cancellation)
    {
        lock (gate) {
            string? error = cancellation.IsCancellationRequested ? "전송 요청을 취소했습니다." : Check(request.Draft, now);
            if (error == null && pending != null) error = "앞선 채팅 전송을 처리 중입니다.";
            if (error == null && now < nextSend) error = "잠시 후 다음 메시지를 보내 주세요.";
            if (error != null) return Task.FromResult(new ChatSendResult(false, error));
            if (!GameChatSendText.TryBuild(request, out var command, out var invalid)) return Task.FromResult(new ChatSendResult(false, invalid));
            var result = new TaskCompletionSource<ChatSendResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            pending = new(request, command, now, cancellation, result);
            return result.Task;
        }
    }

    private string? Check(ChatSendDraft draft, long now)
    {
        if (stopped) return "디스코드 연결이 종료되어 전송하지 않았습니다.";
        if (draft.Session != state.Session || draft.Self == 0 || draft.Self != state.Self) return "게임 접속 상태가 바뀌었습니다. /pvkr-send로 다시 작성해 주세요.";
        if (now - published > 5000) return "게임 응답이 늦어 전송하지 않았습니다. 다시 시도해 주세요.";
        if (state.Reason.Length != 0) return state.Reason;
        if (draft.Kind == GameChatSendKind.Reply && (draft.Reply == null || draft.Reply != state.Reply))
            return "최근 귓속말 상대가 바뀌어 전송하지 않았습니다. /pvkr-send 답장으로 상대를 다시 확인해 주세요.";
        return null;
    }

    internal void Update(IGameChatSendHost host, long now)
    {
        lock (gate) {
            if (stopped) return;
            try {
                var self = host.SelfId;
                if (self != state.Self) {
                    state = new(Guid.NewGuid().ToString("N"), self, null, ""); nextSend = 0;
                }
                state = state with { Reason = self == 0 ? "게임에 로그인해 주세요." : host.BlockReason ?? "" };
                published = now;
                if (pending is not { } work) return;
                pending = null; // Claim before invoking the game. No exception can replay it.
                var error = work.Cancellation.IsCancellationRequested ? "전송 요청을 취소했습니다." :
                    now - work.At > 5000 ? "전송 대기 시간이 지나 전송하지 않았습니다." : Check(work.Request.Draft, now);
                if (error != null) { work.Result.TrySetResult(new(false, error)); return; }
                nextSend = now + 1000;
                try {
                    var ok = host.TrySend(work.Command, work.Request.Draft.Kind == GameChatSendKind.Reply ? work.Request.Draft.Reply : null, out error);
                    work.Result.TrySetResult(new(ok, ok
                        ? GameChatSendText.Label(work.Request.Draft.Kind) + " 전송을 게임에 요청했습니다."
                        : error));
                }
                catch (Exception) { work.Result.TrySetResult(new(false, "게임의 전송 결과를 확인하지 못했습니다. 중복 방지를 위해 자동 재전송하지 않습니다.")); }
            }
            catch (Exception) {
                state = state with { Reply = null, Reason = "게임 채팅 상태를 읽지 못했습니다." };
                pending?.Result.TrySetResult(new(false, state.Reason)); pending = null;
            }
        }
    }

    internal void Stop()
    {
        lock (gate) {
            stopped = true;
            state = new(Guid.NewGuid().ToString("N"), 0, null, "디스코드 연결이 꺼져 있습니다.");
            pending?.Result.TrySetResult(new(false, "연결이 종료되어 전송하지 않았습니다.")); pending = null;
        }
    }
}
