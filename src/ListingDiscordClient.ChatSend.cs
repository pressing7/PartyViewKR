using Discord;
using Discord.WebSocket;

namespace PartyViewKR;

internal sealed partial class ListingDiscordClient
{
    internal GameChatSendBridge ChatSending { get; } = new();

    internal static SlashCommandProperties SendChatCommand()
    {
        var command = new SlashCommandBuilder().WithName("pvkr-send").WithDescription("게임에 파티·연합파티·귓속말 보내기")
            .WithIntegrationTypes(ApplicationIntegrationType.GuildInstall)
            .WithContextTypes(InteractionContextType.Guild, InteractionContextType.BotDm);
        foreach (var name in new[] { "파티", "연합파티", "귓속말", "답장" })
            command.AddOption(new SlashCommandOptionBuilder().WithName(name).WithDescription(name == "답장"
                ? "최근 받은 귓속말의 상대를 확인하고 답장 작성" : name + " 메시지 작성")
                .WithType(ApplicationCommandOptionType.SubCommand));
        return command.Build();
    }

    private static GameChatSendKind? ReadSendChatCommand(SocketSlashCommand command)
    {
        var options = command.Data.Options?.ToArray() ?? [];
        if (options.Length != 1 || options[0].Type != ApplicationCommandOptionType.SubCommand || options[0].Options?.Any() == true) return null;
        return options[0].Name switch {
            "파티" => GameChatSendKind.Party, "연합파티" => GameChatSendKind.Alliance,
            "귓속말" => GameChatSendKind.Tell, "답장" => GameChatSendKind.Reply, _ => null
        };
    }
}
