using Dalamud.Bindings.ImGui;

namespace PartyViewKR;

internal sealed partial class PartyWindow
{
    private string discordToken = "";
    private bool discordSettingsLoaded, rememberDiscordToken;

    private void DrawDiscord()
    {
        if (!discordSettingsLoaded)
        {
            rememberDiscordToken = plugin.RememberDiscordToken;
            discordSettingsLoaded = true;
        }

        DrawSection("연결 상태");
        ImGui.TextWrapped(plugin.DiscordStatus);

        DrawSection("개인 봇 연결");
        if (plugin.DiscordEnabled)
        {
            if (ImGui.Button("디스코드 연결 끄기")) plugin.TurnOffDiscord();
        }
        else
        {
            ImGui.TextUnformatted("봇 토큰");
            ImGui.SetNextItemWidth(-1);
            ImGui.InputTextWithHint("##discord-token", plugin.HasSavedDiscordToken ? "저장한 토큰 사용 (변경하려면 입력)" : "개인 봇 토큰", ref discordToken, 513, ImGuiInputTextFlags.Password);
            ImGui.Checkbox("토큰 저장 및 시작 시 자동 연결", ref rememberDiscordToken);
            ImGui.TextWrapped(rememberDiscordToken
                ? "이 Windows 계정에서 암호화해 저장합니다. 다시 실행하면 자동 연결하고 등록된 이모지를 재사용합니다."
                : "이번 연결에서만 토큰을 사용합니다. 다음 실행에는 다시 입력합니다.");
            if (ImGui.Button("디스코드 연결 켜기") && plugin.StartDiscord(discordToken, rememberDiscordToken)) discordToken = "";
        }

        if (plugin.HasSavedDiscordToken && ImGui.Button("저장한 봇 토큰 삭제"))
        {
            plugin.ForgetDiscordToken();
            rememberDiscordToken = false;
        }

        DrawSection("사용 안내");
        if (plugin.DiscordInviteUrl.Length > 0 && ImGui.Button("내 봇 초대 링크 복사"))
            ImGui.SetClipboardText(plugin.DiscordInviteUrl);
        if (ImGui.Button("개인 봇 설정 가이드 링크 복사"))
            ImGui.SetClipboardText("https://github.com/pressing7/PartyViewKR/blob/main/docs/discord.md");
        ImGui.TextWrapped("봇을 본인 서버에 초대한 뒤 봇과의 DM에서 /pvkr를 입력하세요. 서버에서 실행한 화면은 본인에게만 표시됩니다.");
        ImGui.TextWrapped("각자 만든 봇의 소유자만 조작할 수 있습니다. 게임과 PVKR이 켜져 있어야 합니다.");
    }
}
