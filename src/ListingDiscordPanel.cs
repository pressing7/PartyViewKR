using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Discord;

namespace PartyViewKR;

internal sealed record DiscordInput(ulong Id, ulong ApplicationId, ulong UserId, string Command,
    string CustomId, string[] Values, Dictionary<string, string> Fields);
internal sealed record DiscordWork(bool Query, string Id, string Session, byte[]? Body);
internal sealed record DiscordScreen(string Text, MessageComponent Components);
internal sealed record DiscordDecision(DiscordScreen? Screen = null, Modal? Modal = null,
    DiscordWork? Work = null, bool Replace = true);

// Only this managed adapter knows Discord's controls. Native operations are
// still queued into ListingWebBridge and executed by the framework thread.
internal sealed class ListingDiscordPanel(ListingWebBridge bridge, ulong applicationId, ulong ownerId)
{
    private const long Lifetime = 15 * 60 * 1000;
    private Panel? panel;
    private sealed class Panel(WebView view)
    {
        internal WebView View = view;
        internal string Nonce = "";
        internal long Expires;
        internal int Group, Slot = -1;
        internal string Page = "main", Field = "";
        internal Dictionary<string, JsonObject> Changes = new(StringComparer.Ordinal);
    }
    internal static readonly (string Key, string Label)[] Fields = [
        ("description", "소개말"), ("objective", "모집 목적"), ("beginner", "초행 및 초보자 환영"),
        ("uniqueJobs", "잡 중복 없음"), ("private", "비공개 설정"), ("completion", "완료조건"),
        ("itemLevel", "평균 아이템 레벨"), ("unrestricted", "제한 해제"),
        ("minimum_item_level", "최저 아이템 레벨로 참가"), ("silence_echo", "초월하는 힘 무효화"),
        ("loot", "전리품 규칙")
    ];
    internal bool Authorized(DiscordInput input) => ownerId != 0 && applicationId != 0 &&
        input.UserId == ownerId && input.ApplicationId == applicationId;
    internal static string RequestId(ulong id) => new Guid(SHA256.HashData(Encoding.ASCII.GetBytes("pvkr-discord:" + id))[..16]).ToString("D");
    private static DiscordDecision Error(string message) => new(new(message, new ComponentBuilder().Build()), Replace: false);

