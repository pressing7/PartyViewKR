using Dalamud.Plugin.Services;

namespace PartyViewKR;

public sealed partial class Plugin
{
    private string chatCaptureError = "";
    private GameChatCapture? chatCapture;
    private readonly RuntimeRows chatRows = new();
    internal string DiscordChatStatus => (discordClient?.ChatStatus ?? "") +
        (chatCaptureError.Length == 0 ? "" : "\n" + chatCaptureError);

    private void UpdateDiscordChat() => discordClient?.ObserveChat(afk.Enabled && ClientState.IsLoggedIn &&
        (!PlayerState.IsLoaded || PlayerState.ContentId == afk.MonitoredSelfId), afk.MonitoredSelfId);

    private void InitializeChatCapture()
    {
        try {
            chatCapture = new(ChatGui, typeof(IChatGui), WantsCapturedChat, ReceiveCapturedChat, chatRows.World,
                error => chatCaptureError = error.Length == 0 ? "" : "게임 채팅을 읽지 못했습니다. · " + error);
        }
        catch (Exception e) { chatCaptureError = "게임 채팅 수신을 시작하지 못했습니다. · " + e.GetType().Name; }
    }
    private bool WantsCapturedChat(GameChatKind kind)
    {
        if (disposed) return false;
        UpdateDiscordChat(); return discordClient?.WantsChat(kind) == true;
    }
    private void ReceiveCapturedChat(GameChatMessage message) => discordClient?.ReceiveChat(afk.MonitoredSelfId, message);

    private Task SaveDiscordChatSettings(DiscordChatSettings settings, CancellationToken cancellation) =>
        Framework.RunOnFrameworkThread(() => {
            cancellation.ThrowIfCancellationRequested();
            if (disposed) throw new OperationCanceledException(cancellation);
            var config = PluginInterface.GetPluginConfig() as RelayConfiguration ?? new();
            config.DiscordChat = settings with { Routes = settings.Routes.ToArray() };
            PluginInterface.SavePluginConfig(config);
        });
}
