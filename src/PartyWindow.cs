using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace PartyViewKR;

internal sealed class PartyWindow : Window
{
    private readonly Plugin plugin;
    private ListingJobDraft? shownDraft;
    private ListingDescriptionDraft? shownDescriptionDraft;
    private ListingObjectiveDraft? shownObjectiveDraft;
    private ListingBeginnerDraft? shownBeginnerDraft;
    private ListingUniqueJobsDraft? shownUniqueJobsDraft;
    private ListingPrivateDraft? shownPrivateDraft;
    private ListingCompletionDraft? shownCompletionDraft;
    private ListingItemLevelDraft? shownItemLevelDraft;
    private ListingRuleDraft? shownRuleDraft;
    private string relayAddress = "", relayKey = "";
    private bool relayAddressLoaded;
    private string discordToken = "";
    private bool discordSettingsLoaded, rememberDiscordToken;
    internal PartyWindow(Plugin plugin) : base("파티 모집 조회##PartyViewKR")
    {
        this.plugin = plugin;
        Size = new Vector2(720, 630);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints { MinimumSize = new Vector2(500, 250), MaximumSize = new Vector2(float.MaxValue) };
    }

    public override void Draw()
    {
        DrawAfk();
        DrawListing();
        DrawJobEditor();
        DrawDescriptionEditor();
        DrawObjectiveEditor();
        DrawBeginnerEditor();
        DrawUniqueJobsEditor();
        DrawPrivateEditor();
        DrawCompletionEditor();
        DrawItemLevelEditor();
        DrawRuleEditor();
        ImGui.Separator();
        if (ImGui.Button("다시 읽기")) plugin.RequestRead();
        ImGui.SameLine();
        if (ImGui.Button("진단 정보 복사")) ImGui.SetClipboardText(plugin.Diagnostic);
        DrawObjectiveTrace();
        ImGui.Separator();
        DrawBrowser();
        DrawRelay();
        DrawDiscord();
        if (ImGui.CollapsingHeader("현재 파티 상태 · 실시간 직업"))
            DrawCurrentParty();
    }

    private void DrawAfk()
    {
        var enabled = plugin.AfkEnabled;
        ImGui.BeginDisabled(!enabled && (!Plugin.ClientState.IsLoggedIn || !Plugin.PlayerState.IsLoaded));
        if (ImGui.Checkbox("AFK 모드 · 모집글 자동 조회", ref enabled)) plugin.SetAfkEnabled(enabled);
        ImGui.EndDisabled();
        if (enabled)
        {
            ImGui.TextWrapped(plugin.AfkStatus);
            ImGui.TextDisabled("명단 변화 시 조회 · 직업 변경은 비교하지 않음");
        }
        if (ImGui.TreeNode("AFK 조회 설정"))
        {
            var settings = plugin.AfkOptions;
            var periodic = settings.PeriodicEnabled; var minutes = settings.PeriodMinutes;
            var retry = settings.RetryEnabled; var seconds = settings.RetrySeconds;
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
            if (changed) plugin.SaveAfkSettings(new(periodic, minutes, retry, seconds));
            if (plugin.AfkSettingsError.Length > 0) ImGui.TextWrapped(plugin.AfkSettingsError);
            ImGui.TreePop();
        }
        ImGui.Separator();
    }

    private void DrawBrowser()
    {
        if (!ImGui.CollapsingHeader("브라우저 조회·수정")) return;
        ImGui.TextWrapped(plugin.WebStatus);
        if (plugin.WebEnabled)
        {
            ImGui.TextUnformatted(plugin.WebAddress);
            if (ImGui.Button("접속 링크 복사")) ImGui.SetClipboardText(plugin.WebLink);
            ImGui.SameLine();
            if (ImGui.Button("브라우저 조회 끄기")) plugin.StopWeb();
        }
        else if (ImGui.Button("브라우저 조회 켜기")) plugin.StartWeb();
        ImGui.TextWrapped("접속 링크를 같은 PC의 브라우저 주소창에 붙여넣으세요. 내 모집글을 조회하고 소개말·직업 제한·모집 조건을 한 번에 수정·갱신할 수 있습니다.");
    }