    internal DiscordDecision Handle(DiscordInput input, long now)
    {
        if (!Authorized(input)) return Error("이 개인 봇은 봇 소유자만 사용할 수 있습니다.");
        var current = bridge.ReadView();
        if (input.Command == "pvkr")
        {
            if (panel is { Changes.Count: > 0 } draft && draft.View.Session == current.Session &&
                draft.View.Editor?.Revision == current.Editor?.Revision && now < draft.Expires)
                return new(Render(draft, now, "적용 대기 중인 변경사항이 있습니다."));
            panel = null;
            return current.CanQuery ? new(Work: new(true, RequestId(input.Id), current.Session, null)) : Complete(now);
        }
        if (input.Command.Length > 0) return Error("지원하지 않는 명령입니다.");
        var parts = input.CustomId.Split(':');
        var p = panel;
        if (p != null && now >= p.Expires) { Clear(); p = null; }
        if (parts.Length is < 3 or > 4 || parts[0] != "pvkr" || p == null || parts[1] != p.Nonce || now >= p.Expires)
            return Error("이 화면은 만료됐습니다. /pvkr로 다시 열어 주세요.");
        if (p.View.Session != current.Session)
        { panel = null; return Error("게임 접속 상태가 바뀌었습니다. /pvkr로 다시 열어 주세요."); }
        var action = parts[2];
        if (action == "status")
        {
            if (p.Changes.Count > 0) return Error("변경사항을 먼저 적용해 주세요.");
            return Complete(now);
        }
        if (action == "query")
        {
            if (p.Changes.Count > 0) return Error("변경사항을 먼저 적용해 주세요.");
            if (!current.CanQuery) return Error(current.Reason);
            panel = null;
            return new(Work: new(true, RequestId(input.Id), current.Session, null));
        }
        if (action == "group")
        {
            if (!One(input, out var group) || !int.TryParse(group, out var index) ||
                index < 0 || index >= (p.View.Listing?.Groups.Length ?? 0)) return Error("파티를 다시 선택해 주세요.");
            p.Group = index; p.Slot = -1;
            return new(Render(p, now));
        }
        if (action == "main") { p.Page = "main"; return new(Render(p, now)); }
        var model = p.View.Editor;
        if (model == null || model.Revision != current.Editor?.Revision)
            return Error("모집 정보가 바뀌었습니다. /pvkr로 현재 정보를 다시 조회해 주세요.");
        if (action == "apply")
        {
            if (!current.CanRefresh) return Error(current.RefreshReason);
            if (p.Changes.Values.Any(c => c["field"]?.GetValue<string>() == "job" && c["jobs"]!.AsArray().Count == 0))
                return Error("각 자리의 허용 직업을 하나 이상 선택해 주세요.");
            var body = JsonSerializer.SerializeToUtf8Bytes(new { revision = model.Revision, changes = p.Changes.Values.ToArray() });
            p.Changes.Clear(); panel = null;
            return new(Work: new(false, RequestId(input.Id), current.Session, body));
        }
        if (action == "settings") { p.Page = "settings"; return new(Render(p, now)); }
        if (action == "jobs")
        {
            if (!model.Supported.Contains("job")) return Error("현재 모집에서 직업 제한을 수정할 수 없습니다.");
            p.Page = "jobs"; return new(Render(p, now));
        }
        if (action == "field")
        {
            if (!One(input, out var field) || !model.Supported.Contains(field) || !Fields.Any(f => f.Key == field))
                return Error("수정 항목을 다시 선택해 주세요.");
            p.Field = field;
            if (field is "description" or "itemLevel") return new(Modal: BuildModal(p, field));
            p.Page = "field"; return new(Render(p, now));
        }
        if (action == "value")
        {
            if (p.Page != "field" || !One(input, out var selected) || !Options(p.Field).Any(o => o.Value == selected) ||
                !model.Supported.Contains(p.Field)) return Error("설정값을 다시 선택해 주세요.");
            if (p.Field == "private" && selected == "1") return new(Modal: BuildModal(p, "private"));
            JsonNode value = p.Field is "beginner" or "uniqueJobs" or "private" ?
                JsonValue.Create(selected == "1")! : JsonValue.Create(int.Parse(selected, CultureInfo.InvariantCulture))!;
            Set(p, p.Field, new() { ["field"] = p.Field, ["value"] = value });
            p.Page = "settings"; return new(Render(p, now, "변경사항에 담았습니다."));
        }
        if (action == "text" && parts.Length == 4)
        {
            var field = parts[3];
            if (!model.Supported.Contains(field) || field is not ("description" or "itemLevel" or "private") ||
                input.Fields.Count != 1 || !input.Fields.TryGetValue("value", out var value)) return Error("입력값을 다시 확인해 주세요.");
            if (field == "description")
            {
                var error = ListingDescriptionEdit.Validate(value);
                if (error.Length > 0) return Error(error);
                Set(p, field, new() { ["field"] = field, ["value"] = ListingDescriptionEdit.Normalize(value) });
            }
            else if (field == "itemLevel")
            {
                if (value.Length is 0 or > 3 || value.Any(c => c is < '0' or > '9') || !int.TryParse(value, out var level) || level > 999)
                    return Error("평균 아이템 레벨은 1~999, 제한 해제는 0을 입력해 주세요.");
                Set(p, field, new() { ["field"] = field, ["value"] = level });
            }
            else
            {
                if (!ListingPrivate.TryPassword(value, out _)) return Error("비밀번호는 0000~9999의 숫자 네 자리입니다.");
                Set(p, field, new() { ["field"] = field, ["value"] = true, ["password"] = value });
            }
            p.Page = "settings"; return new(Render(p, now, "변경사항에 담았습니다."));
        }
        if (action == "slot")
        {
            if (p.Page != "jobs" || !One(input, out var slot) || !int.TryParse(slot, out var index) || !GroupSlots(p).Any(s => s.NativeIndex == index))
                return Error("현재 파티의 빈자리를 선택해 주세요.");
            p.Slot = index; return new(Render(p, now));
        }
        if (action is "choosejobs" or "alljobs")
        {
            if (p.Page != "jobs" || !model.Supported.Contains("job") || !GroupSlots(p).Any(s => s.NativeIndex == p.Slot))
                return Error("직업을 수정할 빈자리를 선택해 주세요.");
            var selected = SelectedJobs(p);
            if (action == "alljobs") selected = model.Jobs.Select(j => j.Id).ToHashSet();
            else
            {
                if (parts.Length != 4 || !int.TryParse(parts[3], out var page) || page is < 0 or > 1) return Error("직업 목록을 다시 열어 주세요.");
                var available = model.Jobs.Skip(page * 16).Take(16).Select(j => j.Id).ToHashSet();
                var ids = new HashSet<uint>();
                foreach (var v in input.Values)
                    if (!uint.TryParse(v, out var id) || !available.Contains(id) || !ids.Add(id)) return Error("허용 직업을 다시 선택해 주세요.");
                selected.ExceptWith(available); selected.UnionWith(ids);
            }
            Set(p, "job:" + p.Slot, new() { ["field"] = "job", ["slot"] = p.Slot,
                ["jobs"] = new JsonArray(model.Jobs.Where(j => selected.Contains(j.Id)).Select(j => (JsonNode?)JsonValue.Create(j.Id)).ToArray()) });
            return new(Render(p, now, "직업 선택을 변경사항에 담았습니다."));
        }
        return Error("지원하지 않는 조작입니다. /pvkr로 다시 열어 주세요.");
    }

