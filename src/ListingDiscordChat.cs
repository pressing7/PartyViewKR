namespace PartyViewKR;

internal interface IDiscordChatTransport
{
    bool Connected { get; }
    Task<string?> CheckChannel(DiscordChatRoute route, CancellationToken cancellation);
    Task Send(DiscordChatRoute route, GameChatMessage message, CancellationToken cancellation);
}

// A single ordered worker keeps game callbacks free of network work. Queued
// messages retain their original route; configuration changes never redirect them.
internal sealed class ListingDiscordChat : IDisposable
{
    private const int QueueLimit = 200;
    private readonly object gate = new();
    private readonly IDiscordChatTransport transport;
    private readonly Func<DiscordChatSettings, CancellationToken, Task> save;
    private readonly CancellationTokenSource stop;
    private readonly SemaphoreSlim signal = new(0), editing = new(1);
    private readonly Queue<Pending> queue = new();
    private sealed record Pending(ulong Self, DiscordChatRoute Route, GameChatMessage Message);
    private DiscordChatSettings settings;
    private CancellationTokenSource? sending;
    private string sendingKind = "";
    private ulong self;
    private bool watching, disposed;
    private int sent, failed, dropped;
    private string last = "";
    internal Task Completion { get; }
    internal string Status { get { lock (gate) return StatusLocked(); } }
    internal bool Editable { get { lock (gate) return !disposed && !stop.IsCancellationRequested; } }
    internal DiscordChatSettings Settings { get { lock (gate) return settings with { Routes = settings.Routes.ToArray() }; } }
    internal int PendingCount { get { lock (gate) return queue.Count + (sending != null ? 1 : 0); } }

    internal ListingDiscordChat(ulong applicationId, IDiscordChatTransport transport, DiscordChatSettings? saved,
        Func<DiscordChatSettings, CancellationToken, Task> save, CancellationToken cancellation)
    {
        this.transport = transport; this.save = save;
        settings = (saved ?? new()).ForApplication(applicationId);
        stop = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        Completion = Task.Run(Pump);
    }

    internal void Observe(bool loggedIn, ulong selfId)
    {
        lock (gate) {
            if (disposed) return;
            var active = loggedIn && selfId != 0;
            if (!active || self != selfId) { dropped += queue.Count; queue.Clear(); sending?.Cancel(); }
            watching = active; self = active ? selfId : 0;
        }
    }

    internal bool Wants(GameChatKind kind)
    {
        lock (gate) return !disposed && watching && settings.Routes.Any(r => r.Enabled && r.Kind == GameChat.Key(kind));
    }

    internal bool Receive(ulong selfId, GameChatMessage message)
    {
        lock (gate) {
            if (disposed || !watching || selfId == 0 || self != selfId || !Enum.IsDefined(message.Kind)) return false;
            var route = settings.Routes.FirstOrDefault(r => r.Enabled && r.Kind == GameChat.Key(message.Kind));
            if (route == null || string.IsNullOrWhiteSpace(message.Text)) return false;
            if (message.Text.Length > 16384 || message.Name.Length > 512 || message.World.Length > 512) {
                failed++; last = "메시지가 너무 길어 전달하지 못했습니다."; return false;
            }
            if (queue.Count >= QueueLimit) {
                queue.Dequeue(); dropped++; last = "전송 대기 200건을 넘어 가장 오래된 채팅을 제외했습니다.";
            }
            var wake = queue.Count == 0;
            queue.Enqueue(new(self, route, message)); if (wake) signal.Release(); return true;
        }
    }