    private void DrawRelay()
    {
        if (!ImGui.CollapsingHeader("외부 접속 · 휴대폰 브라우저")) return;
        if (!relayAddressLoaded) { relayAddress = plugin.RelaySavedAddress; relayAddressLoaded = true; }
        ImGui.TextWrapped(plugin.RelayStatus);
        if (plugin.RelayEnabled)
        {
            ImGui.TextUnformatted(relayAddress);
            ImGui.BeginDisabled(!plugin.RelayConnected);
            if (ImGui.Button("외부 접속 링크 복사")) ImGui.SetClipboardText(plugin.RelayLink);
            ImGui.SameLine();
            if (ImGui.Button("연결 코드 복사")) ImGui.SetClipboardText(plugin.RelayCode);
            ImGui.EndDisabled();
            if (ImGui.Button("외부 연결 끄기")) plugin.StopRelay();
            ImGui.TextWrapped("휴대폰에서 링크를 열거나 서버 웹사이트에 연결 코드를 붙여넣으세요. 연결을 껐다 켜면 새 링크가 필요합니다.");
        }
        else
        {
            ImGui.SetNextItemWidth(-1);
            ImGui.InputTextWithHint("##relay-address", "https://pvkr-relay.내주소.workers.dev", ref relayAddress, 512);
            ImGui.SetNextItemWidth(-1);
            ImGui.InputTextWithHint("##relay-key", "서버 연결 키 (PVKR_CONNECT_KEY)", ref relayKey, 65, ImGuiInputTextFlags.Password);
            if (ImGui.Button("새 서버 연결 키 만들기")) relayKey = Plugin.GenerateRelayKey();
            ImGui.SameLine();
            ImGui.BeginDisabled(!ListingRelayProtocol.IsKey(relayKey.Trim().ToUpperInvariant()));
            if (ImGui.Button("서버 연결 키 복사")) ImGui.SetClipboardText(relayKey.Trim().ToUpperInvariant());
            ImGui.EndDisabled();
            ImGui.TextWrapped("처음에는 서버 연결 키를 만들어 Cloudflare의 PVKR_CONNECT_KEY 비밀 값에 등록하세요. 이 키는 파일에 저장하지 않으므로 다음 실행에도 필요합니다.");
            if (ImGui.Button("외부 연결 켜기") && plugin.StartRelay(relayAddress, relayKey)) relayKey = "";
            ImGui.TextWrapped("같은 PC 브라우저·외부 브라우저·디스코드 연결 중 하나를 사용합니다. 게임과 PVKR이 실행되어 있어야 합니다.");
        }
    }

    private void DrawDiscord()
    {
        if (!ImGui.CollapsingHeader("외부 접속 · 개인 디스코드 봇")) return;
        if (!discordSettingsLoaded) { rememberDiscordToken = plugin.RememberDiscordToken; discordSettingsLoaded = true; }
        ImGui.TextWrapped(plugin.DiscordStatus);
        if (plugin.DiscordNotificationStatus.Length > 0) ImGui.TextWrapped(plugin.DiscordNotificationStatus);
        if (plugin.DiscordChatStatus.Length > 0) ImGui.TextWrapped(plugin.DiscordChatStatus);
        DrawDiscordChatControls();
        if (plugin.DiscordEnabled) {
            if (plugin.DiscordInviteUrl.Length > 0 && ImGui.Button("내 봇 초대 링크 복사")) ImGui.SetClipboardText(plugin.DiscordInviteUrl);
            ImGui.TextWrapped("봇을 본인 서버에 초대한 뒤 봇과의 DM에서 /pvkr를 입력하세요. 서버에서 실행한 화면은 본인에게만 표시됩니다.");
            if (ImGui.Button("디스코드 연결 끄기")) plugin.TurnOffDiscord();
        }
        else {
            ImGui.SetNextItemWidth(-1);
            ImGui.InputTextWithHint("##discord-token", plugin.HasSavedDiscordToken ? "저장한 토큰 사용 (변경하려면 입력)" : "개인 봇 토큰", ref discordToken, 513, ImGuiInputTextFlags.Password);
            ImGui.Checkbox("토큰 저장 및 시작 시 자동 연결", ref rememberDiscordToken);
            ImGui.TextWrapped(rememberDiscordToken
                ? "토큰은 이 Windows 계정에서 암호화해 저장합니다. 플러그인을 다시 켜면 자동 연결하고 등록된 이모지를 재사용합니다."
                : "이번 연결에서만 토큰을 사용합니다. 다음 실행에는 다시 입력합니다.");
            if (ImGui.Button("디스코드 연결 켜기") && plugin.StartDiscord(discordToken, rememberDiscordToken)) discordToken = "";
        }
        if (plugin.HasSavedDiscordToken && ImGui.Button("저장한 봇 토큰 삭제")) { plugin.ForgetDiscordToken(); rememberDiscordToken = false; }
        if (ImGui.Button("개인 봇 설정 가이드 링크 복사")) ImGui.SetClipboardText("https://github.com/pressing7/PartyViewKR/blob/main/docs/discord.md");
        ImGui.TextWrapped("각자 만든 봇의 소유자만 조작할 수 있습니다. 게임과 PVKR이 켜져 있어야 하며 다른 원격 연결과 번갈아 사용합니다.");
    }

