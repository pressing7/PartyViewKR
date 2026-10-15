using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace PartyViewKR;

internal sealed class PartyWindow : Window
{
    private readonly Plugin plugin;
    internal PartyWindow(Plugin plugin) : base("파티 모집 조회##PartyViewKR")
    {
        this.plugin = plugin;
        Size = new Vector2(720, 630);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints { MinimumSize = new Vector2(500, 250), MaximumSize = new Vector2(float.MaxValue) };
    }

    public override void Draw()
    {
        DrawListing();
        ImGui.Separator();
        if (ImGui.Button("다시 읽기")) plugin.RequestRead();
        ImGui.SameLine();
        if (ImGui.Button("진단 정보 복사")) ImGui.SetClipboardText(plugin.Diagnostic);
        ImGui.Separator();
        if (ImGui.CollapsingHeader("현재 파티 상태 · 실시간 직업"))
            DrawCurrentParty();
    }

    private void DrawCurrentParty()
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
            DrawListingSlots(listing);
        }
        if (listing.Note.Length > 0)
        {
            ImGui.PushTextWrapPos(0);
            ImGui.TextUnformatted(listing.Note);
            ImGui.PopTextWrapPos();
        }
    }

    private static void DrawListingSlots(ListingSnapshot listing)
    {
        ImGui.Separator();
        var slots = listing.Slots;
        ImGui.TextUnformatted(listing.IsLive ? "모집창 등록 직업" : "마지막으로 읽은 모집창 등록 직업");
        if (!slots.Readable)
        {
            ImGui.TextUnformatted("모집 직업 정보를 확인하지 못했습니다. 모집글을 다시 조회해 주세요.");
            return;
        }
        ImGui.TextUnformatted($"모집 인원: {slots.SlotsFilled}/{slots.TotalSlots}");
        if (slots.NumberOfParties > 1)
            ImGui.TextUnformatted($"모집 파티 수: {slots.NumberOfParties}");
        ImGui.TextUnformatted("모집글에서 받은 직업을 자리 순서대로 표시합니다.");
        if (!slots.CountsAgree)
            ImGui.TextWrapped("모집 인원수와 자리 정보가 아직 일치하지 않습니다. 다시 조회해 주세요.");

        var height = Math.Min(slots.Slots.Length + 1, 10) * ImGui.GetTextLineHeightWithSpacing() + 12;
        if (ImGui.BeginTable("listing-jobs", 3,
            ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.ScrollY,
            new Vector2(0, height)))
        {
            ImGui.TableSetupColumn("자리", ImGuiTableColumnFlags.WidthFixed, 40);
            ImGui.TableSetupColumn("상태", ImGuiTableColumnFlags.WidthFixed, 70);
            ImGui.TableSetupColumn("등록 직업");
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableHeadersRow();
            foreach (var slot in slots.Slots)
            {
                ImGui.TableNextRow();
                Cell(slot.Position.ToString());
                Cell(slot.HasMember ? "참가 중" : slots.CountsAgree ? "빈자리" : "확인 중");
                Cell(slot.JobId != 0 ? slot.Job : slot.HasMember ? "직업 정보 없음" : "—");
            }
            ImGui.EndTable();
        }
    }

    private static void Cell(string text) { ImGui.TableNextColumn(); ImGui.TextUnformatted(text); }
}
