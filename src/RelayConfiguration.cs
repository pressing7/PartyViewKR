using Dalamud.Configuration;

namespace PartyViewKR;

[Serializable]
public sealed class RelayConfiguration : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    public string ServerUrl { get; set; } = "";
    public string EncryptedDiscordToken { get; set; } = "";
    public bool RememberDiscordToken { get; set; } = true;
    // Existing saved tokens also reconnect when upgrading from 0.3.46.
    public bool DiscordAutoConnect { get; set; } = true;
    public ulong DiscordNotificationApplicationId { get; set; }
    public ulong DiscordNotificationGuildId { get; set; }
    public ulong DiscordNotificationChannelId { get; set; }
    public DiscordChatSettings? DiscordChat { get; set; }
    internal DiscordNotificationTarget ReadNotificationTarget() => new(DiscordNotificationApplicationId,
        DiscordNotificationGuildId, DiscordNotificationChannelId);
    internal void WriteNotificationTarget(DiscordNotificationTarget target)
    {
        DiscordNotificationApplicationId = target.ApplicationId;
        DiscordNotificationGuildId = target.GuildId; DiscordNotificationChannelId = target.ChannelId;
    }
    public bool AfkPeriodicEnabled { get; set; }
    public int AfkPeriodMinutes { get; set; } = 1;
    public bool AfkRetryEnabled { get; set; } = true;
    public int AfkRetrySeconds { get; set; } = 5;
    internal AfkSettings ReadAfkSettings() => new AfkSettings(AfkPeriodicEnabled, AfkPeriodMinutes,
        AfkRetryEnabled, AfkRetrySeconds).Normalize();
    internal void WriteAfkSettings(AfkSettings settings)
    {
        settings = settings.Normalize();
        AfkPeriodicEnabled = settings.PeriodicEnabled; AfkPeriodMinutes = settings.PeriodMinutes;
        AfkRetryEnabled = settings.RetryEnabled; AfkRetrySeconds = settings.RetrySeconds;
    }
    internal bool HasSavedDiscordToken => !string.IsNullOrEmpty(EncryptedDiscordToken);
    internal bool ShouldAutoConnectDiscord => DiscordAutoConnect && HasSavedDiscordToken;

    internal bool PrepareDiscordConnection(string input, bool remember,
        Func<string, string> protect, Func<string, string> unprotect, out string token)
    {
        token = input.Trim();
        var useSaved = token.Length == 0 && HasSavedDiscordToken;
        if (useSaved) token = unprotect(EncryptedDiscordToken);
        if (token.Length is < 32 or > 512 || token.Any(c => c is < '!' or > '~')) return false;
        // Calculate before changing any setting, and reuse existing ciphertext.
        // A failed decryption/encryption must not erase a working saved token.
        var encrypted = remember ? useSaved ? EncryptedDiscordToken : protect(token) : "";
        EncryptedDiscordToken = encrypted;
        RememberDiscordToken = remember;
        DiscordAutoConnect = remember;
        return true;
    }

    internal void ForgetDiscordToken()
    {
        EncryptedDiscordToken = "";
        RememberDiscordToken = false;
        DiscordAutoConnect = false;
    }
    // Only the DPAPI-protected token, connection/channel preferences and AFK timing are persisted.
    // No relay key, connection code, recruitment data or PIN is saved.
}
