using System.Security.Cryptography;
using System.Text;

namespace PartyViewKR;

public sealed partial class Plugin
{
    private ListingDiscordClient? discordClient;
    private bool discordStartupChecked;
    private string discordStatus = "디스코드 연결이 꺼져 있습니다.";
    internal bool DiscordEnabled => discordClient?.Running == true;
    internal string DiscordStatus => discordClient?.Status ?? discordStatus;
    internal string DiscordNotificationStatus => discordClient?.NotificationStatus ?? "";
    internal string DiscordInviteUrl => discordClient?.InviteUrl ?? "";
    internal bool HasSavedDiscordToken => (PluginInterface.GetPluginConfig() as RelayConfiguration)?.HasSavedDiscordToken == true;
    internal bool RememberDiscordToken => (PluginInterface.GetPluginConfig() as RelayConfiguration)?.RememberDiscordToken ?? true;

    private void RestoreDiscordConnection()
    {
        if (discordStartupChecked || disposed) return;
        discordStartupChecked = true;
        if (AnyWebEnabled) return;
        try {
            var config = PluginInterface.GetPluginConfig() as RelayConfiguration;
            if (config?.ShouldAutoConnectDiscord == true) StartDiscord("", true);
        }
        catch (Exception) { discordStatus = "저장한 디스코드 설정을 읽지 못했습니다. 연결 설정을 확인해 주세요."; }
    }

    internal bool StartDiscord(string token, bool remember)
    {
        if (disposed || DiscordEnabled) return false;
        discordStartupChecked = true;
        if (discordClient != null) StopDiscord();
        var stage = "연결 설정 읽기";
        try
        {
            var config = PluginInterface.GetPluginConfig() as RelayConfiguration ?? new();
            if (!config.PrepareDiscordConnection(token, remember, ProtectDiscordToken, UnprotectDiscordToken, out token)) {
                discordStatus = "Developer Portal의 Bot 토큰을 입력해 주세요."; return false;
            }
            stage = "연결 설정 저장";
            PluginInterface.SavePluginConfig(config);
            stage = "기존 연결 종료";
            StopWeb();
            webHost ??= new WebHost(this);
            stage = "디스코드 클라이언트 생성";
            discordClient = new(new ListingWebBridge(typeof(Plugin).Assembly.GetName().Version?.ToString() ?? "unknown"), token,
                notificationTarget: config.ReadNotificationTarget(), saveNotificationTarget: SaveDiscordNotificationTarget,
                chatSettings: config.DiscordChat, saveChatSettings: SaveDiscordChatSettings);
            stage = "모집글 조회 시작";
            RequestRead(); return true;
        }
        catch (CryptographicException) { discordStatus = "저장한 토큰을 읽거나 암호화하지 못했습니다. 토큰을 다시 입력해 주세요."; }
        catch (FormatException) { discordStatus = "저장한 토큰을 읽지 못했습니다. 토큰을 다시 입력해 주세요."; }
        catch (Exception e) {
            StopDiscord();
            discordStatus = $"디스코드 연결을 시작하지 못했습니다. 단계: {stage} · {e.GetType().Name}";
        }
        return false;
    }
    private Task SaveDiscordNotificationTarget(DiscordNotificationTarget target, CancellationToken cancellation) =>
        Framework.RunOnFrameworkThread(() => {
            cancellation.ThrowIfCancellationRequested();
            if (disposed) throw new OperationCanceledException(cancellation);
            var config = PluginInterface.GetPluginConfig() as RelayConfiguration ?? new();
            config.WriteNotificationTarget(target); PluginInterface.SavePluginConfig(config);
        });

    private void UpdateDiscordCompletion() => discordClient?.ObserveCompletion(afk.Enabled,
        ClientState.IsLoggedIn && PlayerState.IsLoaded ? PlayerState.ContentId : 0,
        afk.CompletionRevision, afk.RecruitmentComplete || afk.SingleJobRecruitmentComplete,
        !afk.RecruitmentComplete && afk.SingleJobRecruitmentComplete);
    private static string ProtectDiscordToken(string token)
    {
        var plain = Encoding.UTF8.GetBytes(token);
        try { return Convert.ToBase64String(ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser)); }
        finally { CryptographicOperations.ZeroMemory(plain); }
    }
    private static string UnprotectDiscordToken(string encrypted)
    {
        var bytes = ProtectedData.Unprotect(Convert.FromBase64String(encrypted), null, DataProtectionScope.CurrentUser);
        try { return Encoding.UTF8.GetString(bytes); }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }
    private bool DisableDiscordAutoConnect()
    {
        discordStartupChecked = true;
        try {
            var config = PluginInterface.GetPluginConfig() as RelayConfiguration ?? new();
            if (config.DiscordAutoConnect) {
                config.DiscordAutoConnect = false;
                PluginInterface.SavePluginConfig(config);
            }
            return true;
        }
        catch (Exception) { discordStatus = "자동 연결 설정을 저장하지 못했습니다. 다시 시도해 주세요."; return false; }
    }
    internal void TurnOffDiscord()
    {
        StopDiscord();
        DisableDiscordAutoConnect();
    }
    internal void ForgetDiscordToken()
    {
        discordStartupChecked = true;
        try {
            var config = PluginInterface.GetPluginConfig() as RelayConfiguration ?? new();
            config.ForgetDiscordToken(); PluginInterface.SavePluginConfig(config);
        }
        catch (Exception) { discordStatus = "저장한 토큰을 지우지 못했습니다. 다시 시도해 주세요."; }
    }
    internal void StopDiscord()
    {
        var client = Interlocked.Exchange(ref discordClient, null);
        client?.Dispose();
        if (client != null) { webHost?.CancelOwnedQuery(); webHost?.CancelOwnedRefresh(); }
        discordStatus = "디스코드 연결이 꺼져 있습니다.";
    }
}
