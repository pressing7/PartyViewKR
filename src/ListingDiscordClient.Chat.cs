using Discord;
using Discord.WebSocket;

namespace PartyViewKR;

internal sealed partial class ListingDiscordClient
{
    private ListingDiscordChat? chat;
    internal string ChatStatus => chat?.Status ?? "채팅 연결 준비 중입니다.";
    internal bool ChatEditable => chat?.Editable == true;
    internal DiscordChatSettings? ChatSettings => chat?.Settings;
    internal Task<string> ConfigureChat(GameChatKind kind, bool? enabled, bool? mention) =>
        chat?.ConfigureLocal(kind, enabled, mention, CancellationToken.None) ?? Task.FromResult("채팅 연결 준비 중입니다.");
    internal void ObserveChat(bool enabled, ulong self) => chat?.Observe(enabled, self);
    internal bool WantsChat(GameChatKind kind) => chat?.Wants(kind) == true;
    internal bool ReceiveChat(ulong self, GameChatMessage message) => chat?.Receive(self, message) == true;

    internal static SlashCommandProperties ChatCommand()
    {
        static SlashCommandOptionBuilder Kind() => new SlashCommandOptionBuilder()
            .WithName("종류").WithDescription("전달할 게임 채팅 종류").WithType(ApplicationCommandOptionType.String).WithRequired(true)
            .AddChoice("파티", "party").AddChoice("귓속말", "tell").AddChoice("연합파티", "alliance");
        static SlashCommandOptionBuilder Mention(string name, bool required) => new SlashCommandOptionBuilder()
            .WithName(name).WithDescription("everyone 멘션 사용 여부").WithType(ApplicationCommandOptionType.String).WithRequired(required)
            .AddChoice("끄기", "off").AddChoice("everyone", "everyone");
        static SlashCommandOptionBuilder Action(string name, string description) => new SlashCommandOptionBuilder()
            .WithName(name).WithDescription(description).WithType(ApplicationCommandOptionType.SubCommand);
        return new SlashCommandBuilder().WithName("pvkr-chat").WithDescription("게임 채팅 수신 채널과 멘션 설정")
            .WithIntegrationTypes(ApplicationIntegrationType.GuildInstall).WithContextTypes(InteractionContextType.Guild)
            .AddOption(Action("설정", "채팅 종류별 수신 채널 지정").AddOption(Kind())
                .AddOption(new SlashCommandOptionBuilder().WithName("채널").WithDescription("채팅을 받을 서버 텍스트 채널")
                    .WithType(ApplicationCommandOptionType.Channel).AddChannelType(ChannelType.Text).WithRequired(true))
                .AddOption(Mention("멘션", false)))
            .AddOption(Action("멘션", "기존 수신 채널의 멘션 변경").AddOption(Kind()).AddOption(Mention("설정", true)))
            .AddOption(Action("해제", "선택한 종류의 채팅 전달 중단").AddOption(Kind()))
            .AddOption(Action("상태", "수신 채널과 멘션 설정 및 전송 상태 확인")).Build();
    }

    private static DiscordChatCommand? ReadChatCommand(SocketSlashCommand command)
    {
        var actions = command.Data.Options?.ToArray() ?? [];
        if (actions.Length != 1 || actions[0].Type != ApplicationCommandOptionType.SubCommand) return null;
        var action = actions[0]; var options = action.Options?.ToArray() ?? [];
        var names = options.Select(o => o.Name).ToArray();
        if (names.Distinct(StringComparer.Ordinal).Count() != names.Length) return null;
        var allowed = action.Name switch {
            "설정" => new[] { "종류", "채널", "멘션" }, "멘션" => ["종류", "설정"], "해제" => ["종류"], "상태" => [], _ => null
        };
        if (allowed == null || names.Any(n => !allowed.Contains(n))) return null;
        if (action.Name == "상태") return new("상태");
        var kind = options.FirstOrDefault(o => o.Name == "종류");
        if (kind?.Type != ApplicationCommandOptionType.String || kind.Value is not string key || !GameChat.TryKind(key, out _)) return null;
        ulong channel = 0;
        if (action.Name == "설정") {
            var option = options.FirstOrDefault(o => o.Name == "채널");
            if (option?.Type != ApplicationCommandOptionType.Channel || option.Value is not IChannel value) return null;
            channel = value.Id;
        }
        var mention = options.FirstOrDefault(o => o.Name == (action.Name == "멘션" ? "설정" : "멘션"));
        bool? everyone = null;
        if (mention != null) {
            if (mention.Type != ApplicationCommandOptionType.String || mention.Value is not string flag || flag is not ("off" or "everyone")) return null;
            everyone = flag == "everyone";
        }
        if (action.Name == "멘션" && everyone == null) return null;
        return new(action.Name, key, channel, everyone);
    }

    private sealed class ChatTransport(ListingDiscordClient client) : IDiscordChatTransport
    {
        public bool Connected => client.Connected;
        private SocketTextChannel? Channel(DiscordChatRoute route) => client.socket.GetGuild(route.GuildId)?.GetTextChannel(route.ChannelId);
        public Task<string?> CheckChannel(DiscordChatRoute route, CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            if (!Connected) return Task.FromResult<string?>("봇이 연결될 때까지 기다려 주세요.");
            var channel = Channel(route);
            if (channel == null) return Task.FromResult<string?>("명령을 실행한 서버의 일반 텍스트 채널을 선택해 주세요.");
            var permissions = channel.Guild.CurrentUser.GetPermissions(channel);
            return Task.FromResult<string?>(!permissions.ViewChannel || !permissions.SendMessages || !permissions.EmbedLinks
                ? "봇에 채널 보기·메시지 보내기·링크 임베드 권한이 필요합니다."
                : route.MentionEveryone && !permissions.MentionEveryone ? "봇에 everyone 멘션 권한이 필요합니다." : null);
        }
        public async Task Send(DiscordChatRoute route, GameChatMessage message, CancellationToken cancellation)
        {
            var cards = DiscordChatFormatting.Render(message);
            for (var i = 0; i < cards.Length; i++) {
                var error = await CheckChannel(route, cancellation);
                if (error != null) throw new InvalidOperationException("Chat channel unavailable");
                cancellation.ThrowIfCancellationRequested();
                var mention = route.MentionEveryone && i == 0;
                var sent = await Channel(route)!.SendMessageAsync(mention ? "@everyone" : null, embed: cards[i],
                    allowedMentions: new(mention ? AllowedMentionTypes.Everyone : AllowedMentionTypes.None),
                    options: new RequestOptions { CancelToken = cancellation, Timeout = 15000, RetryMode = RetryMode.RetryRatelimit });
                if (mention && !sent.MentionedEveryone) throw new InvalidOperationException("Discord did not confirm the chat mention");
            }
        }
    }
}