    private void DrawDiscordChatControls()
    {
        ImGui.Separator();
        ImGui.TextUnformatted("디스코드 채팅 전달");
        ImGui.TextWrapped("AFK 모드와 관계없이 전달합니다. 수신 채널은 서버에서 /pvkr-chat 설정으로 지정하세요.");
        var settings = plugin.DiscordChatSettings;
        if (!ImGui.BeginTable("discord-chat-settings", 4, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg)) return;
        ImGui.TableSetupColumn("채팅 종류");
        ImGui.TableSetupColumn("수신");
        ImGui.TableSetupColumn("@everyone 멘션");
        ImGui.TableSetupColumn("수신 채널");
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
            var mention = route?.MentionEveryone == true;
            if (ImGui.Checkbox("##mention", ref mention)) plugin.SetDiscordChat(kind, mention: mention);
            ImGui.EndDisabled();
            Cell(route == null ? "미설정" : route.ChannelId.ToString());
            ImGui.PopID();
        }
        ImGui.EndTable();
        ImGui.Separator();
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
        ImGui.BeginDisabled(plugin.QueryUnavailableReason != null);
        if (ImGui.Button("내 모집글 조회")) plugin.QueryOwnListing();
        ImGui.EndDisabled();
        ImGui.SameLine();
        ImGui.BeginDisabled(plugin.ObjectiveTrace.Busy || plugin.ListingBusy || plugin.Refresh.Busy || plugin.Refresh.CoolingDown(Environment.TickCount64) ||
            !Plugin.ClientState.IsLoggedIn || !Plugin.PlayerState.IsLoaded);
        if (ImGui.Button(plugin.PendingChanges.Items.Count > 0 ? $"변경사항 적용 및 갱신 ({plugin.PendingChanges.Items.Count})" : "내 모집글 갱신")) plugin.RefreshOwnListing();
        ImGui.EndDisabled();
        if (plugin.Refresh.Busy)
        {
            ImGui.SameLine();
            if (ImGui.Button("중단")) plugin.CancelRefresh();
        }
        ImGui.PushTextWrapPos(0);
        ImGui.TextUnformatted(plugin.Refresh.Status);
        ImGui.PopTextWrapPos();
        if (plugin.PendingStatus.Length > 0) ImGui.TextWrapped(plugin.PendingStatus);
        if (plugin.PendingChanges.Items.Count > 0)
        {
            ImGui.TextUnformatted($"적용 대기: {plugin.PendingChanges.Items.Count}개 · 위 버튼을 누르면 한 번에 갱신합니다.");
            foreach (var change in plugin.PendingChanges.Items)
            {
                ImGui.TextWrapped($"· {change.Title}: {change.ValueLabel}");
                if (change.Edit is ListingJobEdit jobs && ImGui.IsItemHovered())
                    ImGui.SetTooltip(string.Join(" · ", jobs.Rules.Describe(jobs.Selected).Names));
            }
        }
        if (!plugin.Refresh.Busy && plugin.Refresh.BatchResults.Count > 0)
        {
            ImGui.TextUnformatted("최근 일괄 수정 결과");
            foreach (var item in plugin.Refresh.BatchResults) ImGui.TextWrapped("· " + item.Text);
        }
        if (plugin.JobDraftStatus.Length > 0) ImGui.TextWrapped(plugin.JobDraftStatus);
        if (plugin.DescriptionDraftStatus.Length > 0) ImGui.TextWrapped(plugin.DescriptionDraftStatus);
        if (plugin.ObjectiveDraftStatus.Length > 0) ImGui.TextWrapped(plugin.ObjectiveDraftStatus);
        if (plugin.BeginnerDraftStatus.Length > 0) ImGui.TextWrapped(plugin.BeginnerDraftStatus);
        if (plugin.UniqueJobsDraftStatus.Length > 0) ImGui.TextWrapped(plugin.UniqueJobsDraftStatus);
        if (plugin.PrivateDraftStatus.Length > 0) ImGui.TextWrapped(plugin.PrivateDraftStatus);
        if (plugin.CompletionDraftStatus.Length > 0) ImGui.TextWrapped(plugin.CompletionDraftStatus);
        if (plugin.RuleDraftStatus.Length > 0) ImGui.TextWrapped(plugin.RuleDraftStatus);
        if (plugin.ItemLevelDraftStatus.Length > 0) ImGui.TextWrapped(plugin.ItemLevelDraftStatus);
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
            DrawObjective(listing);
            DrawCompletion(listing);
            DrawItemLevel(listing);
            foreach (var kind in Enum.GetValues<ListingRuleKind>()) DrawRule(listing, kind);
            DrawBeginner(listing);
            DrawUniqueJobs(listing);
            DrawPrivate(listing);
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
            ImGui.BeginDisabled(plugin.RuleDraft != null || plugin.ObjectiveTrace.Busy || !listing.IsLive || plugin.Refresh.Busy || plugin.ListingBusy || plugin.JobDraft != null || plugin.ObjectiveDraft != null || plugin.BeginnerDraft != null || plugin.UniqueJobsDraft != null || plugin.PrivateDraft != null || plugin.CompletionDraft != null || plugin.ItemLevelDraft != null ||
                !ListingSlotRoute.SupportsSettings(listing.Slots));
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
            ImGui.TextWrapped("변경 목록에 저장한 뒤 메인 창에서 한 번에 적용합니다.");
            ImGui.BeginDisabled(!plugin.CanQueue(edit) || plugin.Refresh.Busy || plugin.ListingBusy || plugin.Refresh.CoolingDown(Environment.TickCount64));
            if (ImGui.Button("변경 목록에 저장", new Vector2(-1, 0)) && plugin.ApplyDescriptionDraft()) ImGui.CloseCurrentPopup();
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
        if (slots.UnregisteredNativeIndices.Length > 0)
            ImGui.TextWrapped("각 파티 뒤쪽의 미등록 자리는 생략하고, 조회된 팀과 자리 번호를 표시합니다.");
        ImGui.TextWrapped("빈자리의 직업 제한에 마우스를 올리면 허용 직업을 볼 수 있습니다.");
        if (listing.Owner == ListingOwner.Self)
            ImGui.TextWrapped("빈자리를 클릭해 허용 직업 수정 · 일반 4·8인 / 연합 24·48인 / 친선경기 5·5·8인 지원");
        if (!slots.CountsAgree)
            ImGui.TextWrapped("모집 인원수와 자리 정보가 아직 일치하지 않습니다. 다시 조회해 주세요.");

