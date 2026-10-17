using System.Text;
using Discord;

namespace PartyViewKR;

internal sealed partial class ListingDiscordPanel
{
    private DiscordScreen Render(Panel p, long now, string notice = "")
    {
        p.Nonce = Guid.NewGuid().ToString("N"); p.Expires = now + Lifetime; p.Form = "";
        var card = new ListingDiscordCard(p.Editing ? new Color(0xF0B232) : new Color(0x5865F2));
        card.Text(p.Kind switch { PanelKind.Editor => "## PVKR · 모집 수정", PanelKind.Details => "## PVKR · 직업 상세", _ => "## PVKR · 내 모집글" });
        if (notice.Length > 0) card.Text(Escape(Limit(notice, 700)));
        var listing = p.View.Listing;
        if (listing != null) {
            card.Text($"**{Escape(Limit(listing.Duty, 180))}**\n👥 **{listing.Filled?.ToString() ?? "?"}/{listing.Total?.ToString() ?? "?"}명**　⏱ {Remaining(listing)}\n-# 모집자 {Escape(Limit(listing.Leader, 80))}");
        } else card.Text(Escape(p.View.Reason.Length > 0 ? p.View.Reason : p.View.Status));
        if (p.Editing) {
            Pending(card, p);
            card.Row(Button(p, "apply", p.Changes.Count > 0 ? $"{p.Changes.Count}개 적용 및 갱신" : "적용 및 갱신", ButtonStyle.Success,
                !p.View.CanRefresh || p.Changes.Count == 0 || p.Changes.Values.Any(c => c["field"]?.ToString() == "job" && c["jobs"]!.AsArray().Count == 0)));
            card.Divider();
            if (p.Page == "jobs") DrawJobs(card, p); else DrawSettings(card, p);
        } else if (p.Kind == PanelKind.Details) {
            card.Divider(); GroupMenu(card, p); DrawJobDetails(card, p);
        } else {
            card.Row(Button(p, "query", "조회", disabled: !p.View.CanQuery),
                Button(p, "apply", "갱신", ButtonStyle.Success, !p.View.CanRefresh),
                Button(p, "settings", "수정", ButtonStyle.Primary, p.View.Editor == null || !p.View.CanRefresh),
                Button(p, "details", "직업 상세", disabled: listing is not { SlotsReadable: true, Groups.Length: > 0 }));
            if (listing != null) {
                card.Divider(); card.Text("### 소개말\n" + Escape(listing.Description.Length > 0 ? Limit(listing.Description, 400) : "(없음)"));
                var conditions = listing.Settings.Where(s => s.Key is "objective" or "completion" or "itemLevel");
                card.Text(string.Join(" · ", conditions.Select(s => Escape(s.Label + ": " + s.Value))));
                card.Divider(); GroupMenu(card, p);
                if (listing.Groups.Length > p.Group) {
                    var group = listing.Groups[p.Group];
                    card.Text($"### {Escape(group.Name)} · {group.Slots.Count(s => s.HasMember)}/{group.Slots.Length}명\n" + string.Join(" ", group.Slots.Select(icons.ForSlot)));
                    var legend = icons.Legend(group.Slots);
                    if (legend.Length > 0) card.Text("-# " + legend);
                } else card.Text("자리 정보를 읽지 못했습니다. 다시 조회해 주세요.");
            }
            if (!p.View.CanRefresh && p.View.RefreshReason.Length > 0) card.Text(Escape(Limit(p.View.RefreshReason, 240)));
        }
        if (listing?.ReadAt is { } at) card.Text($"-# 조회 <t:{at.ToUnixTimeSeconds()}:T> · 조회 당시의 모집 정보" + (!listing.Live ? " · 최신 정보는 다시 조회" : ""));
        return card.Build();
    }

    private static string Remaining(WebListing listing)
    {
        // Anchor to the captured read, never render time. Navigating a card must
        // not restart the countdown, and an unreadable timer must not become 60m.
        if (listing.ReadAt is { } at && RefreshEvidence.ParseSeconds(listing.Remaining) is { } seconds)
            return $"종료 예정 <t:{at.AddSeconds(seconds).ToUnixTimeSeconds()}:R>";
        return "남은 시간 " + (string.IsNullOrWhiteSpace(listing.Remaining) ? "확인 불가" : Escape(Limit(listing.Remaining, 30)));
    }

