using System.Globalization;
using System.Text.Json;

namespace PartyViewKR;

internal sealed record WebJobChoice(uint Id, string Name);
internal sealed record WebEditSlot(int NativeIndex, string Position, uint[] Jobs);
internal sealed record WebEditModel(string Revision, Dictionary<string, object?> Values, string[] Supported,
    WebJobChoice[] Jobs, WebEditSlot[] Slots);
internal sealed record WebItemResult(string Key, string Title, bool? Applied, string Message);
internal sealed record WebOperation(string Id, string State, string Stage, string Message, bool EditorStarted,
    bool Submitted, WebItemResult[] Results);
internal sealed record WebRefreshProgress(bool Busy, string Stage, string Message, bool EditorStarted,
    bool Submitted, WebItemResult[] Results)
{
    internal static WebRefreshProgress From(ListingRefresh refresh) => new(refresh.Busy, refresh.Stage.ToString(), refresh.Status,
        refresh.EditorStarted, refresh.Submitted, refresh.BatchResults.Select(r =>
            new WebItemResult(r.Change.Key, r.Change.Title, r.Applied, r.Text)).ToArray());
}

internal interface IListingWebEditHost : IListingWebHost
{
    string? RefreshBlockReason { get; }
    RefreshTarget? CaptureEditTarget();
    bool StartRefresh(RefreshTarget source, ListingChange[] changes, out string error);
    WebRefreshProgress? ReadRefresh();
}

internal static class ListingWebEdit
{
    internal const int MaxBodyBytes = 65536, MaxChanges = 59;
    internal static bool SameSource(RefreshTarget a, RefreshTarget b) => ListingPendingChanges.SameSource(a, b) &&
        a.JobEditingAllowed == b.JobEditingAllowed && a.Slots.JobRules.Complete == b.Slots.JobRules.Complete &&
        a.Slots.JobRules.Choices.SequenceEqual(b.Slots.JobRules.Choices);

    internal static WebEditModel Model(RefreshTarget source, string revision)
    {
        var values = new Dictionary<string, object?> {
            ["description"] = source.Description,
            ["objective"] = ListingObjectives.Index(ListingObjectives.Read(source.Criteria)),
            ["beginner"] = ListingBeginners.Read(source.Criteria),
            ["uniqueJobs"] = ListingUniqueJobs.Read(source.ExtraCriteria),
            ["private"] = ListingPrivate.Read(source.ExtraCriteria),
            ["completion"] = ListingCompletions.Read(source.Criteria),
            ["itemLevel"] = ListingItemLevels.Read(source.ExtraCriteria)
        };
        var supported = new List<string>();
        if (ListingJobEdit.SupportsSettings(source)) supported.Add("description");
        if (ListingObjectiveEdit.Supports(source)) supported.Add("objective");
        if (ListingBeginnerEdit.Supports(source)) supported.Add("beginner");
        if (ListingUniqueJobsEdit.Supports(source)) supported.Add("uniqueJobs");
        if (ListingPrivateEdit.Supports(source)) supported.Add("private");
        if (ListingCompletionEdit.Supports(source)) supported.Add("completion");
        if (ListingItemLevelEdit.Supports(source)) supported.Add("itemLevel");
        foreach (var kind in Enum.GetValues<ListingRuleKind>()) {
            var key = ListingRules.Id(kind); values[key] = ListingRules.Read(source.Criteria, kind);
            if (ListingRuleEdit.Supports(source, kind)) supported.Add(key);
        }
        var jobs = source.Slots.JobRules;
        var canEditJobs = ListingJobEdit.Supports(source) && jobs.Complete;
        if (canEditJobs) supported.Add("job");
        return new(revision, values, supported.ToArray(),
            canEditJobs ? jobs.Choices.Select(j => new WebJobChoice(j.Id, j.Name)).ToArray() : [],
            canEditJobs ? source.Slots.Slots.Where(s => !s.HasMember).Select(s => new WebEditSlot(s.NativeIndex,
                source.Slots.PositionLabel(s), jobs.Choices.Where(j => (s.SlotFlags & j.Flag) != 0).Select(j => j.Id).ToArray())).ToArray() : []);
    }