        var height = Math.Min(slots.Slots.Length + 1, 10) * ImGui.GetTextLineHeightWithSpacing() + 12;
        if (ImGui.BeginTable("listing-jobs", 3,
            ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.ScrollY,
            new Vector2(0, height)))
        {
            ImGui.TableSetupColumn("자리", ImGuiTableColumnFlags.WidthFixed, slots.NumberOfParties > 1 ? ImGui.CalcTextSize("그림자팀 8").X + 12 : 40);
            ImGui.TableSetupColumn("상태", ImGuiTableColumnFlags.WidthFixed, 70);
            ImGui.TableSetupColumn("등록 직업 / 모집 조건");
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableHeadersRow();
            foreach (var slot in slots.Slots)
            {
                ImGui.TableNextRow();
                Cell(slots.PositionLabel(slot));
                Cell(slot.HasMember ? "참가 중" : slots.CountsAgree ? "빈자리" : "확인 중");
                DrawListingSlotJob(slot, listing);
            }
            ImGui.EndTable();
        }
    }

    private void DrawObjective(ListingSnapshot listing)
    {
        ImGui.Separator();
        ImGui.TextUnformatted($"모집 목적: {ListingObjectives.Label(listing.Objective)}");
        if (listing.Owner != ListingOwner.Self) return;
        ImGui.SameLine();
        ImGui.BeginDisabled(plugin.RuleDraft != null || !ListingObjectives.AutomaticEditingAvailable || plugin.ObjectiveTrace.Busy || !listing.IsLive || !listing.Objective.HasValue || plugin.Refresh.Busy || plugin.ListingBusy ||
            plugin.JobDraft != null || plugin.DescriptionDraft != null || plugin.BeginnerDraft != null || plugin.UniqueJobsDraft != null || plugin.PrivateDraft != null || plugin.CompletionDraft != null || plugin.ItemLevelDraft != null || !ListingSlotRoute.SupportsSettings(listing.Slots));
        if (ImGui.SmallButton("목적 수정")) plugin.RequestObjectiveEdit();
        ImGui.EndDisabled();
    }

    private void DrawBeginner(ListingSnapshot listing)
    {
        ImGui.TextUnformatted($"초행 및 초보자 환영: {ListingBeginners.Label(listing.BeginnerFriendly)}");
        if (listing.Owner != ListingOwner.Self) return;
        ImGui.SameLine();
        ImGui.BeginDisabled(plugin.RuleDraft != null || plugin.ObjectiveTrace.Busy || !listing.IsLive || !listing.BeginnerFriendly.HasValue ||
            plugin.Refresh.Busy || plugin.ListingBusy || plugin.JobDraft != null || plugin.DescriptionDraft != null || plugin.ObjectiveDraft != null || plugin.UniqueJobsDraft != null || plugin.PrivateDraft != null || plugin.CompletionDraft != null || plugin.ItemLevelDraft != null ||
            !ListingSlotRoute.SupportsSettings(listing.Slots));
        if (ImGui.SmallButton("초보자 환영 설정")) plugin.RequestBeginnerEdit();
        ImGui.EndDisabled();
    }

    private void DrawBeginnerEditor()
    {
        const string popup = "초행 및 초보자 환영 설정##PartyViewKR-beginner-editor";
        var draft = plugin.BeginnerDraft;
        if (draft == null)
        {
            if (shownBeginnerDraft != null)
            {
                var closing = true;
                if (ImGui.BeginPopupModal(popup, ref closing)) { ImGui.CloseCurrentPopup(); ImGui.EndPopup(); }
            }
            shownBeginnerDraft = null;
            return;
        }
        if (!ReferenceEquals(draft, shownBeginnerDraft)) { shownBeginnerDraft = draft; ImGui.OpenPopup(popup); }
        var open = true;
        if (ImGui.BeginPopupModal(popup, ref open, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.TextUnformatted($"현재 설정: {ListingBeginners.Label(ListingBeginners.Read(draft.Original.Criteria))}");
            var enabled = draft.Enabled;
            if (ImGui.Checkbox("초행 및 초보자 환영", ref enabled)) draft.Enabled = enabled;
            var edit = draft.Request;
            if (!edit.Changed) ImGui.TextDisabled("체크 상태를 변경하면 적용할 수 있습니다.");
            ImGui.Separator();
            ImGui.TextUnformatted("변경 목록에 저장한 뒤 메인 창에서 한 번에 적용합니다.");
            ImGui.BeginDisabled(!plugin.CanQueue(edit) || plugin.Refresh.Busy || plugin.ListingBusy || plugin.Refresh.CoolingDown(Environment.TickCount64));
            if (ImGui.Button("변경 목록에 저장", new Vector2(340, 0)) && plugin.ApplyBeginnerDraft()) ImGui.CloseCurrentPopup();
            ImGui.EndDisabled();
            ImGui.EndPopup();
        }
        if (!open) plugin.CloseBeginnerDraft();
    }

    private void DrawUniqueJobs(ListingSnapshot listing)
    {
        ImGui.TextUnformatted($"잡 중복 없음: {ListingCheckboxValues.Label(listing.UniqueJobs)}");
        if (listing.Owner != ListingOwner.Self) return;
        ImGui.SameLine();
        ImGui.BeginDisabled(plugin.RuleDraft != null || plugin.ObjectiveTrace.Busy || !listing.IsLive || !listing.UniqueJobs.HasValue ||
            plugin.Refresh.Busy || plugin.ListingBusy || plugin.JobDraft != null || plugin.DescriptionDraft != null || plugin.ObjectiveDraft != null || plugin.BeginnerDraft != null || plugin.PrivateDraft != null || plugin.CompletionDraft != null || plugin.ItemLevelDraft != null ||
            !ListingSlotRoute.SupportsSettings(listing.Slots));
        if (ImGui.SmallButton("잡 중복 없음 설정")) plugin.RequestUniqueJobsEdit();
        ImGui.EndDisabled();
    }

    private void DrawUniqueJobsEditor()
    {
        const string popup = "잡 중복 없음 설정##PartyViewKR-unique-jobs-editor";
        var draft = plugin.UniqueJobsDraft;
        if (draft == null)
        {
            if (shownUniqueJobsDraft != null)
            {
                var closing = true;
                if (ImGui.BeginPopupModal(popup, ref closing)) { ImGui.CloseCurrentPopup(); ImGui.EndPopup(); }
            }
            shownUniqueJobsDraft = null;
            return;
        }
        if (!ReferenceEquals(draft, shownUniqueJobsDraft)) { shownUniqueJobsDraft = draft; ImGui.OpenPopup(popup); }
        var open = true;
        if (ImGui.BeginPopupModal(popup, ref open, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.TextUnformatted($"현재 설정: {ListingCheckboxValues.Label(ListingUniqueJobs.Read(draft.Original.ExtraCriteria))}");
            var enabled = draft.Enabled;
            if (ImGui.Checkbox("잡 중복 없음", ref enabled)) draft.Enabled = enabled;
            var edit = draft.Request;
            if (!edit.Changed) ImGui.TextDisabled("체크 상태를 변경하면 적용할 수 있습니다.");
            ImGui.Separator();
            ImGui.TextUnformatted("변경 목록에 저장한 뒤 메인 창에서 한 번에 적용합니다.");
            ImGui.BeginDisabled(!plugin.CanQueue(edit) || plugin.Refresh.Busy || plugin.ListingBusy || plugin.Refresh.CoolingDown(Environment.TickCount64));
            if (ImGui.Button("변경 목록에 저장", new Vector2(340, 0)) && plugin.ApplyUniqueJobsDraft()) ImGui.CloseCurrentPopup();
            ImGui.EndDisabled();
            ImGui.EndPopup();
        }
        if (!open) plugin.CloseUniqueJobsDraft();
    }

    private void DrawPrivate(ListingSnapshot listing)
    {
        ImGui.TextUnformatted($"비공개: {ListingCheckboxValues.Label(listing.Private)}");
        if (listing.Owner != ListingOwner.Self) return;
        ImGui.SameLine();
        ImGui.BeginDisabled(plugin.RuleDraft != null || plugin.ObjectiveTrace.Busy || !listing.IsLive || !listing.Private.HasValue ||
            plugin.Refresh.Busy || plugin.ListingBusy || plugin.JobDraft != null || plugin.DescriptionDraft != null ||
            plugin.ObjectiveDraft != null || plugin.BeginnerDraft != null || plugin.UniqueJobsDraft != null || plugin.CompletionDraft != null || plugin.ItemLevelDraft != null ||
            !ListingSlotRoute.SupportsSettings(listing.Slots));
        if (ImGui.SmallButton("비공개 설정")) plugin.RequestPrivateEdit();
        ImGui.EndDisabled();
    }

    private void DrawPrivateEditor()
    {
        const string popup = "비공개 설정##PartyViewKR-private-editor";
        var draft = plugin.PrivateDraft;
        if (draft == null)
        {
            if (shownPrivateDraft != null)
            {
                var closing = true;
                if (ImGui.BeginPopupModal(popup, ref closing)) { ImGui.CloseCurrentPopup(); ImGui.EndPopup(); }
            }
            shownPrivateDraft = null;
            return;
        }
        if (!ReferenceEquals(draft, shownPrivateDraft)) { shownPrivateDraft = draft; ImGui.OpenPopup(popup); }
        var open = true;
        if (ImGui.BeginPopupModal(popup, ref open, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.TextUnformatted($"현재 설정: {ListingCheckboxValues.Label(ListingPrivate.Read(draft.Original.ExtraCriteria))}");
            var enabled = draft.Enabled;
            if (ImGui.Checkbox("비공개 설정", ref enabled)) draft.Enabled = enabled;
            if (draft.Enabled)
            {
                var password = draft.Password;
                ImGui.SetNextItemWidth(180);
                // Allow an overlong paste to remain invalid instead of silently
                // truncating it to a different, valid four-digit password.
                if (ImGui.InputText("비밀번호 (숫자 4자리)", ref password, 32, ImGuiInputTextFlags.CharsDecimal)) draft.Password = password;
                if (!ListingPrivate.TryPassword(draft.Password, out _)) ImGui.TextDisabled("0000~9999 사이의 숫자 네 자리를 입력해 주세요.");
                if (ListingPrivate.Read(draft.Original.ExtraCriteria) == true)
                    ImGui.TextUnformatted("비공개를 유지하며 입력한 비밀번호로 변경합니다.");
            }
            else ImGui.TextUnformatted("비공개를 끄면 비밀번호 없이 참가할 수 있습니다.");
            var edit = draft.Request;
            ImGui.Separator();
            ImGui.TextUnformatted("변경 목록에 저장한 뒤 메인 창에서 한 번에 적용합니다.");
            ImGui.BeginDisabled(!plugin.CanQueue(edit) || plugin.Refresh.Busy || plugin.ListingBusy || plugin.Refresh.CoolingDown(Environment.TickCount64));
            if (ImGui.Button("변경 목록에 저장", new Vector2(400, 0)) && plugin.ApplyPrivateDraft()) ImGui.CloseCurrentPopup();
            ImGui.EndDisabled();
            ImGui.EndPopup();
        }
        if (!open) plugin.ClosePrivateDraft();
    }

    private void DrawCompletion(ListingSnapshot listing)
    {
        ImGui.TextUnformatted($"완료조건: {ListingCompletions.Label(listing.Completion)}");
        if (listing.Owner != ListingOwner.Self) return;
        ImGui.SameLine();
        ImGui.BeginDisabled(plugin.RuleDraft != null || plugin.ObjectiveTrace.Busy || !listing.IsLive || !ListingCompletions.Supported(listing.Completion) || plugin.ItemLevelDraft != null ||
            plugin.Refresh.Busy || plugin.ListingBusy || plugin.JobDraft != null || plugin.DescriptionDraft != null ||
            plugin.ObjectiveDraft != null || plugin.BeginnerDraft != null || plugin.UniqueJobsDraft != null || plugin.PrivateDraft != null ||
            !ListingSlotRoute.SupportsSettings(listing.Slots));
        if (ImGui.SmallButton("완료조건 설정")) plugin.RequestCompletionEdit();
        ImGui.EndDisabled();
    }

    private void DrawCompletionEditor()
    {
        const string popup = "완료조건 설정##PartyViewKR-completion-editor";
        var draft = plugin.CompletionDraft;
        if (draft == null)
        {
            if (shownCompletionDraft != null)
            {
                var closing = true;
                if (ImGui.BeginPopupModal(popup, ref closing)) { ImGui.CloseCurrentPopup(); ImGui.EndPopup(); }
            }
            shownCompletionDraft = null;
            return;
        }
        if (!ReferenceEquals(draft, shownCompletionDraft)) { shownCompletionDraft = draft; ImGui.OpenPopup(popup); }
        var open = true;
        if (ImGui.BeginPopupModal(popup, ref open, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.TextUnformatted($"현재 조건: {ListingCompletions.Label(ListingCompletions.Read(draft.Original.Criteria))}");
            var enabled = draft.Enabled;
            if (ImGui.Checkbox("완료조건 설정", ref enabled)) draft.Enabled = enabled;
            ImGui.BeginDisabled(!draft.Enabled);
            ImGui.SetNextItemWidth(340);
            if (ImGui.BeginCombo("##completion-choice", ListingCompletions.Label(ListingCompletions.Value(true, draft.SelectedIndex))))
            {
                for (var i = 0; i < ListingCompletions.Names.Length; i++)
                {
                    var selected = draft.SelectedIndex == i;
                    if (ImGui.Selectable(ListingCompletions.Names[i], selected)) draft.SelectedIndex = i;
                    if (selected) ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }
            ImGui.EndDisabled();
            var edit = draft.Request;
            if (!draft.Enabled) ImGui.TextUnformatted("완료 여부에 제한을 두지 않습니다.");
            if (!edit.Changed) ImGui.TextDisabled("체크 상태나 완료조건을 변경하면 적용할 수 있습니다.");
            ImGui.Separator();
            ImGui.TextUnformatted("변경 목록에 저장한 뒤 메인 창에서 한 번에 적용합니다.");
            ImGui.BeginDisabled(!plugin.CanQueue(edit) || plugin.Refresh.Busy || plugin.ListingBusy || plugin.Refresh.CoolingDown(Environment.TickCount64));
            if (ImGui.Button("변경 목록에 저장", new Vector2(400, 0)) && plugin.ApplyCompletionDraft()) ImGui.CloseCurrentPopup();
            ImGui.EndDisabled();
            ImGui.EndPopup();
        }
        if (!open) plugin.CloseCompletionDraft();
    }

    private void DrawItemLevel(ListingSnapshot listing)
    {
        ImGui.TextUnformatted($"평균 아이템 레벨: {ListingItemLevels.Label(listing.ItemLevel)}");
        if (listing.Owner != ListingOwner.Self) return;
        ImGui.SameLine();
        ImGui.BeginDisabled(plugin.RuleDraft != null || plugin.ObjectiveTrace.Busy || !listing.IsLive || !listing.ItemLevel.HasValue ||
            plugin.Refresh.Busy || plugin.ListingBusy || plugin.JobDraft != null || plugin.DescriptionDraft != null ||
            plugin.ObjectiveDraft != null || plugin.BeginnerDraft != null || plugin.UniqueJobsDraft != null ||
            plugin.PrivateDraft != null || plugin.CompletionDraft != null || !ListingSlotRoute.SupportsSettings(listing.Slots));
        if (ImGui.SmallButton("평균 아이템 레벨 설정")) plugin.RequestItemLevelEdit();
        ImGui.EndDisabled();
    }

    private void DrawItemLevelEditor()
    {
        const string popup = "평균 아이템 레벨 설정##PartyViewKR-item-level-editor";
        var draft = plugin.ItemLevelDraft;
        if (draft == null)
        {
            if (shownItemLevelDraft != null)
            {
                var closing = true;
                if (ImGui.BeginPopupModal(popup, ref closing)) { ImGui.CloseCurrentPopup(); ImGui.EndPopup(); }
            }
            shownItemLevelDraft = null;
            return;
        }
        if (!ReferenceEquals(draft, shownItemLevelDraft)) { shownItemLevelDraft = draft; ImGui.OpenPopup(popup); }
        var open = true;
        if (ImGui.BeginPopupModal(popup, ref open, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.TextUnformatted($"현재 조건: {ListingItemLevels.Label(ListingItemLevels.Read(draft.Original.ExtraCriteria))}");
            var enabled = draft.Enabled;
            if (ImGui.Checkbox("평균 아이템 레벨 설정", ref enabled)) draft.Enabled = enabled;
            ImGui.BeginDisabled(!draft.Enabled);
            ImGui.SetNextItemWidth(180);
            var level = draft.LevelText;
            // Keep invalid/overlong input visible instead of silently clamping
            // or truncating it into a different recruitment requirement.
            if (ImGui.InputText("아이템 레벨 (1~999)", ref level, 32, ImGuiInputTextFlags.CharsDecimal)) draft.LevelText = level;
            ImGui.EndDisabled();
            if (!draft.Enabled) ImGui.TextUnformatted("평균 아이템 레벨 제한을 해제합니다.");
            else if (!ListingItemLevels.TryLevel(draft.LevelText, out _)) ImGui.TextDisabled("1~999 사이의 정수를 입력해 주세요.");
            var edit = draft.Request;
            if (edit.Value.HasValue && !edit.Changed) ImGui.TextDisabled("체크 상태나 숫자를 변경하면 적용할 수 있습니다.");
            ImGui.Separator();
            ImGui.TextUnformatted("변경 목록에 저장한 뒤 메인 창에서 한 번에 적용합니다.");
            ImGui.BeginDisabled(!plugin.CanQueue(edit) || plugin.Refresh.Busy || plugin.ListingBusy || plugin.Refresh.CoolingDown(Environment.TickCount64));
            if (ImGui.Button("변경 목록에 저장", new Vector2(420, 0)) && plugin.ApplyItemLevelDraft()) ImGui.CloseCurrentPopup();
            ImGui.EndDisabled();
            ImGui.EndPopup();
        }
        if (!open) plugin.CloseItemLevelDraft();
    }

    private void DrawRule(ListingSnapshot listing, ListingRuleKind kind)
    {
        var value = listing.RuleValue(kind);
        ImGui.TextUnformatted($"{ListingRules.Title(kind)}: {ListingRules.Label(kind, value)}");
        if (listing.Owner != ListingOwner.Self) return;
        ImGui.SameLine();
        ImGui.BeginDisabled(plugin.ObjectiveTrace.Busy || !listing.IsLive || !value.HasValue || plugin.Refresh.Busy || plugin.ListingBusy ||
            plugin.RuleDraft != null || plugin.ItemLevelDraft != null || plugin.CompletionDraft != null || plugin.JobDraft != null ||
            plugin.DescriptionDraft != null || plugin.ObjectiveDraft != null || plugin.BeginnerDraft != null || plugin.UniqueJobsDraft != null ||
            plugin.PrivateDraft != null || !ListingSlotRoute.SupportsSettings(listing.Slots));
        if (ImGui.SmallButton($"{ListingRules.Title(kind)} 설정")) plugin.RequestRuleEdit(kind);
        ImGui.EndDisabled();
    }

    private void DrawRuleEditor()
    {
        const string popup = "모집 규칙 설정##PartyViewKR-rule-editor";
        var draft = plugin.RuleDraft;
        if (draft == null)
        {
            if (shownRuleDraft != null)
            {
                var closing = true;
                if (ImGui.BeginPopupModal(popup, ref closing)) { ImGui.CloseCurrentPopup(); ImGui.EndPopup(); }
            }
            shownRuleDraft = null;
            return;
        }
        if (!ReferenceEquals(draft, shownRuleDraft)) { shownRuleDraft = draft; ImGui.OpenPopup(popup); }
        var open = true;
        if (ImGui.BeginPopupModal(popup, ref open, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.TextUnformatted(ListingRules.Title(draft.Kind));
            ImGui.TextUnformatted($"현재 설정: {ListingRules.Label(draft.Kind, ListingRules.Read(draft.Original.Criteria, draft.Kind))}");
            if (draft.Kind == ListingRuleKind.Loot)
            {
                var selected = draft.SelectedValue;
                ImGui.SetNextItemWidth(220);
                if (ImGui.Combo("전리품 규칙", ref selected, ListingRules.LootLabels, ListingRules.LootLabels.Length)) draft.SelectedValue = selected;
            }
            else
            {
                var enabled = draft.SelectedValue == 1;
                if (ImGui.Checkbox(ListingRules.Title(draft.Kind), ref enabled)) draft.SelectedValue = enabled ? 1 : 0;
            }
            var edit = draft.Request;
            if (!edit.Changed) ImGui.TextDisabled("설정을 변경하면 적용할 수 있습니다.");
            ImGui.TextUnformatted("게임 수정창에서 선택할 수 있는 항목만 적용합니다.");
            ImGui.Separator();
            ImGui.TextUnformatted("변경 목록에 저장한 뒤 메인 창에서 한 번에 적용합니다.");
            ImGui.BeginDisabled(!plugin.CanQueue(edit) || plugin.Refresh.Busy || plugin.ListingBusy || plugin.Refresh.CoolingDown(Environment.TickCount64));
            if (ImGui.Button("변경 목록에 저장", new Vector2(420, 0)) && plugin.ApplyRuleDraft()) ImGui.CloseCurrentPopup();
            ImGui.EndDisabled();
            ImGui.EndPopup();
        }
        if (!open) plugin.CloseRuleDraft();
    }

    private void DrawObjectiveTrace()
    {
        if (!ImGui.CollapsingHeader("목적 선택 진단")) return;
        ImGui.TextWrapped("게임의 모집 수정창을 직접 열고 기록을 시작한 뒤, 목적 드롭다운에서 아래 순서로 선택해 주세요.");
        ImGui.TextWrapped("설정 안 함 → 완료 목적 → 연습 → 반복 공략 → 설정 안 함");
        ImGui.TextWrapped("각 항목에서 잠깐 기다린 뒤 다음 항목을 선택하고, 기록 종료 후 복사해 주세요. 갱신 버튼을 누를 필요는 없습니다.");
        ImGui.TextWrapped(plugin.ObjectiveTrace.Status);
        ImGui.BeginDisabled(plugin.ObjectiveTrace.Busy || plugin.Refresh.Busy || plugin.ListingBusy ||
            plugin.JobDraft != null || plugin.DescriptionDraft != null || plugin.ObjectiveDraft != null || plugin.BeginnerDraft != null || plugin.UniqueJobsDraft != null || plugin.PrivateDraft != null || plugin.CompletionDraft != null || plugin.RuleDraft != null || plugin.ItemLevelDraft != null);
        if (ImGui.Button("목적 기록 시작")) plugin.ObjectiveTrace.RequestStart();
        ImGui.EndDisabled();
        ImGui.SameLine();
        ImGui.BeginDisabled(!plugin.ObjectiveTrace.Busy);
        if (ImGui.Button("기록 종료")) plugin.ObjectiveTrace.RequestStop();
        ImGui.EndDisabled();
        ImGui.SameLine();
        if (ImGui.Button("목적 기록 복사")) ImGui.SetClipboardText(plugin.Diagnostic);
    }

    private void DrawObjectiveEditor()
    {
        const string popup = "모집 목적 수정##PartyViewKR-objective-editor";
        var draft = plugin.ObjectiveDraft;
        if (draft == null)
        {
            if (shownObjectiveDraft != null)
            {
                var closing = true;
                if (ImGui.BeginPopupModal(popup, ref closing)) { ImGui.CloseCurrentPopup(); ImGui.EndPopup(); }
            }
            shownObjectiveDraft = null;
            return;
        }
        if (!ReferenceEquals(draft, shownObjectiveDraft)) { shownObjectiveDraft = draft; ImGui.OpenPopup(popup); }
        var open = true;
        if (ImGui.BeginPopupModal(popup, ref open, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.TextUnformatted($"현재 목적: {ListingObjectives.Label(ListingObjectives.Read(draft.Original.Criteria))}");
            ImGui.SetNextItemWidth(340);
            if (ImGui.BeginCombo("##objective-choice", ListingObjectives.Label(ListingObjectives.Value(draft.SelectedIndex))))
            {
                for (var i = 0; i < ListingObjectives.Names.Length; i++)
                {
                    var selected = draft.SelectedIndex == i;
                    if (ImGui.Selectable(ListingObjectives.Names[i], selected)) draft.SelectedIndex = i;
                    if (selected) ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }
            var edit = draft.Request;
            if (!edit.Changed) ImGui.TextDisabled("다른 목적을 선택하면 적용할 수 있습니다.");
            ImGui.Separator();
            ImGui.TextUnformatted("변경 목록에 저장한 뒤 메인 창에서 한 번에 적용합니다.");
            ImGui.BeginDisabled(!ListingObjectives.AutomaticEditingAvailable || !plugin.CanQueue(edit) || plugin.Refresh.Busy || plugin.ListingBusy || plugin.Refresh.CoolingDown(Environment.TickCount64));
            if (ImGui.Button("변경 목록에 저장", new Vector2(340, 0)) && plugin.ApplyObjectiveDraft()) ImGui.CloseCurrentPopup();
            ImGui.EndDisabled();
            ImGui.EndPopup();
        }
        if (!open) plugin.CloseObjectiveDraft();
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
            ImGui.BeginDisabled(plugin.ObjectiveTrace.Busy || plugin.Refresh.Busy || plugin.ListingBusy);
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
            ImGui.TextUnformatted($"{draft.Original.Slots.PositionLabel(draft.Original.Slots.Slots[draft.Position - 1])}번 빈자리 · {(draft.Changed ? "변경 예정" : "현재 허용 직업")}");
            ImGui.TextUnformatted("변경 목록에 저장한 뒤 메인 창에서 한 번에 적용합니다.");
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
            ImGui.BeginDisabled(!plugin.CanQueue(draft.Request) || plugin.Refresh.Busy || plugin.ListingBusy || plugin.Refresh.CoolingDown(Environment.TickCount64));
            if (ImGui.Button("변경 목록에 저장") && plugin.ApplyJobDraft()) ImGui.CloseCurrentPopup();
            ImGui.EndDisabled();
            ImGui.EndPopup();
        }
        if (!open) plugin.CloseJobDraft();
    }

    private static void Cell(string text) { ImGui.TableNextColumn(); ImGui.TextUnformatted(text); }
}
