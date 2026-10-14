using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace PartyViewKR;

internal sealed class PartyWindow : Window
{
    private readonly Plugin plugin;
    internal PartyWindow(Plugin plugin) : base("현재 파티 조회##PartyViewKR")
    {
        this.plugin = plugin;
        Size = new Vector2(720, 630);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints { MinimumSize = new Vector2(500, 250), MaximumSize = new Vector2(float.MaxValue) };
    }

    public override void Draw()
    {
        var state = plugin.Snapshot;
        ImGui.TextUnformatted($"{state.Status}  ·  {state.Members.Length}명");
        ImGui.TextUnformatted($"조회 경로: {state.Source}");
        if (state.ReadAt is { } at) ImGui.TextUnformatted($"마지막 조회: {at:HH:mm:ss}  ·  0.5초마다 자동 조회");
        if (state.IsStale) ImGui.TextUnformatted("현재 정보가 준비될 때까지 마지막 조회 결과를 표시합니다.");
        if (state.Error.Length > 0) ImGui.TextWrapped(state.Error);
        ImGui.Separator();

        if (ImGui.BeginTable("party-members", 6, ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("번호", ImGuiTableColumnFlags.WidthFixed, 34);
            ImGui.TableSetupColumn("이름");
            ImGui.TableSetupColumn("서버");
            ImGui.TableSetupColumn("직업");
            ImGui.TableSetupColumn("레벨", ImGuiTableColumnFlags.WidthFixed, 40);
            ImGui.TableSetupColumn("표시");
            ImGui.TableHeadersRow();
            for (var i = 0; i < state.Members.Length; i++)
            {
                var member = state.Members[i];
                ImGui.TableNextRow();
                Cell((i + 1).ToString()); Cell(member.Name); Cell(member.World); Cell(member.Job);
                Cell(member.Level > 0 ? member.Level.ToString() : "?");
                Cell(member.IsSelf && member.IsLeader ? "나 · 파티장" : member.IsSelf ? "나" : member.IsLeader ? "파티장" : "");
            }
            ImGui.EndTable();
        }
        ImGui.Separator();
        DrawListing();
        ImGui.Separator();
        if (ImGui.Button("다시 읽기")) plugin.RequestRead();
        ImGui.SameLine();
        if (ImGui.Button("진단 정보 복사")) ImGui.SetClipboardText(plugin.Diagnostic);
    }

    private void DrawListing()
    {
        var listing = plugin.Listing;
        ImGui.TextUnformatted("모집글 조회");
        ImGui.SameLine();
        ImGui.BeginDisabled(plugin.ListingBusy || plugin.Refresh.Busy || !Plugin.ClientState.IsLoggedIn || !Plugin.PlayerState.IsLoaded);
        if (ImGui.Button("내 모집글 조회")) plugin.QueryOwnListing();
        ImGui.EndDisabled();
        ImGui.SameLine();
        ImGui.BeginDisabled(plugin.ListingBusy || plugin.Refresh.Busy || plugin.Refresh.CoolingDown(Environment.TickCount64) ||
            !Plugin.ClientState.IsLoggedIn || !Plugin.PlayerState.IsLoaded);
        if (ImGui.Button("내 모집글 갱신")) plugin.RefreshOwnListing();
        ImGui.EndDisabled();
        if (plugin.Refresh.Busy)
        {
            ImGui.SameLine();
            if (ImGui.Button("중단")) plugin.CancelRefresh();
        }
        ImGui.PushTextWrapPos(0);
        ImGui.TextUnformatted(plugin.Refresh.Status);
        ImGui.PopTextWrapPos();
        ImGui.TextUnformatted(listing.Status);
        if (listing.ReadAt is { } at)
        {
            var owner = listing.Owner switch {
                ListingOwner.Self => "내 모집글",
                ListingOwner.PartyLeader => "내 파티장의 모집글",
                ListingOwner.Other => "다른 모집자의 글",
                _ => "현재 연 모집글 (작성자 확인 전)"
            };
            ImGui.TextUnformatted($"대상: {owner}");
            if (listing.Leader.Length > 0) ImGui.TextUnformatted($"모집자: {listing.Leader}");
            if (listing.Duty.Length > 0) ImGui.TextUnformatted($"모집 대상: {listing.Duty}");
            if (listing.Remaining.Length > 0)
                ImGui.TextUnformatted($"{(listing.IsLive ? "게임에 표시된 남은 시간" : "마지막으로 확인한 남은 시간")}: {listing.Remaining}");
            ImGui.TextUnformatted($"내용을 읽은 시각: {at:HH:mm:ss}");
            ImGui.PushTextWrapPos(0);
            ImGui.TextUnformatted(listing.Description.Length > 0 ? listing.Description : "(모집 문구 없음)");
            ImGui.PopTextWrapPos();
        }
        if (listing.Note.Length > 0)
        {
            ImGui.PushTextWrapPos(0);
            ImGui.TextUnformatted(listing.Note);
            ImGui.PopTextWrapPos();
        }
    }

    private static void Cell(string text) { ImGui.TableNextColumn(); ImGui.TextUnformatted(text); }
}
