using Dalamud.Bindings.ImGui;

namespace PartyViewKR;

internal sealed partial class PartyWindow
{
    private void DrawSettings()
    {
        DrawSection("채팅 전달");
        ImGui.TextWrapped("AFK 모드와 관계없이 전달합니다. 수신 채널은 서버에서 /pvkr-chat 설정으로 지정하세요.");
        if (plugin.DiscordChatStatus.Length > 0) ImGui.TextWrapped(plugin.DiscordChatStatus);
        var settings = plugin.DiscordChatSettings;
        if (!ImGui.BeginTable("discord-chat-settings", 4, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg)) return;
        ImGui.TableSetupColumn("채팅 종류", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("연합파티").X + 8);
        ImGui.TableSetupColumn("수신", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("수신").X + 8);
        ImGui.TableSetupColumn("@everyone 멘션", ImGuiTableColumnFlags.WidthStretch, 1);
        ImGui.TableSetupColumn("수신 채널", ImGuiTableColumnFlags.WidthStretch, 1.4f);
        ImGui.TableHeadersRow();
        foreach (var kind in Enum.GetValues<GameChatKind>()) {
            var route = settings.Routes.FirstOrDefault(r => r.Kind == GameChat.Key(kind));
            ImGui.PushID(GameChat.Key(kind));
            ImGui.TableNextRow(); Cell(GameChat.Label(kind));
            ImGui.TableNextColumn();
            ImGui.BeginDisabled(route == null || !plugin.CanConfigureDiscordChat);
            var enabled = route?.Enabled == true;
            if (ImGui.Checkbox("##receive", ref enabled)) plugin.SetDiscordChat(kind, enabled: enabled);
            ImGui.EndDisabled();
            ImGui.TableNextColumn();
            ImGui.BeginDisabled(route == null || !plugin.CanConfigureDiscordChat);
            var mention = route?.MentionEveryone == true ? 1 : route?.MentionOnSound == true && kind != GameChatKind.Tell ? 2 : 0;
            string[] choices = kind == GameChatKind.Tell ? ["끄기", "항상"] : ["끄기", "항상", "효과음만"];
            ImGui.SetNextItemWidth(-1);
            if (ImGui.Combo("##mention", ref mention, choices, choices.Length))
                plugin.SetDiscordChat(kind, mention: mention == 1, sound: mention == 2);
            ImGui.EndDisabled();
            Cell(route == null ? "미설정" : route.ChannelId.ToString());
            ImGui.PopID();
        }
        ImGui.EndTable();
    }
}
