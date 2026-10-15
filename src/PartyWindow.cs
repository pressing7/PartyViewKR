using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace PartyViewKR;

internal sealed class PartyWindow : Window
{
    private readonly Plugin plugin;
    private ListingJobDraft? shownDraft;
    private ListingDescriptionDraft? shownDescriptionDraft;
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
        DrawJobEditor();
        DrawDescriptionEditor();
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
        if (plugin.JobDraftStatus.Length > 0) ImGui.TextWrapped(plugin.JobDraftStatus);
        if (plugin.DescriptionDraftStatus.Length > 0) ImGui.TextWrapped(plugin.DescriptionDraftStatus);
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
            DrawDescription(listing);
            DrawListingSlots(listing);
        }
        if (listing.Note.Length > 0)
        {
            ImGui.PushTextWrapPos(0);
            ImGui.TextUnformatted(listing.Note);
            ImGui.PopTextWrapPos();
        }
    }

    private void DrawDescription(ListingSnapshot listing)
    {
        ImGui.Separator();
        ImGui.TextUnformatted("모집 소개말");
        if (listing.Owner == ListingOwner.Self)
        {
            ImGui.SameLine();
            ImGui.BeginDisabled(!listing.IsLive || plugin.Refresh.Busy || plugin.ListingBusy || plugin.JobDraft != null ||
                ListingSlotRoute.Classify(listing.Slots) == RecruitmentSlotLayout.Unknown);
            if (ImGui.SmallButton("소개말 수정")) plugin.RequestDescriptionEdit();
            ImGui.EndDisabled();
        }
        if (ImGui.BeginChild("listing-description", new Vector2(0, ImGui.GetTextLineHeightWithSpacing() * 4 + 12), true))
        {
            ImGui.PushTextWrapPos(0);
            if (listing.Description.Length > 0) ImGui.TextUnformatted(listing.Description);
            else ImGui.TextDisabled("(모집 문구 없음)");
            ImGui.PopTextWrapPos();
        }
        ImGui.EndChild();
    }

    private void DrawDescriptionEditor()
    {
        const string popup = "모집 소개말 수정##PartyViewKR-description-editor";
        var draft = plugin.DescriptionDraft;
        if (draft == null)
        {
            if (shownDescriptionDraft != null)
            {
                var closing = true;
                if (ImGui.BeginPopupModal(popup, ref closing)) { ImGui.CloseCurrentPopup(); ImGui.EndPopup(); }
            }
            shownDescriptionDraft = null;
            return;
        }
        if (!ReferenceEquals(draft, shownDescriptionDraft)) { shownDescriptionDraft = draft; ImGui.OpenPopup(popup); }
        var open = true;
        ImGui.SetNextWindowSize(new Vector2(620, 0), ImGuiCond.Appearing);
        if (ImGui.BeginPopupModal(popup, ref open, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.TextUnformatted("모집 소개말");
            ImGui.TextDisabled("내용을 비우면 소개말 없이 모집합니다.");
            if (ImGui.IsWindowAppearing()) ImGui.SetKeyboardFocusHere();
            var text = draft.Text;
            if (ImGui.InputTextMultiline("##description-text", ref text, 4096, new Vector2(-1, ImGui.GetTextLineHeightWithSpacing() * 8)))
                draft.Text = text;
            var edit = draft.Request;
            ImGui.TextDisabled($"입력량: {(edit.ByteCount < 0 ? "?" : edit.ByteCount.ToString())} / {ListingDescriptionEdit.MaxBytes} 바이트 · 한글은 보통 3바이트");
            if (edit.ValidationError.Length > 0)
                ImGui.TextColored(new Vector4(1, 0.55f, 0.45f, 1), edit.ValidationError);
            else if (!edit.Changed) ImGui.TextDisabled("내용을 변경하면 적용할 수 있습니다.");
            else if (edit.Value.Length == 0) ImGui.TextUnformatted("적용 후 소개말이 비워집니다.");
            ImGui.Separator();
            ImGui.TextWrapped("적용하면 게임 수정창에 내용을 반영하고 모집을 갱신합니다.");
            ImGui.BeginDisabled(!edit.Valid || plugin.Refresh.Busy || plugin.ListingBusy || plugin.Refresh.CoolingDown(Environment.TickCount64));
            if (ImGui.Button("변경 적용 및 갱신", new Vector2(-1, 0)) && plugin.ApplyDescriptionDraft()) ImGui.CloseCurrentPopup();
            ImGui.EndDisabled();
            ImGui.EndPopup();
        }
        if (!open) plugin.CloseDescriptionDraft();
    }

    private void DrawListingSlots(ListingSnapshot listing)
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
        ImGui.TextWrapped("빈자리의 직업 제한에 마우스를 올리면 허용 직업을 볼 수 있습니다.");
        if (listing.Owner == ListingOwner.Self)
            ImGui.TextWrapped("빈자리를 클릭해 허용 직업 수정 · 일반 4·8인 / 연합 24·48인에서 한 자리씩 지원");
        if (!slots.CountsAgree)
            ImGui.TextWrapped("모집 인원수와 자리 정보가 아직 일치하지 않습니다. 다시 조회해 주세요.");

        var height = Math.Min(slots.Slots.Length + 1, 10) * ImGui.GetTextLineHeightWithSpacing() + 12;
        if (ImGui.BeginTable("listing-jobs", 3,
            ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.ScrollY,
            new Vector2(0, height)))
        {
            ImGui.TableSetupColumn("자리", ImGuiTableColumnFlags.WidthFixed, 40);
            ImGui.TableSetupColumn("상태", ImGuiTableColumnFlags.WidthFixed, 70);
            ImGui.TableSetupColumn("등록 직업 / 모집 조건");
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableHeadersRow();
            foreach (var slot in slots.Slots)
            {
                ImGui.TableNextRow();
                Cell(slot.Position.ToString());
                Cell(slot.HasMember ? "참가 중" : slots.CountsAgree ? "빈자리" : "확인 중");
                DrawListingSlotJob(slot, listing);
            }
            ImGui.EndTable();
        }
    }

    private void DrawListingSlotJob(ListingSlot slot, ListingSnapshot listing)
    {
        if (slot.HasMember)
        {
            Cell(slot.JobId != 0 ? slot.Job : "직업 정보 없음");
            return;
        }
        if (!listing.Slots.CountsAgree) { Cell("확인 중"); return; }

        var allowed = slot.AllowedJobs;
        var label = !allowed.Complete ? "직업 조건 확인 불가" : allowed.Anyone ? "누구나 가능" : "직업 제한";
        if (plugin.JobDraft is { Changed: true } draft && draft.Position == slot.Position)
            label += " · 변경 예정";
        ImGui.TableNextColumn();
        var editable = listing.IsLive && listing.Owner == ListingOwner.Self && allowed.Complete &&
            ListingSlotRoute.Classify(listing.Slots) != RecruitmentSlotLayout.Unknown;
        if (editable)
        {
            ImGui.BeginDisabled(plugin.Refresh.Busy || plugin.ListingBusy);
            if (ImGui.SmallButton($"{label}##slot-{slot.Position}")) plugin.RequestSlotEdit(slot.Position);
            ImGui.EndDisabled();
        }
        else ImGui.TextUnformatted(label);
        if (!ImGui.IsItemHovered()) return;
        ImGui.BeginTooltip();
        ImGui.PushTextWrapPos(ImGui.GetFontSize() * 26);
        ImGui.TextUnformatted("허용 직업");
        ImGui.Separator();
        if (allowed.Names.Length > 0)
            ImGui.TextUnformatted(string.Join(", ", allowed.Names));
        else if (allowed.Complete)
            ImGui.TextUnformatted("허용된 직업이 없습니다.");
        if (!allowed.Complete)
            ImGui.TextUnformatted("허용 직업을 모두 확인하지 못했습니다. 모집글을 다시 조회해 주세요.");
        ImGui.PopTextWrapPos();
        ImGui.EndTooltip();
    }

    private void DrawJobEditor()
    {
        const string popup = "빈자리 허용 직업##PartyViewKR-job-editor";
        var draft = plugin.JobDraft;
        if (draft == null)
        {
            // Logout/character changes can discard a draft while its modal is open.
            if (shownDraft != null)
            {
                var closing = true;
                if (ImGui.BeginPopupModal(popup, ref closing)) { ImGui.CloseCurrentPopup(); ImGui.EndPopup(); }
            }
            shownDraft = null;
            return;
        }
        if (!ReferenceEquals(draft, shownDraft)) { shownDraft = draft; ImGui.OpenPopup(popup); }
        var open = true;
        ImGui.SetNextWindowSize(new Vector2(640, 0), ImGuiCond.Appearing);
        if (ImGui.BeginPopupModal(popup, ref open, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.TextUnformatted($"{draft.Position}번 빈자리 · {(draft.Changed ? "변경 예정" : "현재 허용 직업")}");
            ImGui.TextUnformatted("적용 버튼을 누르면 모집글을 다시 조회한 뒤 변경하고 갱신합니다.");
            if (ImGui.Button("전체 선택")) draft.Selected = draft.Rules.AllFlags;
            ImGui.SameLine();
            if (ImGui.Button("전체 해제")) draft.Selected = 0;
            ImGui.Separator();
            if (ImGui.BeginTable("job-choices", 4, ImGuiTableFlags.SizingStretchSame))
            {
                for (var i = 0; i < draft.Rules.Choices.Count; i++)
                {
                    if (i % 4 == 0) ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    var job = draft.Rules.Choices[i];
                    var selected = (draft.Selected & job.Flag) != 0;
                    if (ImGui.Checkbox($"{job.Name}##job-{job.Id}", ref selected))
                        draft.Selected = selected ? draft.Selected | job.Flag : draft.Selected & ~job.Flag;
                }
                ImGui.EndTable();
            }
            ImGui.Separator();
            if (draft.Selected == 0) ImGui.TextUnformatted("직업을 하나 이상 선택해 주세요.");
            ImGui.BeginDisabled(!draft.Request.Valid || plugin.Refresh.Busy || plugin.ListingBusy || plugin.Refresh.CoolingDown(Environment.TickCount64));
            if (ImGui.Button("변경 적용 및 갱신") && plugin.ApplyJobDraft()) ImGui.CloseCurrentPopup();
            ImGui.EndDisabled();
            ImGui.EndPopup();
        }
        if (!open) plugin.CloseJobDraft();
    }

    private static void Cell(string text) { ImGui.TableNextColumn(); ImGui.TextUnformatted(text); }
}
