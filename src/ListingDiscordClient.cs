using Discord;
using Discord.Net;
using Discord.WebSocket;

namespace PartyViewKR;

internal sealed class ListingDiscordClient : IDisposable
{
    private readonly CancellationTokenSource stop = new();
    private readonly DiscordSocketClient socket;
    private readonly Task lifetime;
    private ListingDiscordSession? session;
    private string status = "디스코드에 연결하는 중입니다.";
    private string inviteUrl = "";
    private int disposed, ready, running = 1;
    internal ListingWebBridge Bridge { get; }
    internal bool Running => Volatile.Read(ref running) != 0 && Volatile.Read(ref disposed) == 0;
    internal bool Connected => Running && Volatile.Read(ref ready) != 0 && socket.ConnectionState == ConnectionState.Connected;
    internal string Status => Volatile.Read(ref status);
    internal string InviteUrl => Volatile.Read(ref inviteUrl);

    internal ListingDiscordClient(ListingWebBridge bridge, string token)
    {
        Bridge = bridge;
        socket = new(new DiscordSocketConfig {
            GatewayIntents = GatewayIntents.Guilds, AlwaysDownloadUsers = false,
            MessageCacheSize = 0, LogLevel = LogSeverity.Critical
        });
        socket.Ready += Ready;
        socket.Connected += Reconnected;
        socket.Disconnected += Disconnected;
        socket.InteractionCreated += Interaction;
        lifetime = Task.Run(() => Run(token));
    }
    private async Task Run(string token)
    {
        try
        {
            await socket.LoginAsync(TokenType.Bot, token); token = "";
            stop.Token.ThrowIfCancellationRequested();
            var application = await socket.GetApplicationInfoAsync(Options());
            if (application.Team != null || application.Owner == null || application.Owner.IsBot) {
                status = "개인 계정이 소유한 봇을 사용해 주세요. 팀 소유 앱은 지원하지 않습니다."; return;
            }
            session = new(Bridge, application.Id, application.Owner.Id, stop.Token);
            inviteUrl = $"https://discord.com/oauth2/authorize?client_id={application.Id}&scope=bot%20applications.commands&permissions=0";
            // Create-by-name updates only /pvkr; never bulk-overwrite other commands.
            await ((IDiscordClient)socket.Rest).CreateGlobalApplicationCommand(new SlashCommandBuilder()
                .WithName("pvkr").WithDescription("내 파티 모집글 조회·수정·갱신")
                .WithIntegrationTypes(ApplicationIntegrationType.GuildInstall)
                .WithContextTypes(InteractionContextType.Guild, InteractionContextType.BotDm).Build(), Options());
            await socket.StartAsync();
            await Task.Delay(Timeout.Infinite, stop.Token);
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
        catch (HttpException e) when (e.HttpCode == System.Net.HttpStatusCode.Unauthorized) {
            status = "봇 토큰이 올바르지 않습니다. Developer Portal의 Bot 토큰을 확인해 주세요.";
        }
        catch (Exception) { status = "디스코드 연결을 시작하지 못했습니다. 인터넷 연결과 봇 토큰을 확인한 후 다시 연결해 주세요."; }
        finally {
            token = ""; Volatile.Write(ref ready, 0); Volatile.Write(ref running, 0);
            Bridge.Stop(); stop.Cancel(); session?.ClearIfIdle();
            try { await socket.StopAsync(); } catch (Exception) { }
            try { await socket.LogoutAsync(); } catch (Exception) { }
            socket.Dispose();
        }
    }
    private RequestOptions Options() => new() { CancelToken = stop.Token, Timeout = 15000, RetryMode = RetryMode.RetryRatelimit };
    private Task Ready()
    {
        Volatile.Write(ref ready, 1);
        status = "연결됨 · 봇과의 DM 또는 초대한 서버에서 /pvkr를 입력하세요.";
        return Task.CompletedTask;
    }
    private Task Disconnected(Exception exception)
    {
        if (!stop.IsCancellationRequested) status = "디스코드 연결이 끊겼습니다. 자동으로 다시 연결하는 중입니다.";
        return Task.CompletedTask;
    }
    private Task Reconnected()
    {
        // A resumed Gateway session need not emit READY again.
        if (Volatile.Read(ref ready) != 0 && !stop.IsCancellationRequested)
            status = "연결됨 · 봇과의 DM 또는 초대한 서버에서 /pvkr를 입력하세요.";
        return Task.CompletedTask;
    }
    private Task Interaction(SocketInteraction interaction)
    {
        var active = session;
        if (!Running || active == null) return Task.CompletedTask;
        DiscordInput? input = interaction switch {
            SocketSlashCommand command when command.Data.Name == "pvkr" =>
                new(command.Id, command.ApplicationId, command.User.Id, command.Data.Name, "", [], []),
            SocketMessageComponent component when component.Data.CustomId.StartsWith("pvkr:", StringComparison.Ordinal) =>
                new(component.Id, component.ApplicationId, component.User.Id, "", component.Data.CustomId, component.Data.Values.ToArray(), []),
            SocketModal modal when modal.Data.CustomId.StartsWith("pvkr:", StringComparison.Ordinal) =>
                new(modal.Id, modal.ApplicationId, modal.User.Id, "", modal.Data.CustomId, [], modal.Data.Components.ToDictionary(c => c.CustomId, c => c.Value)),
            _ => null
        };
        // Never block Discord's Gateway dispatch/heartbeat waiting for the game.
        if (input != null) _ = active.Process(input, new Response(interaction, stop.Token));
        return Task.CompletedTask;
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
            void Set(MessageProperties p) { p.Content = screen.Text; p.Components = screen.Components; p.AllowedMentions = AllowedMentions.None; }
            if (deferred) await interaction.ModifyOriginalResponseAsync(Set, Options());
            else if (replace && interaction is SocketMessageComponent component) await component.UpdateAsync(Set, Options(true));
            else if (replace && interaction is SocketModal { Message: not null } modal) await modal.UpdateAsync(Set, Options(true));
            else await interaction.RespondAsync(screen.Text, ephemeral: true, allowedMentions: AllowedMentions.None,
                components: screen.Components, options: Options(true));
        }
    }
}
