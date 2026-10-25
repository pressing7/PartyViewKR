using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Discord;

namespace PartyViewKR;

internal sealed record DiscordInput(ulong Id, ulong ApplicationId, ulong UserId, string Command,
    string CustomId, string[] Values, Dictionary<string, string> Fields)
{
    internal Dictionary<string, string[]> Choices { get; init; } = [];
    internal Dictionary<string, bool> Checks { get; init; } = [];
}
internal sealed record DiscordWork(bool Query, string Id, string Session, byte[]? Body, long RecoveryRevision = 0);
internal sealed record DiscordScreen(string Text, MessageComponent Components)
{
    internal bool HasControls => ListingDiscordCard.Walk(Components.Components).Any(c => c is ButtonComponent or SelectMenuComponent);
}
internal sealed record DiscordDecision(DiscordScreen? Screen = null, Modal? Modal = null,
    DiscordWork? Work = null, bool Replace = true);

// Editors, job details and complete results are separate messages with independent navigation.
// Only the shared bridge may queue native game operations.
internal sealed partial class ListingDiscordPanel(ListingWebBridge bridge, ulong applicationId, ulong ownerId,
    ListingDiscordIcons? iconCatalog = null)
{
    private const long Lifetime = 15 * 60 * 1000;
    private readonly ListingDiscordIcons icons = iconCatalog ?? new();
    private Panel? overview, editor, details, results;
    private enum PanelKind { Overview, Editor, Details, Results }
    private sealed class Panel(WebView view, PanelKind kind = PanelKind.Overview)
    {
        internal WebView View = view;
        internal readonly PanelKind Kind = kind;
        internal bool Editing => Kind == PanelKind.Editor;
        internal string Nonce = "";
        internal long Expires;
        internal int Group, Slot = -1;
        internal string Page = kind == PanelKind.Editor ? "settings" : "main", Form = "";
        internal Dictionary<string, JsonObject> Changes = new(StringComparer.Ordinal);
        internal string[] ResultPages = [];
        internal string ResultPreview = "";
        internal int ResultPage;
    }
    internal static readonly (string Key, string Label)[] Fields = [
        ("description", "소개말"), ("objective", "모집 목적"), ("beginner", "초행 및 초보자 환영"),
        ("uniqueJobs", "잡 중복 없음"), ("private", "비공개 설정"), ("completion", "완료 조건 설정"),
        ("itemLevel", "평균 아이템 레벨"), ("unrestricted", "제한 해제"),
        ("minimum_item_level", "최저 아이템 레벨로 참가"), ("silence_echo", "초월하는 힘 무효화"),
        ("loot", "전리품 규칙")
    ];
    private static readonly string[] ConditionFlags = ["beginner", "uniqueJobs"];
    private static readonly string[] RuleFlags = ["unrestricted", "minimum_item_level", "silence_echo"];
    private static readonly string[] SettingFlags = ["beginner", .. RuleFlags, "uniqueJobs"];
    private static readonly string[] JobGroups = ["방어", "회복", "근거리 공격", "원거리 물리", "원거리 마법"];
    internal bool Authorized(DiscordInput input) => ownerId != 0 && applicationId != 0 && input.UserId == ownerId && input.ApplicationId == applicationId;
    internal static string RequestId(ulong id) => new Guid(SHA256.HashData(Encoding.ASCII.GetBytes("pvkr-discord:" + id))[..16]).ToString("D");
    private static DiscordDecision Error(string message) => new(ListingDiscordCard.Message(message), Replace: false);
    private static bool Current(Panel p, WebView view, long now) => now < p.Expires &&
        p.View.Session == view.Session && p.View.Editor?.Revision == view.Editor?.Revision;

    internal DiscordDecision Handle(DiscordInput input, long now)
    {
        if (!Authorized(input)) return Error("이 개인 봇은 봇 소유자만 사용할 수 있습니다.");
        var current = bridge.ReadView();
        if (editor != null && !Current(editor, current, now)) { editor.Changes.Clear(); editor = null; }
        if (input.Command == "pvkr") {
            if (editor is { Changes.Count: > 0 }) return new(Render(editor, now, "작성 중인 편집 카드를 다시 열었습니다."), Replace: false);
            Clear();
            return current.CanQuery ? new(Work: new(true, RequestId(input.Id), current.Session, null, bridge.RecoveryRevision)) : Complete(now);
        }
        if (input.Command.Length > 0) return Error("지원하지 않는 명령입니다.");
        var parts = input.CustomId.Split(':');
        if (parts.Length is < 3 or > 4 || parts[0] != "pvkr") return Error("지원하지 않는 조작입니다.");
        var p = editor?.Nonce == parts[1] ? editor : overview?.Nonce == parts[1] ? overview :
            details?.Nonce == parts[1] ? details : results?.Nonce == parts[1] ? results : null;
        if (p == null || now >= p.Expires) return Error("이 카드는 만료됐습니다. /pvkr로 다시 열어 주세요.");
        if (p.View.Session != current.Session) { Clear(); return Error("게임 접속 상태가 바뀌었습니다. /pvkr로 다시 열어 주세요."); }
        var action = parts[2];
        if (p.Kind == PanelKind.Results) {
            if (action != "resultpage" || parts.Length != 4 || !int.TryParse(parts[3], out var page) ||
                page < 0 || page >= p.ResultPages.Length) return Error("작업 결과 카드에서는 결과만 확인할 수 있습니다.");
            p.ResultPage = page; return new(Render(p, now));
        }
        if (action == "results" && p.Kind == PanelKind.Overview && p.ResultPages.Length > 0) {
            results = new(p.View, PanelKind.Results) { ResultPages = p.ResultPages };
            return new(Render(results, now), Replace: false);
        }
        if (p.Kind == PanelKind.Overview && (action is "query" or "apply") && editor is { Changes.Count: > 0 })
            return Error("새 편집 카드에 변경사항이 있습니다. 편집 카드에서 ‘적용 및 갱신’을 눌러 주세요.");
        if (action == "query" && p.Kind == PanelKind.Overview) {
            if (!current.CanQuery) return Error(current.Reason);
            Clear(); return new(Work: new(true, RequestId(input.Id), current.Session, null, bridge.RecoveryRevision));
        }
        if (action == "group") {
            if (p.Kind == PanelKind.Overview) return Error("조회 카드에는 모든 파티를 표시합니다. 직업 상세에서 파티를 선택해 주세요.");
            if (!One(input, out var group) || !int.TryParse(group, out var index) || index < 0 || index >= (p.View.Listing?.Groups.Length ?? 0))
                return Error("파티를 다시 선택해 주세요.");
            p.Group = index; p.Slot = -1; return new(Render(p, now));
        }
        if (action == "details" && p.Kind == PanelKind.Overview) {
            if (p.View.Listing is not { SlotsReadable: true, Groups.Length: > 0 })
                return Error("자리 정보를 읽지 못했습니다. 내 모집글을 다시 조회해 주세요.");
            details = new(p.View, PanelKind.Details) { Group = p.Group };
            return new(Render(details, now), Replace: false);
        }
        // Reading a captured listing does not require an editable game state,
        // but a details nonce must never be usable to submit or alter a draft.
        if (p.Kind == PanelKind.Details) return Error("직업 상세 카드는 조회 전용입니다. 변경하려면 조회 카드의 ‘수정’을 눌러 주세요.");
        var model = p.View.Editor;
        if (model == null || model.Revision != current.Editor?.Revision) return Error("모집 정보가 바뀌었습니다. /pvkr로 현재 정보를 다시 조회해 주세요.");
        if (action is "settings" or "jobs") {
            if (action == "jobs" && !p.Editing) return Error("조회 카드의 ‘수정’에서 자리·직업을 선택해 주세요.");
            if (action == "jobs" && !model.Supported.Contains("job")) return Error("현재 모집에서 직업 제한을 수정할 수 없습니다.");
            var freshMessage = !p.Editing;
            if (freshMessage) { editor ??= new(p.View, PanelKind.Editor) { Group = p.Group }; p = editor; }
            p.Page = action; return new(Render(p, now), Replace: !freshMessage);
        }
        if (action == "apply") {
            if (!current.CanRefresh) return Error(current.RefreshReason);
            if (p.Changes.Values.Any(c => c["field"]?.GetValue<string>() == "job" && c["jobs"]!.AsArray().Count == 0)) return Error("각 자리의 허용 직업을 하나 이상 선택해 주세요.");
            var body = JsonSerializer.SerializeToUtf8Bytes(new { revision = model.Revision, changes = p.Changes.Values.ToArray() });
            Clear(); return new(Work: new(false, RequestId(input.Id), current.Session, body, bridge.RecoveryRevision));
        }
        if (!p.Editing) return Error("‘수정’을 눌러 새 편집 카드를 열어 주세요.");
        if (action == "form" && parts.Length == 4) {
            if (!FormSupported(p, parts[3])) return Error("현재 모집에서 이 항목을 수정할 수 없습니다.");
            p.Form = parts[3]; return new(Modal: BuildForm(p, p.Form));
        }
        if (action == "submit" && parts.Length == 4) {
            if (p.Form != parts[3] || !FormSupported(p, parts[3])) return Error("편집 카드에서 입력창을 다시 열어 주세요.");
            if (!ReadForm(p, parts[3], input, out var changes, out var error)) return Error(error);
            foreach (var change in changes) Set(p, change["field"]!.GetValue<string>(), change);
            p.Form = ""; p.Page = "settings";
            return new(Render(p, now));
        }
        if (action == "slot") {
            if (p.Page != "jobs" || !One(input, out var slot) || !int.TryParse(slot, out var index) || !GroupSlots(p).Any(s => s.NativeIndex == index))
                return Error("현재 파티의 빈자리를 선택해 주세요.");
            p.Slot = index; return new(Render(p, now));
        }
        if (action is "choosejobs" or "alljobs" or "preset") {
            if (p.Page != "jobs" || !model.Supported.Contains("job") || !GroupSlots(p).Any(s => s.NativeIndex == p.Slot)) return Error("직업을 수정할 빈자리를 선택해 주세요.");
            var selected = SelectedJobs(p);
            if (action == "alljobs") selected = model.Jobs.Select(j => j.Id).ToHashSet();
            else if (action == "preset") {
                if (parts.Length != 4 || parts[3] is not ("clear" or "tank" or "healer" or "damage")) return Error("직업 묶음을 다시 선택해 주세요.");
                selected = model.Jobs.Where(j => Preset(parts[3], j.Id)).Select(j => j.Id).ToHashSet();
            } else {
                if (parts.Length != 4 || !int.TryParse(parts[3], out var group) || group < 0 || group >= JobGroups.Length) return Error("직업 목록을 다시 열어 주세요.");
                var available = model.Jobs.Where(j => JobGroup(j.Id) == group).Select(j => j.Id).ToHashSet();
                var ids = new HashSet<uint>();
                foreach (var v in input.Values) if (!uint.TryParse(v, out var id) || !available.Contains(id) || !ids.Add(id)) return Error("허용 직업을 다시 선택해 주세요.");
                selected.ExceptWith(available); selected.UnionWith(ids);
            }
            Set(p, "job:" + p.Slot, new() { ["field"] = "job", ["slot"] = p.Slot,
                ["jobs"] = new JsonArray(model.Jobs.Where(j => selected.Contains(j.Id)).Select(j => (JsonNode?)JsonValue.Create(j.Id)).ToArray()) });
            return new(Render(p, now));
        }
        return Error("지원하지 않는 조작입니다. /pvkr로 다시 열어 주세요.");
    }

    internal WebReply Start(DiscordWork work, long now) => work.Query ? bridge.QueueQuery(work.Id, work.Session, now, work.RecoveryRevision) : bridge.QueueApply(work.Id, work.Session, work.Body ?? [], now, work.RecoveryRevision);
    internal WebReply Progress(DiscordWork work) => work.Query ? bridge.ReadQuery(work.Id) : bridge.ReadOperation(work.Id);
    internal DiscordDecision Complete(long now, string notice = "")
    {
        Clear(); overview = new(bridge.ReadView());
        // The bridge keeps the last operation across queries. A fresh /pvkr
        // therefore still exposes its full result without replaying the edit.
        var operation = overview.View.Operation;
        var report = operation is { State: not ("queued" or "running") } ? ListingDiscordSession.Describe(new(200, operation)) : "";
        var summary = operation != null ? ListingDiscordSession.ResultSummary(operation) : "";
        if (notice == report) notice = "";
        if (Escape(notice).Length > 700) { report = notice; notice = ""; summary = ""; }
        if (report.Length > 0) {
            overview.ResultPages = PaginateResult(report);
            overview.ResultPreview = Escape(report).Length <= 700 ? Escape(report) :
                (summary.Length > 0 ? summary + "\n" : "") + $"전체 {overview.ResultPages.Length}페이지";
        }
        return new(Render(overview, now, notice));
    }
    internal void Clear() { editor?.Changes.Clear(); editor = null; overview = null; details = null; results = null; }
    private static bool One(DiscordInput input, out string value) { value = input.Values.FirstOrDefault() ?? ""; return input.Values.Length == 1; }
    private static JsonNode? Original(Panel p, string key) => p.View.Editor!.Values.TryGetValue(key, out var value) ? JsonSerializer.SerializeToNode(value) : null;
    private static JsonNode? Value(Panel p, string key) => p.Changes.TryGetValue(key, out var change) ? change["value"] : Original(p, key);
    private static bool Enabled(Panel p, string key) => Value(p, key)?.ToString() is "true" or "1";
    private static JsonObject FlagChange(string key, bool enabled) => new() { ["field"] = key, ["value"] = ConditionFlags.Contains(key) ? JsonValue.Create(enabled) : JsonValue.Create(enabled ? 1 : 0) };
    private static void Set(Panel p, string key, JsonObject change)
    {
        var unchanged = key.StartsWith("job:", StringComparison.Ordinal) ?
            p.View.Editor!.Slots.First(s => s.NativeIndex == change["slot"]!.GetValue<int>()).Jobs.Order().SequenceEqual(change["jobs"]!.AsArray().Select(j => j!.GetValue<uint>()).Order()) :
            !change.ContainsKey("password") && JsonNode.DeepEquals(Original(p, key), change["value"]);
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
        change["jobs"]!.AsArray().Select(j => j!.GetValue<uint>()).ToHashSet() : p.View.Editor!.Slots.First(s => s.NativeIndex == p.Slot).Jobs.ToHashSet();
    // Classes use the same role as their recruitment jobs; preserve the shared
    // job order within each group and when serializing the complete selection.
    private static int JobGroup(uint id) => id switch {
        19 or 21 or 32 or 37 or 1 or 3 => 0,
        24 or 33 or 28 or 40 or 6 => 1,
        20 or 22 or 30 or 34 or 39 or 41 or 43 or 2 or 4 or 29 => 2,
        23 or 31 or 38 or 5 => 3,
        25 or 27 or 35 or 42 or 36 or 7 or 26 => 4,
        _ => -1
    };
    private static bool Preset(string name, uint id) => name switch {
        "tank" => JobGroup(id) == 0, "healer" => JobGroup(id) == 1,
        "damage" => JobGroup(id) is 2 or 3 or 4, _ => false
    };
    private static string Id(Panel p, string action) => "pvkr:" + p.Nonce + ":" + action;
    private static string Label(string key) => Fields.FirstOrDefault(f => f.Key == key).Label ?? key;
    private static string DisplayValue(Panel p, string key, bool original = false)
    {
        var value = original ? Original(p, key) : Value(p, key);
        if (key == "description") return value?.GetValue<string>() is { Length: > 0 } text ? text : "(없음)";
        if (key == "itemLevel") return value?.ToString() == "0" ? "제한 없음" : value?.ToString() ?? "확인 불가";
        var selected = value is JsonValue j && j.TryGetValue<bool>(out var boolean) ? (boolean ? "1" : "0") : value?.ToString();
        if (key == "private") return selected == "1" ? "비공개" : "공개";
        return Options(key).FirstOrDefault(o => o.Value == selected).Label ?? "확인 불가";
    }
    private static (string Value, string Label)[] Options(string key) => key switch {
        "objective" => [("0", "설정 안 함"), ("1", "완료 목적"), ("2", "연습"), ("3", "반복 공략")],
        "completion" => [("1", "설정 안 함"), ("2", "공략 완료"), ("4", "미완료")],
        "loot" => [("0", "일반"), ("1", "선입찰 금지"), ("2", "파티장 분배")],
        "beginner" or "uniqueJobs" or "private" or "unrestricted" or "minimum_item_level" or "silence_echo" => [("1", "✅"), ("0", "❌")], _ => []
    };
    private static string Escape(string value)
    {
        var result = new StringBuilder();
        foreach (var c in value) { if (c is '\\' or '*' or '_' or '~' or '`' or '|' or '>' or '[' or ']' or '#') result.Append('\\'); result.Append(c); }
        return result.ToString();
    }
    internal static string Limit(string text, int max = 1900)
    {
        if (text.Length <= max) return text;
        var length = max - 1;
        if (char.IsHighSurrogate(text[length - 1])) length--;
        return text[..length] + "…";
    }
}