    private static void DrawJobDetails(ListingDiscordCard card, Panel p)
    {
        var groups = p.View.Listing?.Groups ?? [];
        if (p.Group < 0 || p.Group >= groups.Length) { card.Text("자리 정보를 읽지 못했습니다. 조회 카드에서 다시 조회해 주세요."); return; }
        var group = groups[p.Group];
        card.Text($"### {Escape(group.Name)} · {group.Slots.Count(s => s.HasMember)}/{group.Slots.Length}명\n-# 참가 직업은 모집에 등록된 직업입니다.");
        foreach (var slot in group.Slots) {
            var info = slot.HasMember ? "등록 직업: " + (slot.Job.Length > 0 ? Escape(slot.Job) : "확인 불가") :
                !slot.RulesComplete ? "허용 직업을 확인할 수 없습니다." : slot.Anyone ? "누구나 가능" :
                "허용 직업: " + (slot.AllowedJobs.Length > 0 ? Escape(string.Join(", ", slot.AllowedJobs)) : "없음");
            card.Text($"**{Escape(slot.Position)} · {(slot.HasMember ? "참가" : "모집 중")}**\n{info}");
        }
    }

    private static ButtonBuilder Button(Panel p, string action, string label, ButtonStyle style = ButtonStyle.Secondary, bool disabled = false) =>
        new(label, Id(p, action), style, isDisabled: disabled);
    private static void GroupMenu(ListingDiscordCard card, Panel p)
    {
        var groups = p.View.Listing?.Groups ?? [];
        if (groups.Length < 2) return;
        card.Row(new SelectMenuBuilder().WithCustomId(Id(p, "group")).WithPlaceholder("파티 선택")
            .WithOptions(groups.Select((g, i) => new SelectMenuOptionBuilder($"{g.Name} · {g.Slots.Count(s => s.HasMember)}/{g.Slots.Length}명", i.ToString(), isDefault: i == p.Group)).ToList()));
    }
    private static string Setting(Panel p, string key) =>
        $"{Label(key)}: " + (p.Changes.ContainsKey(key) ? $"{Escape(DisplayValue(p, key, true))} → **{Escape(DisplayValue(p, key))}**" : Escape(DisplayValue(p, key)));
    private static void Pending(ListingDiscordCard card, Panel p)
    {
        if (p.Changes.Count == 0) { card.Text("-# 변경할 항목을 고른 뒤 한 번에 적용하세요."); return; }
        var labels = p.Changes.Values.Select(c => c["field"]!.GetValue<string>() is "job" ?
            p.View.Editor!.Slots.First(s => s.NativeIndex == c["slot"]!.GetValue<int>()).Position + " 직업" : Label(c["field"]!.GetValue<string>()));
        card.Text($"**적용 대기 {p.Changes.Count}개**\n" + Escape(Limit(string.Join(" · ", labels), 500)));
    }
    private static void DrawSettings(ListingDiscordCard card, Panel p)
    {
        var supported = p.View.Editor!.Supported;
        if (supported.Contains("description")) {
            var content = "### 소개말\n" + Escape(Limit(DisplayValue(p, "description"), 400));
            if (p.Changes.ContainsKey("description")) content += "\n-# 변경 대기 · 기존: " + Escape(Limit(DisplayValue(p, "description", true), 160));
            card.Section(content, Button(p, "form:description", "수정")); card.Divider();
        }
        if (FormSupported(p, "conditions")) {
            card.Section("### 모집 조건\n" + string.Join('\n', FormFields("conditions").Where(supported.Contains).Select(k => Setting(p, k))), Button(p, "form:conditions", "함께 수정"));
            Toggles(card, p, ConditionFlags); card.Divider();
        }
        if (FormSupported(p, "rules")) {
            card.Section("### 참가 규칙\n" + string.Join('\n', FormFields("rules").Where(supported.Contains).Select(k => Setting(p, k))), Button(p, "form:rules", "함께 수정"));
            Toggles(card, p, RuleFlags); card.Divider();
        }
        if (supported.Contains("private")) {
            card.Section("### 비공개 설정\n" + Setting(p, "private") + (p.Changes.TryGetValue("private", out var change) && change.ContainsKey("password") ? " · 비밀번호 변경 대기" : ""),
                Button(p, "form:private", "설정")); card.Divider();
        }
        card.Section("### 자리·직업\n파티별 빈자리의 허용 직업을 수정합니다.", Button(p, "jobs", "선택", disabled: p.View.Editor.Slots.Length == 0));
    }
    private static void Toggles(ListingDiscordCard card, Panel p, string[] keys)
    {
        var buttons = keys.Where(p.View.Editor!.Supported.Contains).Select(k => Button(p, "toggle:" + k,
            (k switch { "beginner" => "초행 환영", "uniqueJobs" => "잡 중복 없음", "minimum_item_level" => "최저 템렙", "silence_echo" => "초월힘 무효", _ => "제한 해제" }) + (Enabled(p, k) ? " 끄기" : " 켜기"))).ToArray();
        if (buttons.Length > 0) card.Row(buttons);
    }
    private static void DrawJobs(ListingDiscordCard card, Panel p)
    {
        GroupMenu(card, p);
        var slots = GroupSlots(p);
        if (!slots.Any(s => s.NativeIndex == p.Slot)) p.Slot = slots.FirstOrDefault()?.NativeIndex ?? -1;
        if (slots.Length == 0) card.Text("이 파티에는 수정할 수 있는 빈자리가 없습니다.");
        else {
            card.Row(new SelectMenuBuilder().WithCustomId(Id(p, "slot")).WithPlaceholder("수정할 빈자리")
                .WithOptions(slots.Select(s => new SelectMenuOptionBuilder(s.Position + (p.Changes.ContainsKey("job:" + s.NativeIndex) ? " · 변경 대기" : ""), s.NativeIndex.ToString(), isDefault: s.NativeIndex == p.Slot)).ToList()));
            var selected = SelectedJobs(p); var jobs = p.View.Editor!.Jobs;
            card.Text($"### {Escape(slots.First(s => s.NativeIndex == p.Slot).Position)}\n" +
                (selected.Count == jobs.Length ? "**누구나 가능**" : $"**{selected.Count}개 직업 허용**\n" + Escape(Limit(string.Join(", ", jobs.Where(j => selected.Contains(j.Id)).Select(j => j.Name)), 500))));
            card.Row(Button(p, "alljobs", "전체 선택"), Button(p, "preset:clear", "선택 비우기"),
                Button(p, "preset:tank", "탱커", disabled: !jobs.Any(j => Preset("tank", j.Id))),
                Button(p, "preset:healer", "힐러", disabled: !jobs.Any(j => Preset("healer", j.Id))),
                Button(p, "preset:damage", "딜러", disabled: !jobs.Any(j => Preset("damage", j.Id))));
            card.Text("-# 탱커·힐러·딜러 버튼은 해당 잡만 선택합니다. 클래스는 아래에서 추가할 수 있습니다.");
            for (var page = 0; page < 2; page++) {
                var choices = jobs.Skip(page * 16).Take(16).ToArray(); if (choices.Length == 0) continue;
                card.Row(new SelectMenuBuilder().WithCustomId(Id(p, "choosejobs:" + page)).WithPlaceholder($"허용 직업 {page + 1}/2")
                    .WithMinValues(0).WithMaxValues(choices.Length).WithOptions(choices.Select(j => new SelectMenuOptionBuilder(j.Name, j.Id.ToString(), isDefault: selected.Contains(j.Id))).ToList()));
            }
            if (selected.Count == 0) card.Text("⚠ 허용 직업을 하나 이상 선택해야 적용할 수 있습니다.");
        }
        card.Row(Button(p, "settings", "편집 목록"));
    }
}

