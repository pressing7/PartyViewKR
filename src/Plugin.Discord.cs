using System.Security.Cryptography;
using System.Text;

namespace PartyViewKR;

public sealed partial class Plugin
{
    private ListingDiscordClient? discordClient;
    private string discordStatus = "디스코드 연결이 꺼져 있습니다.";
    internal bool DiscordEnabled => discordClient?.Running == true;
    internal string DiscordStatus => discordClient?.Status ?? discordStatus;
    internal string DiscordInviteUrl => discordClient?.InviteUrl ?? "";
    internal bool HasSavedDiscordToken => !string.IsNullOrEmpty((PluginInterface.GetPluginConfig() as RelayConfiguration)?.EncryptedDiscordToken);

    internal bool StartDiscord(string token, bool remember)
    {
        if (disposed || DiscordEnabled) return false;
        if (discordClient != null) StopDiscord();
        var stage = "연결 설정 읽기";
        try
        {
            var config = PluginInterface.GetPluginConfig() as RelayConfiguration ?? new();
            token = token.Trim();
            if (token.Length == 0 && config.EncryptedDiscordToken.Length > 0) {
                var bytes = ProtectedData.Unprotect(Convert.FromBase64String(config.EncryptedDiscordToken), null, DataProtectionScope.CurrentUser);
                try { token = Encoding.UTF8.GetString(bytes); } finally { CryptographicOperations.ZeroMemory(bytes); }
            }
            if (token.Length is < 32 or > 512 || token.Any(c => c is < '!' or > '~')) {
                discordStatus = "Developer Portal의 Bot 토큰을 입력해 주세요."; return false;
            }
            var plain = Encoding.UTF8.GetBytes(token);
            try { config.EncryptedDiscordToken = remember ? Convert.ToBase64String(ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser)) : ""; }
            finally { CryptographicOperations.ZeroMemory(plain); }
            stage = "연결 설정 저장";
            PluginInterface.SavePluginConfig(config);
            stage = "기존 연결 종료";
            StopWeb();
            webHost ??= new WebHost(this);
            stage = "디스코드 클라이언트 생성";
            discordClient = new(new ListingWebBridge(typeof(Plugin).Assembly.GetName().Version?.ToString() ?? "unknown"), token);
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
    internal void ForgetDiscordToken()
    {
        try {
            var config = PluginInterface.GetPluginConfig() as RelayConfiguration ?? new();
            config.EncryptedDiscordToken = ""; PluginInterface.SavePluginConfig(config);
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