    internal async Task<string> Configure(DiscordChatCommand command, ulong guildId, CancellationToken cancellation)
    {
        if (command.Action == "상태") { lock (gate) return SummaryLocked(); }
        if (guildId == 0) return "채팅 수신 설정은 디스코드 서버에서 실행해 주세요.";
        if (!GameChat.TryKind(command.Kind, out var kind) || command.Action is not ("설정" or "멘션" or "해제"))
            return "채팅 종류와 명령을 다시 선택해 주세요.";
        if (command.MentionOnSound == true && kind == GameChatKind.Tell)
            return "효과음 멘션은 파티·연합파티 채팅에서만 사용할 수 있습니다.";
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(stop.Token, cancellation);
        budget.CancelAfter(TimeSpan.FromSeconds(15)); var ct = budget.Token;
        var acquired = false;
        try {
            await editing.WaitAsync(ct); acquired = true;
            DiscordChatSettings before; lock (gate) before = settings;
            var existing = before.Routes.FirstOrDefault(r => r.Kind == command.Kind);
            if (command.Action != "설정" && (existing == null || existing.GuildId != guildId))
                return "이 서버에 지정된 수신 설정이 없습니다. 먼저 ‘설정’ 명령으로 채널을 지정해 주세요.";
            DiscordChatRoute? route = null;
            if (command.Action != "해제") {
                if (command.Action == "설정" && command.ChannelId == 0 || command.Action == "멘션" && command.MentionEveryone == null)
                    return "수신 채널 또는 멘션 설정을 다시 선택해 주세요.";
                route = new() { Kind = command.Kind, GuildId = guildId,
                    ChannelId = command.Action == "설정" ? command.ChannelId : existing!.ChannelId,
                    Enabled = command.Action == "설정" || existing!.Enabled,
                    MentionEveryone = command.MentionEveryone ?? existing?.MentionEveryone ?? false,
                    MentionOnSound = command.MentionOnSound ?? (command.MentionEveryone.HasValue ? false : existing?.MentionOnSound ?? false) };
                var error = await transport.CheckChannel(route, ct);
                if (error != null) return error;
            }
            var updated = before with { Routes = before.Routes.Where(r => r.Kind != command.Kind)
                .Concat(route == null ? [] : new[] { route }).OrderBy(r => r.Kind, StringComparer.Ordinal).ToArray() };
            await Apply(updated, command.Kind, existing != route, ct);
            return route == null ? $"{GameChat.Label(kind)} 채팅 수신을 해제했습니다." : RouteSummary(kind, route);
        }
        catch (OperationCanceledException) when (!stop.IsCancellationRequested && !cancellation.IsCancellationRequested)
        { return "채팅 설정 시간이 초과됐습니다. 다시 시도해 주세요."; }
        catch (Exception e) when (e is not OperationCanceledException)
        { return "채팅 설정을 확인하거나 저장하지 못했습니다. · " + e.GetType().Name; }
        finally { if (acquired) editing.Release(); }
    }

    // Share the same serialization and latest settings with slash commands.
    // Turning a category off does not depend on Discord channel permissions.
    internal async Task<string> ConfigureLocal(GameChatKind kind, bool? enabled, bool? mention, CancellationToken cancellation, bool? sound = null)
    {
        if (!Enum.IsDefined(kind)) return "채팅 종류를 확인해 주세요.";
        if (sound == true && kind == GameChatKind.Tell) return "효과음 멘션은 파티·연합파티 채팅에서만 사용할 수 있습니다.";
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(stop.Token, cancellation);
        budget.CancelAfter(TimeSpan.FromSeconds(15)); var ct = budget.Token;
        var acquired = false;
        try {
            await editing.WaitAsync(ct); acquired = true;
            DiscordChatSettings before; lock (gate) before = settings;
            var route = before.Routes.FirstOrDefault(r => r.Kind == GameChat.Key(kind));
            if (route == null) return "먼저 /pvkr-chat 설정으로 수신 채널을 지정해 주세요.";
            var updated = before.WithOptions(kind, enabled, mention, sound);
            var next = updated.Routes.Single(r => r.Kind == route.Kind);
            await Apply(updated, route.Kind, route != next, ct);
            return $"{GameChat.Label(kind)} · 수신 {(next.Enabled ? "켜기" : "끄기")} · 멘션 {next.MentionLabel}";
        }
        catch (OperationCanceledException) when (!stop.IsCancellationRequested && !cancellation.IsCancellationRequested)
        { return "채팅 설정 시간이 초과됐습니다. 다시 시도해 주세요."; }
        finally { if (acquired) editing.Release(); }
    }

