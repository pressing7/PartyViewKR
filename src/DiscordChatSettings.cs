namespace PartyViewKR;

// Only routing preferences are persisted. Chat contents never enter plugin configuration.
public sealed record DiscordChatRoute
{
    public string Kind { get; init; } = "";
    public ulong GuildId { get; init; }
    public ulong ChannelId { get; init; }
    public bool Enabled { get; init; } = true;
    public bool MentionEveryone { get; init; }
    public bool MentionOnSound { get; init; }
    internal string MentionLabel => MentionEveryone ? "항상 @everyone" : MentionOnSound && Kind is "party" or "alliance" ? "효과음만 @everyone" : "끄기";
    internal bool ShouldMention(GameChatMessage message) => MentionEveryone ||
        MentionOnSound && message.Kind is GameChatKind.Party or GameChatKind.Alliance && GameChat.HasSoundEffect(message.Text);
}

public sealed record DiscordChatSettings
{
    public ulong ApplicationId { get; init; }
    public DiscordChatRoute[] Routes { get; init; } = [];

    internal DiscordChatSettings ForApplication(ulong app) => new() {
        ApplicationId = app,
        Routes = ApplicationId == app && app != 0
            ? (Routes ?? []).Where(r => r != null && GameChat.TryKind(r.Kind, out _) && r.GuildId != 0 && r.ChannelId != 0)
                .GroupBy(r => r.Kind).Select(g => g.Last()).OrderBy(r => r.Kind, StringComparer.Ordinal).ToArray() : []
    };

    internal DiscordChatSettings WithOptions(GameChatKind kind, bool? enabled, bool? mention, bool? sound = null) => this with {
        Routes = Routes.Select(r => r.Kind == GameChat.Key(kind)
            ? r with { Enabled = enabled ?? r.Enabled, MentionEveryone = mention ?? r.MentionEveryone,
                MentionOnSound = kind != GameChatKind.Tell && (sound ?? (mention.HasValue ? false : r.MentionOnSound)) } : r).ToArray()
    };
}

internal enum GameChatKind { Party, Tell, Alliance }
internal sealed record GameChatMessage(GameChatKind Kind, string Name, string World, string Text, DateTimeOffset At);
internal sealed record DiscordChatCommand(string Action, string Kind = "", ulong ChannelId = 0, bool? MentionEveryone = null, bool? MentionOnSound = null);

internal static class GameChat
{
    internal static bool HasSoundEffect(string text)
    {
        // Exact tags only: 0, 17, padded numbers and incomplete tags are not sounds.
        for (var i = 1; i <= 16; i++)
            if (text.Contains($"<se.{i}>", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }
    // API 15 XivChatType values; CrossParty belongs to the same user-facing category.
    internal static GameChatKind? FromType(ushort type) => type switch {
        14 or 32 => GameChatKind.Party, 13 => GameChatKind.Tell, 15 => GameChatKind.Alliance, _ => null
    };
    internal static string Key(GameChatKind kind) => kind switch {
        GameChatKind.Party => "party", GameChatKind.Tell => "tell", GameChatKind.Alliance => "alliance", _ => ""
    };
    internal static bool TryKind(string key, out GameChatKind kind)
    {
        kind = key switch { "party" => GameChatKind.Party, "tell" => GameChatKind.Tell, "alliance" => GameChatKind.Alliance, _ => (GameChatKind)(-1) };
        return Enum.IsDefined(kind);
    }
    internal static string Label(GameChatKind kind) => kind switch {
        GameChatKind.Party => "파티", GameChatKind.Tell => "귓속말", GameChatKind.Alliance => "연합파티", _ => "알 수 없음"
    };
}
