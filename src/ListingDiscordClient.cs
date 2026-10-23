using Discord;
using Discord.Net;
using Discord.WebSocket;

namespace PartyViewKR;

internal sealed class ListingDiscordClient : IDisposable
{
    private readonly CancellationTokenSource stop = new();
    private readonly DiscordSocketClient socket;
    private readonly Task lifetime;
    private readonly TaskCompletionSource gatewayReady = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private ListingDiscordSession? session;
    private const string ConnectedStatus = "연결됨 · 봇과의 DM 또는 초대한 서버에서 /pvkr를 입력하세요.";
    private string status = "디스코드에 연결하는 중입니다.";
    private string inviteUrl = "";
    private string emojiStatus = "";
    private int disposed, ready, running = 1;
    internal ListingWebBridge Bridge { get; }
    internal bool Running => Volatile.Read(ref running) != 0 && Volatile.Read(ref disposed) == 0;
    internal bool Connected => Running && Volatile.Read(ref ready) != 0 && socket.ConnectionState == ConnectionState.Connected;
    internal string Status => Volatile.Read(ref status);
    internal string InviteUrl => Volatile.Read(ref inviteUrl);
    internal Task Completion => lifetime;

    internal ListingDiscordClient(ListingWebBridge bridge, string token, DiscordSocketConfig? config = null)
    {
        Bridge = bridge;
        socket = new(config ?? new DiscordSocketConfig {
            GatewayIntents = GatewayIntents.Guilds, AlwaysDownloadUsers = false,
            AlwaysDownloadDefaultStickers = false, MessageCacheSize = 0, LogLevel = LogSeverity.Critical
        });
        socket.Ready += Ready;
        socket.Connected += Reconnected;
        socket.Disconnected += Disconnected;
        socket.InteractionCreated += Interaction;
        lifetime = Task.Run(() => Run(token));
    }
    private async Task Run(string token)
    {
        var stage = "봇 로그인";
        try
        {
            await socket.LoginAsync(TokenType.Bot, token); token = "";
            stop.Token.ThrowIfCancellationRequested();
            stage = "봇 정보 확인";
            status = "봇 정보를 확인하는 중입니다.";
            var application = await socket.GetApplicationInfoAsync(Options());
            if (application.Team != null || application.Owner == null || application.Owner.IsBot) {
                status = "개인 계정이 소유한 봇을 사용해 주세요. 팀 소유 앱은 지원하지 않습니다."; return;
            }
            var icons = new ListingDiscordIcons();
            session = new(Bridge, application.Id, application.Owner.Id, stop.Token, iconCatalog: icons);
            inviteUrl = $"https://discord.com/oauth2/authorize?client_id={application.Id}&scope=bot%20applications.commands&permissions=0";
            stage = "Gateway 연결 준비";
            status = "디스코드 Gateway의 연결 준비를 기다리는 중입니다.";
            await socket.StartAsync();
            // Login/GetApplicationInfo do not set the socket REST client's application ID.
            // Discord.Net fills it from READY. Never register commands before that event.
            await gatewayReady.Task.WaitAsync(TimeSpan.FromSeconds(30), stop.Token);
            stop.Token.ThrowIfCancellationRequested();
            stage = "/pvkr 명령어 등록";
            status = "/pvkr 명령어를 등록하는 중입니다.";
            // Create-by-name updates only /pvkr; never bulk-overwrite other commands.
            await ((IDiscordClient)socket.Rest).CreateGlobalApplicationCommand(new SlashCommandBuilder()
                .WithName("pvkr").WithDescription("내 파티 모집글 조회·수정·갱신")
                .WithIntegrationTypes(ApplicationIntegrationType.GuildInstall)
                .WithContextTypes(InteractionContextType.Guild, InteractionContextType.BotDm).Build(), Options());
            stop.Token.ThrowIfCancellationRequested();
            Volatile.Write(ref ready, 1);
            status = ConnectedStatus;
            // Interactions already work while the first upload runs. This awaits
            // only on our background lifetime task, never the Gateway/UI thread.
            await PrepareEmojis(icons);
            await Task.Delay(Timeout.Infinite, stop.Token);
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
        catch (HttpException e) when (e.HttpCode == System.Net.HttpStatusCode.Unauthorized) {
            status = "봇 토큰이 올바르지 않습니다. Developer Portal의 Bot 토큰을 확인해 주세요. " + FailureDetails(stage, e);
        }
        catch (Exception e) { status = "디스코드 연결을 시작하지 못했습니다. " + FailureDetails(stage, e); }
        finally {
            token = ""; Volatile.Write(ref ready, 0); Volatile.Write(ref running, 0);
            Bridge.Stop(); stop.Cancel(); session?.ClearIfIdle();
            try { await socket.StopAsync(); } catch (Exception) { }
            try { await socket.LogoutAsync(); } catch (Exception) { }
            socket.Dispose();
        }
    }
    private RequestOptions Options() => new() { CancelToken = stop.Token, Timeout = 15000, RetryMode = RetryMode.RetryRatelimit };
    private async Task PrepareEmojis(ListingDiscordIcons icons)
    {
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(stop.Token);
        budget.CancelAfter(TimeSpan.FromMinutes(5));
        RequestOptions EmojiOptions(CancellationToken ct) => new() { CancelToken = ct, Timeout = 15000, RetryMode = RetryMode.RetryRatelimit };
        var reused = -1; var uploaded = 0; var renamed = 0;
        void Progress(int done, int total)
        {
            if (reused < 0) reused = done;
            SetEmojiStatus($"직업 이모지 준비 중 · {done}/{total} · 기존 {reused}개 재사용 · 새 업로드 {uploaded}개");
        }
        try {
            SetEmojiStatus("기존 직업 이모지 확인 중입니다.");
            await ListingDiscordEmojiAssets.Sync(icons,
                ct => socket.Rest.GetApplicationEmotesAsync(EmojiOptions(ct)),
                async (name, stream, ct) => {
                    SetEmojiStatus($"새 직업 이모지 업로드 중 · {icons.Count}/{ListingDiscordEmojiAssets.All.Count} · 기존 {reused}개 재사용");
                    var emote = await socket.Rest.CreateApplicationEmoteAsync(name, new Image(stream), EmojiOptions(ct));
                    uploaded++; return emote;
                },
                async (id, name, ct) => {
                    SetEmojiStatus($"기존 직업 이모지 이름 변경 중 · {icons.Count}/{ListingDiscordEmojiAssets.All.Count}");
                    var emote = await socket.Rest.ModifyApplicationEmoteAsync(id, p => p.Name = name, EmojiOptions(ct));
                    renamed++; return emote;
                },
                Progress, budget.Token);
            SetEmojiStatus($"직업 이모지 준비 완료 · {icons.Count}/{ListingDiscordEmojiAssets.All.Count} · 기존 {reused}개 재사용 · 새 업로드 {uploaded}개" +
                (renamed > 0 ? $" · 이름 변경 {renamed}개" : ""));
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
        catch (Exception error) {
            SetEmojiStatus($"직업 이모지 일부 미준비 · {icons.Count}/{ListingDiscordEmojiAssets.All.Count}. " +
                "준비되지 않은 이미지는 기본 기호로 표시합니다. 연결을 다시 켜면 이어서 준비합니다. " + FailureDetails("이모지 준비", error));
        }
    }
    private string ConnectedMessage => ConnectedStatus + (emojiStatus.Length > 0 ? "\n" + emojiStatus : "");
    private void SetEmojiStatus(string text)
    {
        Volatile.Write(ref emojiStatus, text);
        if (Connected && !stop.IsCancellationRequested) status = ConnectedMessage;
    }
    internal static string FailureDetails(string stage, Exception error)
    {
        // SDK messages/requests can include credentials: expose only codes and the type.
        var detail = error switch {
            HttpException http => $"HTTP {(int)http.HttpCode}" + (http.DiscordCode is { } code ? $" / Discord {(int)code}" : ""),
            WebSocketClosedException closed => $"Gateway {closed.CloseCode}",
            _ => error.GetType().Name
        };
        return $"단계: {stage} · {detail}";
    }
    private Task Ready()
    {
        gatewayReady.TrySetResult();
        if (Volatile.Read(ref ready) != 0 && !stop.IsCancellationRequested) status = ConnectedMessage;
        return Task.CompletedTask;
    }
    private Task Disconnected(Exception exception)
    {
        if (!stop.IsCancellationRequested) status = "디스코드 연결이 끊겼습니다. 자동으로 다시 연결하는 중입니다. " + FailureDetails("Gateway 연결", exception);
        return Task.CompletedTask;
    }
    private Task Reconnected()
    {
        // A resumed Gateway session need not emit READY again.
        if (Volatile.Read(ref ready) != 0 && !stop.IsCancellationRequested)
            status = ConnectedMessage;
        return Task.CompletedTask;
    }
    private Task Interaction(SocketInteraction interaction)
    {
        var active = session;
        if (!Connected || active == null) return Task.CompletedTask;
        DiscordInput? input = interaction switch {
            SocketSlashCommand command when command.Data.Name == "pvkr" =>
                new(command.Id, command.ApplicationId, command.User.Id, command.Data.Name, "", [], []),
            SocketMessageComponent component when component.Data.CustomId.StartsWith("pvkr:", StringComparison.Ordinal) =>
                // Buttons omit values; Discord.Net exposes null, unlike select menus.
                new(component.Id, component.ApplicationId, component.User.Id, "", component.Data.CustomId, component.Data.Values?.ToArray() ?? [], []),
            SocketModal modal when modal.Data.CustomId.StartsWith("pvkr:", StringComparison.Ordinal) =>
                ReadModal(modal),
            _ => null
        };
        // Never block Discord's Gateway dispatch/heartbeat waiting for the game.
        if (input != null) _ = active.Process(input, new Response(interaction, stop.Token));
        return Task.CompletedTask;
    }
    private static DiscordInput ReadModal(SocketModal modal)
    {
        var result = new DiscordInput(modal.Id, modal.ApplicationId, modal.User.Id, "", modal.Data.CustomId, [], []);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var component in modal.Data.Components) {
            if (!seen.Add(component.CustomId)) { result.Fields["invalid"] = ""; continue; }
            switch (component.Type) {
                case ComponentType.CheckboxGroup:
                    if (component.Values != null) result.Choices[component.CustomId] = component.Values.ToArray();
                    else result.Fields["invalid"] = "";
                    break;
                case ComponentType.Checkbox:
                    if (component.BoolValue is { } boolean) result.Checks[component.CustomId] = boolean;
                    else result.Fields["invalid"] = "";
                    break;
                case ComponentType.RadioGroup:
                case ComponentType.TextInput:
                    result.Fields[component.CustomId] = component.Value ?? ""; break;
                default: result.Fields["invalid"] = ""; break;
            }
        }
        return result;
    }
    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;
        Bridge.Stop(); stop.Cancel(); session?.ClearIfIdle();
        // Network teardown is async; do not stall Dalamud's framework thread.
        _ = lifetime.ContinueWith(_ => stop.Dispose(), TaskScheduler.Default);
    }
    private sealed class Response(SocketInteraction interaction, CancellationToken cancellation) : IDiscordResponse
    {
        private RequestOptions Options(bool ack = false) => new() {
            CancelToken = cancellation, Timeout = ack ? 2000 : 10000,
            RetryMode = ack ? RetryMode.AlwaysFail : RetryMode.RetryRatelimit
        };
        public Task Defer() => interaction.DeferAsync(ephemeral: interaction is SocketSlashCommand, options: Options(true));
        public Task Modal(Modal modal) => interaction.RespondWithModalAsync(modal, Options(true));
        public async Task Screen(DiscordScreen screen, bool replace, bool deferred)
        {
            void Set(MessageProperties p) {
                p.Content = null!; p.Embeds = Array.Empty<Embed>(); p.Components = screen.Components;
                p.Flags = MessageFlags.ComponentsV2; p.AllowedMentions = AllowedMentions.None;
            }
            if (deferred) await interaction.ModifyOriginalResponseAsync(Set, Options());
            else if (replace && interaction is SocketMessageComponent component) await component.UpdateAsync(Set, Options(true));
            else if (replace && interaction is SocketModal { Message: not null } modal) await modal.UpdateAsync(Set, Options(true));
            else await interaction.RespondAsync(ephemeral: true, allowedMentions: AllowedMentions.None,
                components: screen.Components, flags: MessageFlags.ComponentsV2, options: Options(true));
        }
    }
}