    private async Task Apply(DiscordChatSettings updated, string kind, bool changed, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested(); await save(updated, ct).WaitAsync(ct); ct.ThrowIfCancellationRequested();
        lock (gate) {
            if (disposed) throw new OperationCanceledException(ct);
            settings = updated;
            if (!changed) return;
            var kept = queue.Where(p => p.Route.Kind != kind).ToArray();
            dropped += queue.Count - kept.Length; queue.Clear(); foreach (var item in kept) queue.Enqueue(item);
            if (sendingKind == kind) sending?.Cancel();
        }
    }

    private static string RouteSummary(GameChatKind kind, DiscordChatRoute route) =>
        $"{GameChat.Label(kind)} → <#{route.ChannelId}> · 수신 {(route.Enabled ? "켜기" : "끄기")} · 멘션 {route.MentionLabel}";
    private string StatusLocked() => $"채팅 전달 · {(watching ? "수신 대기" : "로그인 대기")} · 수신 켜짐 {settings.Routes.Count(r => r.Enabled)}/3 · " +
        $"전송 {sent} · 실패 {failed} · 제외 {dropped} · 대기 {queue.Count}" + (last.Length == 0 ? "" : "\n" + last);
    private string SummaryLocked() => string.Join("\n", Enum.GetValues<GameChatKind>().Select(kind => {
        var route = settings.Routes.FirstOrDefault(r => r.Kind == GameChat.Key(kind));
        return route == null ? GameChat.Label(kind) + " · 수신 꺼짐" : RouteSummary(kind, route);
    })) + "\n\n" + StatusLocked();
    private bool Current(Pending pending) => !disposed && watching && self == pending.Self && pending.Route.Enabled && settings.Routes.Contains(pending.Route);

    private async Task Pump()
    {
        try {
            while (!stop.IsCancellationRequested) {
                await signal.WaitAsync(stop.Token);
                while (!stop.IsCancellationRequested) {
                    lock (gate) if (queue.Count == 0) break;
                    // Keep a bounded queue across temporary Gateway disconnects.
                    if (!transport.Connected) { await Task.Delay(500, stop.Token); continue; }
                    Pending pending; CancellationTokenSource budget;
                    lock (gate) {
                        if (queue.Count == 0) break;
                        pending = queue.Dequeue();
                        if (!Current(pending)) { dropped++; continue; }
                        budget = CancellationTokenSource.CreateLinkedTokenSource(stop.Token);
                        budget.CancelAfter(TimeSpan.FromSeconds(30)); sending = budget; sendingKind = pending.Route.Kind;
                    }
                    try {
                        var error = await transport.CheckChannel(pending.Route, budget.Token);
                        budget.Token.ThrowIfCancellationRequested();
                        lock (gate) {
                            if (!Current(pending)) { dropped++; continue; }
                            if (error != null) { failed++; last = GameChat.Label(pending.Message.Kind) + " 전달 실패 · " + error; continue; }
                        }
                        await transport.Send(pending.Route, pending.Message, budget.Token);
                        lock (gate) sent++;
                    }
                    catch (OperationCanceledException) {
                        lock (gate) { if (Current(pending)) { failed++; last = "채팅 전송 시간 초과 · 중복 방지를 위해 해당 메시지는 재전송하지 않습니다."; } else dropped++; }
                    }
                    catch (Exception e) {
                        lock (gate) { failed++; last = "채팅 전달 실패 · " + e.GetType().Name + " · 해당 메시지는 재전송하지 않습니다."; }
                    }
                    finally { lock (gate) { sending = null; sendingKind = ""; } budget.Dispose(); }
                }
            }
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
    }

    public void Dispose()
    {
        lock (gate) { if (disposed) return; disposed = true; watching = false; queue.Clear(); sending?.Cancel(); }
        stop.Cancel();
    }
}
