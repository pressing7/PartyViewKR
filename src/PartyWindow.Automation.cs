using Dalamud.Bindings.ImGui;

namespace PartyViewKR;

internal sealed partial class PartyWindow
{
    private void DrawAutomation()
    {
        DrawSection("AFK 모드");
        var enabled = plugin.AfkEnabled;
        ImGui.BeginDisabled(!enabled && (!Plugin.ClientState.IsLoggedIn || !Plugin.PlayerState.IsLoaded));
        if (ImGui.Checkbox("AFK 모드 · 모집 관리", ref enabled)) plugin.SetAfkEnabled(enabled);
        ImGui.EndDisabled();
        ImGui.TextWrapped(plugin.AfkStatus);
        ImGui.TextWrapped("AFK 모드에서 본인 또는 참가한 파티의 모집글을 조회하고 알림을 보냅니다. 직업 변경은 비교하지 않습니다.");

        var settings = plugin.AfkOptions;
        var periodic = settings.PeriodicEnabled; var minutes = settings.PeriodMinutes;
        var retry = settings.RetryEnabled; var seconds = settings.RetrySeconds;
        var singleJobCompletion = settings.SingleJobCompletionEnabled;
        var joins = settings.JoinNotificationsEnabled;
        var autoRefresh = settings.RefreshEnabled; var refreshMinutes = settings.RefreshMinutes;

        DrawSection("자동 조회");
        var changed = ImGui.Checkbox("주기적으로 추가 조회", ref periodic);
        ImGui.BeginDisabled(!periodic);
        ImGui.SetNextItemWidth(160);
        changed |= ImGui.InputInt("조회 주기 (1~55분)", ref minutes);
        ImGui.EndDisabled();
        changed |= ImGui.Checkbox("조회 실패 시 자동 재시도", ref retry);
        ImGui.BeginDisabled(!retry);
        ImGui.SetNextItemWidth(160);
        changed |= ImGui.InputInt("재시도 대기 (1~3600초)", ref seconds);
        ImGui.EndDisabled();

        DrawSection("자동 갱신");
        changed |= ImGui.Checkbox("주기적으로 모집 갱신", ref autoRefresh);
        ImGui.BeginDisabled(!autoRefresh);
        ImGui.SetNextItemWidth(160);
        changed |= ImGui.InputInt("갱신 주기 (1~55분)", ref refreshMinutes);
        ImGui.EndDisabled();
        if (autoRefresh || plugin.AutoRefreshInFlight)
        {
            ImGui.TextWrapped("본인 모집글만 기존 조건으로 갱신합니다. 이미 열린 수정창의 갱신은 마무리한 뒤 꺼집니다.");
            ImGui.TextWrapped(plugin.AutoRefreshStatus);
        }
        if (plugin.AutoRefreshLastResult.Length > 0)
            ImGui.TextWrapped("최근 자동 갱신 결과: " + plugin.AutoRefreshLastResult);

        DrawSection("디스코드 알림");
        changed |= ImGui.Checkbox("파티원 참가 시 @everyone 알림", ref joins);
        if (joins) ImGui.TextWrapped("AFK 모드에서 새 파티원이 확인되면 알립니다. 처음 읽은 명단과 직업 변경은 알리지 않습니다.");
        ImGui.TextWrapped("정원이 차면 디스코드로 알립니다. 조회 카드에서 알림 채널을 지정하세요.");
        changed |= ImGui.Checkbox("단일 직업 빈자리 제외 모집 완료 알림", ref singleJobCompletion);
        if (singleJobCompletion)
            ImGui.TextWrapped("허용 직업이 정확히 하나인 빈자리만 남았을 때도 알립니다.");
        if (plugin.DiscordNotificationStatus.Length > 0) ImGui.TextWrapped(plugin.DiscordNotificationStatus);

        if (changed) plugin.SaveAfkSettings(new(periodic, minutes, retry, seconds, singleJobCompletion, autoRefresh, refreshMinutes, joins));
        if (plugin.AfkSettingsError.Length > 0) ImGui.TextWrapped(plugin.AfkSettingsError);
    }
}
