using System.Globalization;

namespace PartyViewKR;

internal enum ListingRuleKind { Unrestricted, MinimumItemLevel, SilenceEcho, Loot }

internal static class ListingRules
{
    // Values from the supplied KR structure DLL, included in the native contract.
    internal static byte Mask(ListingRuleKind kind) => kind switch {
        ListingRuleKind.Unrestricted => 1, ListingRuleKind.MinimumItemLevel => 2,
        ListingRuleKind.SilenceEcho => 4, _ => 0
    };
    internal static string Id(ListingRuleKind kind) => kind switch {
        ListingRuleKind.Unrestricted => "unrestricted", ListingRuleKind.MinimumItemLevel => "minimum_item_level",
        ListingRuleKind.SilenceEcho => "silence_echo", ListingRuleKind.Loot => "loot", _ => "unknown"
    };
    internal static string Title(ListingRuleKind kind) => kind switch {
        ListingRuleKind.Unrestricted => "제한 해제", ListingRuleKind.MinimumItemLevel => "최저 아이템 레벨로 참가",
        ListingRuleKind.SilenceEcho => "초월하는 힘 무효화", ListingRuleKind.Loot => "전리품 규칙", _ => "확인 불가"
    };
    internal static readonly string[] LootLabels = ["일반", "선입찰 금지", "파티장 분배"];
    internal static bool ValidValue(ListingRuleKind kind, int value) => kind == ListingRuleKind.Loot ?
        value is >= 0 and <= 2 : Mask(kind) != 0 && value is 0 or 1;
    internal static string Label(ListingRuleKind kind, int? value) => value.HasValue && ValidValue(kind, value.Value) ?
        kind == ListingRuleKind.Loot ? LootLabels[value.Value] : value == 1 ? "사용" : "사용 안 함" : "확인 불가";
    internal static int? Read(string criteria, ListingRuleKind kind)
    {
        var fields = criteria.Split('/');
        var index = kind == ListingRuleKind.Loot ? 6 : 5;
        if (fields.Length != 9 || (Mask(kind) == 0 && kind != ListingRuleKind.Loot) ||
            !byte.TryParse(fields[index], NumberStyles.None, CultureInfo.InvariantCulture, out var raw)) return null;
        return kind == ListingRuleKind.Loot ? raw <= 2 ? raw : null : (raw & Mask(kind)) != 0 ? 1 : 0;
    }
    internal static string WithValue(string criteria, ListingRuleKind kind, int value)
    {
        if (!ValidValue(kind, value) || !Read(criteria, kind).HasValue) throw new InvalidDataException("invalid_rule_edit");
        var fields = criteria.Split('/');
        if (kind == ListingRuleKind.Loot) fields[6] = value.ToString(CultureInfo.InvariantCulture);
        else
        {
            var flags = byte.Parse(fields[5], CultureInfo.InvariantCulture);
            // Only the selected bit may change, including when other/unknown bits are present.
            fields[5] = (value == 1 ? (byte)(flags | Mask(kind)) : (byte)(flags & ~Mask(kind))).ToString(CultureInfo.InvariantCulture);
        }
        return string.Join('/', fields);
    }
}

internal sealed class ListingRuleDraft(RefreshTarget original, ListingRuleKind kind)
{
    internal RefreshTarget Original { get; } = original;
    internal ListingRuleKind Kind { get; } = kind;
    internal int SelectedValue { get; set; } = ListingRules.Read(original.Criteria, kind) ?? -1;
    internal ListingRuleEdit Request => new(Original, Kind, SelectedValue);
}

internal sealed record ListingRuleEdit(RefreshTarget Original, ListingRuleKind Kind, int Value)
{
    internal bool Changed => ListingRules.Read(Original.Criteria, Kind) is { } original && original != Value;
    internal static bool Supports(RefreshTarget target, ListingRuleKind kind) => ListingJobEdit.SupportsSettings(target) && ListingRules.Read(target.Criteria, kind).HasValue;
    internal bool Valid => Supports(Original, Kind) && ListingRules.ValidValue(Kind, Value) && Changed;
    internal bool MatchesOriginal(RefreshTarget current) => Valid && Supports(current, Kind) && current.ListingId == Original.ListingId &&
        RefreshEvidence.SameRecruitment(Original, current) && ListingJobEdit.SameSlots(Original.Slots, current.Slots);
    internal RefreshTarget Expected(RefreshTarget before)
    {
        if (!Valid || !Supports(before, Kind)) throw new InvalidDataException("invalid_rule_edit");
        return before with { Criteria = ListingRules.WithValue(before.Criteria, Kind, Value) };
    }
    internal bool MatchesResult(RefreshTarget before, RefreshTarget after) => Valid && Supports(before, Kind) && Supports(after, Kind) &&
        RefreshEvidence.SameRecruitment(Expected(before), after) && ListingJobEdit.SameSlots(before.Slots, after.Slots);
}