    // Called only after Discord acknowledges the interaction. A failed ACK must
    // never produce a hidden game mutation. Bridge checks identity/revision again.
    internal WebReply Start(DiscordWork work, long now) => work.Query ? bridge.QueueQuery(work.Id, work.Session, now) :
        bridge.QueueApply(work.Id, work.Session, work.Body ?? [], now);
    internal WebReply Progress(DiscordWork work) => work.Query ? bridge.ReadQuery(work.Id) : bridge.ReadOperation(work.Id);
    internal DiscordDecision Complete(long now, string notice = "")
    {
        panel = new(bridge.ReadView());
        return new(Render(panel, now, notice));
    }
    internal void Clear() { panel?.Changes.Clear(); panel = null; }
    private static bool One(DiscordInput input, out string value)
    { value = input.Values.FirstOrDefault() ?? ""; return input.Values.Length == 1; }
    private static JsonNode? Original(Panel p, string key) => p.View.Editor!.Values.TryGetValue(key, out var value) ? JsonSerializer.SerializeToNode(value) : null;
    private static JsonNode? Value(Panel p, string key) => p.Changes.TryGetValue(key, out var change) ? change["value"] : Original(p, key);
    private static void Set(Panel p, string key, JsonObject change)
    {
        var unchanged = key.StartsWith("job:", StringComparison.Ordinal) ?
            p.View.Editor!.Slots.First(s => s.NativeIndex == change["slot"]!.GetValue<int>()).Jobs.Order().SequenceEqual(change["jobs"]!.AsArray().Select(j => j!.GetValue<uint>()).Order()) :
            key != "private" && JsonNode.DeepEquals(Original(p, key), change["value"]);
        if (unchanged) p.Changes.Remove(key); else p.Changes[key] = change;
    }
    private static WebEditSlot[] GroupSlots(Panel p)
    {
        var groups = p.View.Listing?.Groups ?? [];
        if (p.Group < 0 || p.Group >= groups.Length) return [];
        var indices = groups[p.Group].Slots.Select(s => s.NativeIndex).ToHashSet();
        return p.View.Editor?.Slots.Where(s => indices.Contains(s.NativeIndex)).ToArray() ?? [];
    }
    private static HashSet<uint> SelectedJobs(Panel p) => p.Changes.TryGetValue("job:" + p.Slot, out var change) ?
        change["jobs"]!.AsArray().Select(j => j!.GetValue<uint>()).ToHashSet() :
        p.View.Editor!.Slots.First(s => s.NativeIndex == p.Slot).Jobs.ToHashSet();
    private static string Id(Panel p, string action) => "pvkr:" + p.Nonce + ":" + action;
    private static string Label(string key) => Fields.FirstOrDefault(f => f.Key == key).Label ?? key;
    private static string DisplayValue(Panel p, string key)
    {
        var value = Value(p, key);
        if (key == "itemLevel") return value?.ToString() == "0" ? "제한 없음" : value?.ToString() ?? "확인 불가";
        var selected = value is JsonValue j && j.TryGetValue<bool>(out var boolean) ? (boolean ? "1" : "0") : value?.ToString();
        return Options(key).FirstOrDefault(o => o.Value == selected).Label ?? "확인 불가";
    }
    private static (string Value, string Label)[] Options(string key) => key switch {
        "objective" => [("0", "설정 안 함"), ("1", "완료 목적"), ("2", "연습"), ("3", "반복 공략")],
        "completion" => [("1", "설정 안 함"), ("2", "공략 완료"), ("4", "미완료")],
        "loot" => [("0", "일반"), ("1", "선입찰 금지"), ("2", "파티장 분배")],
        "beginner" or "uniqueJobs" or "private" or "unrestricted" or "minimum_item_level" or "silence_echo" => [("1", "켜기"), ("0", "끄기")], _ => []
    };
    private static Modal BuildModal(Panel p, string field)
    {
        var value = field == "private" ? "" : field == "description" ? Value(p, field)?.GetValue<string>() ?? "" : Value(p, field)?.ToString() ?? "0";
        return new ModalBuilder().WithTitle(Label(field)).WithCustomId(Id(p, "text:" + field))
            .AddTextInput(field == "private" ? "비밀번호 네 자리 (0000 가능)" : field == "itemLevel" ? "1~999, 제한 없음은 0" : "소개말",
                "value", field == "description" ? TextInputStyle.Paragraph : TextInputStyle.Short,
                maxLength: field == "description" ? 192 : field == "private" ? 4 : 3,
                required: field != "description", value: value.Length > 0 ? value : null).Build();
    }
    private static string Escape(string value)
    {
        var result = new StringBuilder();
        foreach (var c in value) {
            if (c is '\\' or '*' or '_' or '~' or '`' or '|' or '>' or '[' or ']') result.Append('\\');
            result.Append(c);
        }
        return result.ToString();
    }
    internal static string Limit(string text, int max = 1900)
    {
        if (text.Length <= max) return text;
        var length = max - 1;
        if (char.IsHighSurrogate(text[length - 1])) length--;
        return text[..length] + "…";
    }
    private DiscordScreen Render(Panel p, long now, string notice = "")
    {
        p.Nonce = Guid.NewGuid().ToString("N"); p.Expires = now + Lifetime;
        var ui = new ComponentBuilder();
        var text = new StringBuilder("**PVKR · 내 모집글**\n");
        if (notice.Length > 0) text.AppendLine(Escape(notice));
        var listing = p.View.Listing;
        if (listing == null) text.AppendLine(Escape(p.View.Reason.Length > 0 ? p.View.Reason : p.View.Status));
        else {
            text.AppendLine($"{Escape(listing.Leader)} · {Escape(listing.Duty)}");
            text.AppendLine($"{listing.Filled}/{listing.Total}명 · 남은 시간 {Escape(listing.Remaining)}");
            var description = p.Changes.TryGetValue("description", out var change) ? change["value"]!.GetValue<string>() : listing.Description;
            text.AppendLine($"소개말{(p.Changes.ContainsKey("description") ? " (변경 대기)" : "")}: {Escape(description.Length > 0 ? description : "(없음)")}");
            text.AppendLine(listing.ReadAt is { } at ? $"조회 시각: <t:{at.ToUnixTimeSeconds()}:T>" : "마지막 조회 정보");
            if (!listing.Live) text.AppendLine("마지막 조회 결과입니다. 최신 내용은 모집글 조회로 확인해 주세요.");
        }
        if (p.Changes.Count > 0) {
            var labels = p.Changes.Values.Select(c => c["field"]!.GetValue<string>() is "job" ?
                p.View.Editor!.Slots.First(s => s.NativeIndex == c["slot"]!.GetValue<int>()).Position + " 직업" : Label(c["field"]!.GetValue<string>()));
            text.AppendLine($"\n**적용 대기 {p.Changes.Count}개**: {Escape(string.Join(", ", labels))}");
        }
        if (p.Page == "settings") {
            text.AppendLine("\n수정할 항목을 선택하세요. 여러 변경을 모아 적용할 수 있습니다.");
            var options = Fields.Where(f => p.View.Editor!.Supported.Contains(f.Key)).ToArray();
            if (options.Length > 0) ui.WithSelectMenu(new SelectMenuBuilder().WithCustomId(Id(p, "field")).WithPlaceholder("모집 조건 수정")
                .WithOptions(options.Select(f => new SelectMenuOptionBuilder(f.Label, f.Key)).ToList()), 0);
            ui.WithButton("직업 제한", Id(p, "jobs"), ButtonStyle.Secondary, row: 1, disabled: p.View.Editor?.Slots.Length == 0);
            ui.WithButton("적용 및 갱신", Id(p, "apply"), ButtonStyle.Success, row: 1, disabled: !p.View.CanRefresh);
            ui.WithButton("모집 정보", Id(p, "main"), ButtonStyle.Secondary, row: 1);
            foreach (var setting in listing?.Settings ?? []) text.AppendLine($"{setting.Label}: {Escape(p.Changes.ContainsKey(setting.Key) ? DisplayValue(p, setting.Key) : setting.Value)}{(p.Changes.ContainsKey(setting.Key) ? " (변경 대기)" : "")}");
        }
        else if (p.Page == "field") {
            text.AppendLine($"\n**{Label(p.Field)}**");
            var current = Value(p, p.Field);
            var selected = current is JsonValue j && j.TryGetValue<bool>(out var boolean) ? (boolean ? "1" : "0") : current?.ToString();
            ui.WithSelectMenu(new SelectMenuBuilder().WithCustomId(Id(p, "value")).WithPlaceholder(Label(p.Field))
                .WithOptions(Options(p.Field).Select(o => new SelectMenuOptionBuilder(o.Label, o.Value, isDefault: o.Value == selected)).ToList()), 0);
            ui.WithButton("설정 목록", Id(p, "settings"), ButtonStyle.Secondary, row: 1);
        }
        else if (p.Page == "jobs") {
            GroupMenu(ui, p, 0);
            var slots = GroupSlots(p);
            if (!slots.Any(s => s.NativeIndex == p.Slot)) p.Slot = slots.FirstOrDefault()?.NativeIndex ?? -1;
            if (slots.Length == 0) text.AppendLine("\n이 파티에는 수정할 수 있는 빈자리가 없습니다.");
            else {
                ui.WithSelectMenu(new SelectMenuBuilder().WithCustomId(Id(p, "slot")).WithPlaceholder("수정할 빈자리")
                    .WithOptions(slots.Select(s => new SelectMenuOptionBuilder(s.Position, s.NativeIndex.ToString(), isDefault: s.NativeIndex == p.Slot)).ToList()), 1);
                var selected = SelectedJobs(p);
                var jobs = p.View.Editor!.Jobs;
                for (var page = 0; page < 2; page++) {
                    var choices = jobs.Skip(page * 16).Take(16).ToArray();
                    if (choices.Length == 0) continue;
                    ui.WithSelectMenu(new SelectMenuBuilder().WithCustomId(Id(p, "choosejobs:" + page)).WithPlaceholder($"허용 직업 {page + 1}/2")
                        .WithMinValues(0).WithMaxValues(choices.Length)
                        .WithOptions(choices.Select(j => new SelectMenuOptionBuilder(j.Name, j.Id.ToString(), isDefault: selected.Contains(j.Id))).ToList()), page + 2);
                }
                text.AppendLine($"\n**{Escape(slots.First(s => s.NativeIndex == p.Slot).Position)}**");
                text.AppendLine(selected.Count == jobs.Length ? "누구나 가능" : "허용: " + Escape(string.Join(", ", jobs.Where(j => selected.Contains(j.Id)).Select(j => j.Name))));
                if (selected.Count == 0) text.AppendLine("허용 직업을 하나 이상 선택해야 적용할 수 있습니다.");
            }
            ui.WithButton("모든 직업", Id(p, "alljobs"), ButtonStyle.Secondary, row: 4, disabled: slots.Length == 0);
            ui.WithButton("적용 및 갱신", Id(p, "apply"), ButtonStyle.Success, row: 4, disabled: !p.View.CanRefresh);
            ui.WithButton("설정 목록", Id(p, "settings"), ButtonStyle.Secondary, row: 4);
        }
        else {
            ui.WithButton("모집글 조회", Id(p, "query"), ButtonStyle.Primary, row: 0, disabled: !p.View.CanQuery || p.Changes.Count > 0);
            ui.WithButton(p.Changes.Count > 0 ? "적용 및 갱신" : "모집글 갱신", Id(p, "apply"), ButtonStyle.Success, row: 0, disabled: !p.View.CanRefresh);
            ui.WithButton("수정", Id(p, "settings"), ButtonStyle.Secondary, row: 0, disabled: p.View.Editor == null || !p.View.CanRefresh);
            ui.WithButton("상태 확인", Id(p, "status"), ButtonStyle.Secondary, row: 0, disabled: p.Changes.Count > 0);
            GroupMenu(ui, p, 1);
            if (listing?.Groups is { Length: > 0 } groups && p.Group < groups.Length) {
                text.AppendLine("\n**" + Escape(groups[p.Group].Name) + "**");
                foreach (var slot in groups[p.Group].Slots)
                    text.AppendLine($"{Escape(slot.Position)}: {(slot.HasMember ? Escape(slot.Job) : !slot.RulesComplete ? "직업 제한 확인 불가" : slot.Anyone ? "빈자리 · 누구나 가능" : $"빈자리 · {slot.AllowedJobs.Length}개 직업 허용")}");
            }
            if (!p.View.CanRefresh && p.View.RefreshReason.Length > 0) text.AppendLine("\n" + Escape(p.View.RefreshReason));
        }
        return new(Limit(text.ToString()), ui.Build());
    }
    private static void GroupMenu(ComponentBuilder ui, Panel p, int row)
    {
        var groups = p.View.Listing?.Groups ?? [];
        if (groups.Length < 2) return;
        ui.WithSelectMenu(new SelectMenuBuilder().WithCustomId(Id(p, "group")).WithPlaceholder("파티 선택")
            .WithOptions(groups.Select((g, i) => new SelectMenuOptionBuilder(g.Name, i.ToString(), isDefault: i == p.Group)).ToList()), row);
    }
}
