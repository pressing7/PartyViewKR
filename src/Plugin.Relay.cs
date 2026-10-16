using Dalamud.Configuration;
using System.Security.Cryptography;

namespace PartyViewKR;

[Serializable]
public sealed class RelayConfiguration : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    public string ServerUrl { get; set; } = "";
    // No server key, connection key, recruitment data or PIN is saved.
}

public sealed partial class Plugin
{
    private ListingRelayClient? relayClient;
    private string relayStatus = "외부 연결이 꺼져 있습니다.";
    internal bool RelayEnabled => relayClient?.Running == true;
    internal bool RelayConnected => relayClient?.Connected == true;
    internal string RelayStatus => relayClient?.Status ?? relayStatus;
    internal string RelayLink => RelayEnabled ? relayClient!.ConnectionUrl : "";
    internal string RelayCode => RelayEnabled ? relayClient!.ConnectionCode : "";
    internal string RelaySavedAddress => (PluginInterface.GetPluginConfig() as RelayConfiguration)?.ServerUrl ?? "";
    internal static string GenerateRelayKey() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    internal bool StartRelay(string address, string key)
    {
        key = key.Trim().ToUpperInvariant();
        if (disposed || RelayEnabled) return false;
        if (!ListingRelayProtocol.TryServer(address, out var server)) { relayStatus = "https://로 시작하는 서버 주소만 입력해 주세요. 경로는 넣지 않습니다."; return false; }
        if (!ListingRelayProtocol.IsKey(key)) { relayStatus = "서버와 동일한 64자리 연결 키를 입력해 주세요."; return false; }
        StopWeb();
        try
        {
            PluginInterface.SavePluginConfig(new RelayConfiguration { ServerUrl = server.GetLeftPart(UriPartial.Authority) });
            var version = typeof(Plugin).Assembly.GetName().Version?.ToString() ?? "unknown";
            webHost ??= new WebHost(this);
            relayClient = new ListingRelayClient(new ListingWebBridge(version), server.AbsoluteUri, key);
            RequestRead(); return true;
        }
        catch (Exception) { relayStatus = "외부 연결을 시작하지 못했습니다. 설정을 확인해 주세요."; return false; }
    }
    internal void StopRelay()
    {
        var relay = Interlocked.Exchange(ref relayClient, null);
        relay?.Dispose();
        if (relay != null) { webHost?.CancelOwnedQuery(); webHost?.CancelOwnedRefresh(); }
        relayStatus = "외부 연결이 꺼져 있습니다.";
    }
}
