using System.Security.Cryptography;
using System.Text.Json;
using Discord;

namespace PartyViewKR;

internal interface IDiscordResponse
{
    Task Defer();
    Task Modal(Modal modal);
    Task Screen(DiscordScreen screen, bool replace, bool deferred);
}

// One editor per personal bot. This class is independent of the Gateway so ACK
// failures, repeated events and shutdown can be tested without a real token.
internal sealed partial class ListingDiscordSession(ListingWebBridge bridge, ulong appId, ulong ownerId,
    CancellationToken cancellation, Func<long>? clock = null, ListingDiscordIcons? iconCatalog = null,
    ListingDiscordNotifications? notifications = null)
{
    private readonly ListingDiscordPanel panel = new(bridge, appId, ownerId, iconCatalog, () => notifications?.Status ?? "");
    private readonly HashSet<ulong> seen = [];
    private readonly Queue<ulong> seenOrder = [];
    private int handling;
    private long Now => clock?.Invoke() ?? Environment.TickCount64;
    private static DiscordScreen Message(string text) => ListingDiscordCard.Message(text);

    internal async Task Process(DiscordInput input, IDiscordResponse response)
    {
        if (cancellation.IsCancellationRequested) return;
        if (!panel.Authorized(input)) {
            await SafeScreen(response, Message("이 개인 봇은 봇 소유자만 사용할 수 있습니다.")); return;
        }
        // Recovery must remain usable even while another interaction is polling
        // the game or waiting for a failed Discord response.
        if (input.Command.Length == 0 && input.CustomId.StartsWith(ListingDiscordPanel.ClosePrefix, StringComparison.Ordinal))
        { await CloseEditor(input, response); return; }
        if (input.Command.Length == 0 && input.CustomId.StartsWith(ListingDiscordPanel.NotificationPrefix, StringComparison.Ordinal))
        { await SelectNotificationChannel(input, response); return; }
        if (Interlocked.CompareExchange(ref handling, 1, 0) != 0) {
            await SafeScreen(response, ListingDiscordPanel.RecoveryMessage("앞선 요청을 처리 중입니다.", bridge.ReadView().Session)); return;
        }
        DiscordWork? work = null;
        var deferred = false;
        try
        {
            if (!Remember(input.Id)) { await SafeScreen(response, Message("이미 접수한 요청입니다.")); return; }
            var decision = panel.Handle(input, Now);
            if (decision.Modal != null) { await response.Modal(decision.Modal); return; }
            work = decision.Work;
            if (work == null) {
                if (decision.Screen != null) await response.Screen(decision.Screen, decision.Replace, false);
                return;
            }
            // Never start a game action if Discord did not acknowledge it.
            await response.Defer(); deferred = true;
            cancellation.ThrowIfCancellationRequested();
            var reply = panel.Start(work, Now);
            if (work.Body != null) CryptographicOperations.ZeroMemory(work.Body);
            if (Pending(reply)) {
                await response.Screen(ListingDiscordPanel.RecoveryMessage(work.Query ? "게임에서 내 모집글을 조회하고 있습니다." : "변경사항을 적용하고 모집글을 갱신하고 있습니다.", work.Session), true, true);
                var started = Environment.TickCount64;
                do {
                    await Task.Delay(400, cancellation);
                    reply = panel.Progress(work);
                    if (Environment.TickCount64 - started >= 180000) {
                        await response.Screen(panel.Complete(Now, "게임 처리 결과를 기다리는 시간이 길어졌습니다. /pvkr로 현재 상태를 확인해 주세요.").Screen!, true, true);
                        return;
                    }
                } while (Pending(reply));
            }
            await response.Screen(panel.Complete(Now, Describe(reply)).Screen!, true, true);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (Exception)
        {
            // Do not log Discord tokens, interaction URLs, PINs or input bodies.
            // An uncertain network response never retries a game mutation.
            await SafeScreen(response, Message(deferred ?
                "디스코드 응답을 보내지 못했습니다. /pvkr로 현재 상태를 확인해 주세요." :
                "요청을 처리하지 못했습니다. /pvkr로 화면을 다시 열어 주세요."), deferred);
        }
        finally {
            if (work?.Body != null) CryptographicOperations.ZeroMemory(work.Body);
            if (cancellation.IsCancellationRequested) panel.Clear();
            Volatile.Write(ref handling, 0);
        }
    }

    internal void ClearIfIdle()
    {
        if (Interlocked.CompareExchange(ref handling, 1, 0) != 0) return;
        try { panel.Clear(); } finally { Volatile.Write(ref handling, 0); }
    }
    private async Task SafeScreen(IDiscordResponse response, DiscordScreen screen, bool deferred = false)
    {
        if (cancellation.IsCancellationRequested) return;
        try { await response.Screen(screen, false, deferred); } catch (Exception) { }
    }
    private static bool Pending(WebReply reply) => reply.Body is WebQuery { State: "queued" or "running" } or WebOperation { State: "queued" or "running" } or WebRecovery { State: "queued" or "running" };
    internal static string ResultSummary(WebOperation operation) => operation.Results.Length == 0 ? "" :
        $"적용 {operation.Results.Count(r => r.Applied == true)}개 · 미적용 {operation.Results.Count(r => r.Applied == false)}개 · 확인 필요 {operation.Results.Count(r => r.Applied == null)}개";
    internal static string Describe(WebReply reply)
    {
        if (reply.Body is WebQuery query) return query.Message;
        if (reply.Body is WebRecovery recovery) return recovery.Message;
        if (reply.Body is WebOperation operation) {
            var summary = ResultSummary(operation);
            return (summary.Length > 0 ? summary + "\n" : "") + operation.Message +
                (operation.Results.Length == 0 ? "" : "\n\n" + string.Join("\n\n", operation.Results.Select(r =>
                    $"[{(r.Applied == true ? "적용" : r.Applied == false ? "미적용" : "확인 필요")}] {r.Title}\n{r.Message}")));
        }
        var body = JsonSerializer.SerializeToElement(reply.Body);
        return body.TryGetProperty("error", out var error) ? error.GetString() ?? "요청을 처리하지 못했습니다." : "요청을 처리하지 못했습니다.";
    }
}
