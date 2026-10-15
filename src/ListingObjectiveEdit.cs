using System.Globalization;

namespace PartyViewKR;

internal static class ListingObjectives
{
    // KR dropdown order confirmed from the user's LookingForGroupCondition UI.
    // UI indices are 0..3; AgentLookingForGroup.Objective stores 1,2,4,8.
    // The command's selection is separate: see CallbackSelection below.
    internal static readonly string[] Names = ["설정 안 함", "완료 목적", "연습", "반복 공략"];
    internal static byte? Value(int index) => index is >= 0 and <= 3 ? (byte)(1 << index) : null;
    internal static int Index(byte? value) => value switch { 1 => 0, 2 => 1, 4 => 2, 8 => 3, _ => -1 };
    internal static string Label(byte? value)
    {
        var index = Index(value);
        return index >= 0 ? Names[index] : "확인 불가";
    }
    internal static byte? Read(string criteria)
    {
        var fields = criteria.Split('/');
        return fields.Length == 9 && byte.TryParse(fields[2], NumberStyles.None, CultureInfo.InvariantCulture, out var value) &&
            Index(value) >= 0 ? value : null;
    }
}

internal sealed class ListingObjectiveDraft(RefreshTarget original)
{
    internal RefreshTarget Original { get; } = original;
    internal int SelectedIndex { get; set; } = ListingObjectives.Index(ListingObjectives.Read(original.Criteria));
    internal ListingObjectiveEdit Request => new(Original, SelectedIndex);
}

internal sealed record ListingObjectiveEdit(RefreshTarget Original, int SelectedIndex)
{
    internal byte? Value => ListingObjectives.Value(SelectedIndex);
    // KR 0.3.15 report: command selection 3 stored Practice (4), not Loot (8).
    // Convert the zero-based UI index to the command's one-based selection.
    // Explicit None must use 1 as well; 0 is not a selectable item here.
    internal uint CallbackSelection => SelectedIndex is >= 0 and <= 3
        ? (uint)SelectedIndex + 1 : throw new InvalidDataException("invalid_objective_selection");
    internal bool Changed => Value.HasValue && ListingObjectives.Read(Original.Criteria) is { } original && original != Value;
    internal static bool Supports(RefreshTarget target) => ListingJobEdit.Supports(target) && ListingObjectives.Read(target.Criteria).HasValue;
    internal bool Valid => Supports(Original) && Changed;

    internal bool MatchesOriginal(RefreshTarget current) => Valid && Supports(current) &&
        current.ListingId == Original.ListingId && RefreshEvidence.SameRecruitment(Original, current) &&
        ListingJobEdit.SameSlots(Original.Slots, current.Slots);

    internal RefreshTarget Expected(RefreshTarget before)
    {
        if (!Valid || !Supports(before)) throw new InvalidDataException("invalid_objective_edit");
        var fields = before.Criteria.Split('/');
        fields[2] = Value!.Value.ToString(CultureInfo.InvariantCulture);
        return before with { Criteria = string.Join('/', fields) };
    }

    internal bool MatchesResult(RefreshTarget before, RefreshTarget after) => Valid && Supports(before) && Supports(after) &&
        RefreshEvidence.SameRecruitment(Expected(before), after) && ListingJobEdit.SameSlots(before.Slots, after.Slots);
}