    // A strict allowlist of managed changes. Browser data cannot name callbacks,
    // game nodes, raw bit masks, memory addresses, or occupied/unregistered slots.
    internal static bool Decode(ReadOnlyMemory<byte> body, RefreshTarget source, string revision,
        out ListingChange[] changes, out string error)
    {
        // Never include supplied text/PIN in errors or serialize the request.
        changes = []; error = "변경 내용을 확인해 주세요.";
        try
        {
            if (body.Length is 0 or > MaxBodyBytes) return false;
            using var document = JsonDocument.Parse(body, new JsonDocumentOptions { MaxDepth = 8 });
            var root = document.RootElement;
            if (!Keys(root, "revision", "changes") || root.GetProperty("changes").ValueKind != JsonValueKind.Array) return false;
            if (root.GetProperty("revision").GetString() != revision)
            { error = "모집 정보가 바뀌었습니다. 현재 정보로 다시 편집해 주세요."; return false; }
            var inputs = root.GetProperty("changes");
            if (inputs.GetArrayLength() > MaxChanges) return false;
            var result = new List<ListingChange>(); var seen = new HashSet<string>(StringComparer.Ordinal);
            var supported = Model(source, revision).Supported;
            foreach (var input in inputs.EnumerateArray())
            {
                if (input.ValueKind != JsonValueKind.Object || !input.TryGetProperty("field", out var field)) return false;
                var key = field.GetString() ?? "";
                if (!supported.Contains(key)) { error = "현재 모집 정보에서 지원하지 않는 수정 항목입니다."; return false; }
                ListingChange change;
                if (key == "job")
                {
                    if (!Keys(input, "field", "slot", "jobs")) return false;
                    var index = input.GetProperty("slot").GetInt32();
                    var position = Array.FindIndex(source.Slots.Slots, s => s.NativeIndex == index) + 1;
                    if (position == 0 || source.Slots.Slots[position - 1].HasMember) { error = "빈자리만 직업 제한을 수정할 수 있습니다."; return false; }
                    var selected = input.GetProperty("jobs");
                    if (selected.ValueKind != JsonValueKind.Array || selected.GetArrayLength() is 0 or > 64) return false;
                    var ids = new HashSet<uint>(); ulong mask = 0;
                    foreach (var idValue in selected.EnumerateArray()) {
                        var id = idValue.GetUInt32(); var job = source.Slots.JobRules.Choices.FirstOrDefault(j => j.Id == id);
                        if (job == null || !ids.Add(id)) return false;
                        mask |= job.Flag;
                    }
                    change = new(new ListingJobEdit(source, position, mask, source.Slots.JobRules));
                }
                else if (key == "private")
                {
                    if (!input.TryGetProperty("value", out var enabledValue)) return false;
                    var enabled = enabledValue.GetBoolean();
                    if (!Keys(input, enabled ? ["field", "value", "password"] : ["field", "value"])) return false;
                    var password = enabled ? input.GetProperty("password").GetString() ?? "" : "";
                    if (enabled && !ListingPrivate.TryPassword(password, out _)) { error = "비공개 설정에는 0000~9999의 숫자 네 자리를 입력해 주세요."; return false; }
                    change = new(new ListingPrivateEdit(source, enabled, password));
                }
                else
                {
                    if (!Keys(input, "field", "value")) return false;
                    var value = input.GetProperty("value");
                    object edit = key switch {
                        "description" => new ListingDescriptionEdit(source, value.GetString() ?? throw new JsonException()),
                        "objective" => new ListingObjectiveEdit(source, value.GetInt32()),
                        "beginner" => new ListingBeginnerEdit(source, value.GetBoolean()),
                        "uniqueJobs" => new ListingUniqueJobsEdit(source, value.GetBoolean()),
                        "completion" => Completion(source, value.GetInt32()),
                        "itemLevel" => ItemLevel(source, value.GetInt32()),
                        _ => Rule(source, key, value.GetInt32())
                    };
                    change = new(edit);
                    if (edit is ListingDescriptionEdit description && description.ValidationError.Length > 0)
                    { error = description.ValidationError; return false; }
                }
                if (!seen.Add(change.Key)) { error = "같은 수정 항목이 중복되어 있습니다."; return false; }
                if (!change.IsPrivatePassword && change.MatchesListing(source)) continue;
                if (!change.Valid) { error = "수정값 또는 모집 상태를 다시 확인해 주세요."; return false; }
                result.Add(change);
            }
            changes = result.ToArray(); error = ""; return true;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException or OverflowException or ArgumentException)
        { return false; }
    }
    private static bool Keys(JsonElement value, params string[] expected) => value.ValueKind == JsonValueKind.Object &&
        value.EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal).SequenceEqual(expected.Order(StringComparer.Ordinal));
    private static ListingCompletionEdit Completion(RefreshTarget source, int value) => value switch {
        1 => new(source, false, 0), 2 => new(source, true, 0), 4 => new(source, true, 1), _ => throw new JsonException()
    };
    private static ListingItemLevelEdit ItemLevel(RefreshTarget source, int value) => value is >= 0 and <= 999
        ? new(source, value != 0, value == 0 ? "" : value.ToString(CultureInfo.InvariantCulture)) : throw new JsonException();
    private static ListingRuleEdit Rule(RefreshTarget source, string key, int value)
    {
        var kind = Enum.GetValues<ListingRuleKind>().Single(k => ListingRules.Id(k) == key);
        return ListingRules.ValidValue(kind, value) ? new(source, kind, value) : throw new JsonException();
    }
}
