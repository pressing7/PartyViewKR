using System.Text;
using Discord;

namespace PartyViewKR;

internal static class DiscordChatFormatting
{
    internal static uint Color(GameChatKind kind) => kind switch {
        GameChatKind.Party => 0x4D9CFF, GameChatKind.Tell => 0xFF77B7, GameChatKind.Alliance => 0xFFA34D,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    // Split before Discord's description limit without cutting a Unicode scalar
    // or an escape sequence. Every part is a separate persistent bot message.
    internal static Embed[] Render(GameChatMessage message)
    {
        var name = string.IsNullOrWhiteSpace(message.Name) ? "이름 미상" : message.Name;
        var world = string.IsNullOrWhiteSpace(message.World) ? "서버 미상" : message.World;
        var chunks = new List<string>(); var buffer = new StringBuilder();
        foreach (var rune in (name + "@" + world + " : " + message.Text).EnumerateRunes()) {
            var token = rune.Value == '@' ? "@\u200b" : rune.IsAscii && "\\`*_~|>[]()#".Contains((char)rune.Value)
                ? "\\" + rune : rune.ToString();
            if (buffer.Length + token.Length > 4000) { chunks.Add(buffer.ToString()); buffer.Clear(); }
            buffer.Append(token);
        }
        if (buffer.Length > 0) chunks.Add(buffer.ToString());
        return chunks.Select((text, i) => new EmbedBuilder().WithColor(Color(message.Kind))
            .WithTitle(GameChat.Label(message.Kind) + (chunks.Count > 1 ? $" · {i + 1}/{chunks.Count}" : ""))
            .WithDescription(text).WithTimestamp(message.At).Build()).ToArray();
    }
}
