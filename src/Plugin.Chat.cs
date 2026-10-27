using Dalamud.Plugin.Services;

namespace PartyViewKR;

public sealed partial class Plugin
{
    private string chatCaptureError = "";
    private GameChatCapture? chatCapture;
    private readonly RuntimeRows chatRows = new();
    private readonly GameChatSession chatSession = new();
    private Task? chatSettingTask;
    private string chatSettingStatus = "";
    internal string DiscordChatStatus => (discordClient?.ChatStatus ?? "") +
        (chatCaptureError.Length == 0 ? "" : "\n" + chatCaptureError) +
        (chatSettingStatus.Length == 0 ? "" : "\n" + chatSettingStatus);
    internal bool CanConfigureDiscordChat => chatSettingTask?.IsCompleted != false &&
        (discordClient == null || !discordClient.Running || discordClient.ChatEditable);
    internal DiscordChatSettings DiscordChatSettings
    {
        get {
            if (discordClient?.Running == true && discordClient.ChatSettings is { } active) return active;
            var saved = (PluginInterface.GetPluginConfig() as RelayConfiguration)?.DiscordChat ?? new();
            return saved.ForApplication(saved.ApplicationId);
        }
    }

    private void UpdateDiscordChat()
    {
        try {
            chatSession.Observe(ClientState.IsLoggedIn, PlayerState.IsLoaded,
                PlayerState.IsLoaded ? PlayerState.ContentId : 0, () => {
                    var id = RuntimeRows.RowId(RuntimeRows.PlayerWorld, PlayerState);
                    return id == 0 ? "" : chatRows.World(id);
                });
        }
        catch (Exception e) { chatCaptureError = "채팅 발신 서버를 확인하지 못했습니다. · " + e.GetType().Name; }
        discordClient?.ObserveChat(ClientState.IsLoggedIn, chatSession.SelfId);
    }

    private void InitializeChatCapture()
    {
        try {
            chatCapture = new(ChatGui, typeof(IChatGui), WantsCapturedChat, ReceiveCapturedChat, chatRows.World,
                error => {
                    chatCaptureError = error.Length == 0 ? "" : "게임 채팅을 읽지 못했습니다. · " + error;
                    if (error.Length != 0) discordClient?.ChatSending.InvalidateReply();
                },
                () => chatSession.HomeWorld);
        }
        catch (Exception e) { chatCaptureError = "게임 채팅 수신을 시작하지 못했습니다. · " + e.GetType().Name; }
    }
    private bool WantsCapturedChat(GameChatKind kind)
    {
        if (disposed) return false;
        UpdateDiscordChat(); return discordClient?.WantsChat(kind) == true || kind == GameChatKind.Tell && discordClient?.Connected == true;
    }
    private void ReceiveCapturedChat(GameChatMessage message)
    {
        discordClient?.ChatSending.ObserveTell(chatSession.SelfId, message);
        discordClient?.ReceiveChat(chatSession.SelfId, message);
    }

    internal void SetDiscordChat(GameChatKind kind, bool? enabled = null, bool? mention = null, bool? sound = null)
    {
        if (disposed || !CanConfigureDiscordChat) return;
        chatSettingTask = ChangeDiscordChat(kind, enabled, mention, sound);
    }

    private async Task ChangeDiscordChat(GameChatKind kind, bool? enabled, bool? mention, bool? sound)
    {
        try {
            chatSettingStatus = "채팅 설정 저장 중입니다.";
            var client = discordClient;
            if (client?.Running == true) {
                chatSettingStatus = await client.ConfigureChat(kind, enabled, mention, sound);
                return;
            }
            var config = PluginInterface.GetPluginConfig() as RelayConfiguration ?? new();
            var saved = config.DiscordChat ?? new();
            saved = saved.ForApplication(saved.ApplicationId);
            if (!saved.Routes.Any(r => r.Kind == GameChat.Key(kind))) {
                chatSettingStatus = "먼저 /pvkr-chat 설정으로 수신 채널을 지정해 주세요."; return;
            }
            config.DiscordChat = saved.WithOptions(kind, enabled, mention, sound);
            PluginInterface.SavePluginConfig(config);
            chatSettingStatus = "채팅 설정을 저장했습니다. 다음 디스코드 연결부터 적용합니다.";
        }
        catch (OperationCanceledException) { chatSettingStatus = "연결이 종료되어 채팅 설정 저장을 중단했습니다."; }
        catch (Exception e) { chatSettingStatus = "채팅 설정을 저장하지 못했습니다. · " + e.GetType().Name; }
    }

    private Task SaveDiscordChatSettings(DiscordChatSettings settings, CancellationToken cancellation) =>
        Framework.RunOnFrameworkThread(() => {
            cancellation.ThrowIfCancellationRequested();
            if (disposed) throw new OperationCanceledException(cancellation);
            var config = PluginInterface.GetPluginConfig() as RelayConfiguration ?? new();
            config.DiscordChat = settings with { Routes = settings.Routes.ToArray() };
            PluginInterface.SavePluginConfig(config);
        });
}