// All responses, including progress/errors, use V2. A V2 message cannot switch
// back to legacy content when an interaction finishes or fails.
internal sealed class ListingDiscordCard(Color color)
{
    private readonly ContainerBuilder container = new ContainerBuilder().WithAccentColor(color);
    private readonly StringBuilder text = new();
    private int remaining = 3800;
    private TextDisplayBuilder Display(string value)
    {
        value = ListingDiscordPanel.Limit(value, Math.Max(3, remaining)); remaining -= value.Length;
        text.AppendLine(value); return new(value);
    }
    internal void Text(string value) => container.AddComponent(Display(value));
    internal void Section(string value, ButtonBuilder button) => container.AddComponent(new SectionBuilder(button, [Display(value)]));
    internal void Divider() => container.AddComponent(new SeparatorBuilder());
    internal void Row(params IMessageComponentBuilder[] controls) => container.AddComponent(new ActionRowBuilder(controls));
    internal DiscordScreen Build() => new(text.ToString(), new ComponentBuilderV2([container]).Build());
    internal static DiscordScreen Message(string value) { var card = new ListingDiscordCard(new Color(0x5865F2)); card.Text(ListingDiscordPanel.Limit(value)); return card.Build(); }
    internal static IEnumerable<IMessageComponent> Walk(IEnumerable<IMessageComponent> components)
    {
        foreach (var component in components) {
            yield return component;
            IEnumerable<IMessageComponent> children = component switch {
                ContainerComponent c => c.Components, SectionComponent s => [.. s.Components, s.Accessory], ActionRowComponent r => r.Components,
                LabelComponent l => [l.Component], _ => []
            };
            foreach (var child in Walk(children)) yield return child;
        }
    }
}
