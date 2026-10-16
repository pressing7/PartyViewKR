using System.Globalization;

namespace PartyViewKR;

internal static class ListingBeginners
{
    internal static bool? Decode(byte value) => ListingCheckboxValues.Decode(value);
    internal static string Label(bool? value) => ListingCheckboxValues.Label(value);
    internal static bool? Read(string criteria)
    {
        var fields = criteria.Split('/');
        return fields.Length == 9 && byte.TryParse(fields[3], NumberStyles.None, CultureInfo.InvariantCulture, out var value)
            ? Decode(value) : null;
    }
}

internal sealed class ListingBeginnerDraft(RefreshTarget original)
{
    internal RefreshTarget Original { get; } = original;
    internal bool Enabled { get; set; } = ListingBeginners.Read(original.Criteria) == true;
    internal ListingBeginnerEdit Request => new(Original, Enabled);
}

internal sealed record ListingBeginnerEdit(RefreshTarget Original, bool Enabled)
{
    internal byte Value => Enabled ? (byte)1 : (byte)0;
    internal bool Changed => ListingBeginners.Read(Original.Criteria) is { } original && original != Enabled;
    internal static bool Supports(RefreshTarget target) => ListingJobEdit.SupportsSettings(target) && ListingBeginners.Read(target.Criteria).HasValue;
    internal bool Valid => Supports(Original) && Changed;
    internal bool MatchesOriginal(RefreshTarget current) => Valid && Supports(current) &&
        current.ListingId == Original.ListingId && RefreshEvidence.SameRecruitment(Original, current) &&
        ListingJobEdit.SameSlots(Original.Slots, current.Slots);

    internal RefreshTarget Expected(RefreshTarget before)
    {
        if (!Valid || !Supports(before)) throw new InvalidDataException("invalid_beginner_edit");
        var fields = before.Criteria.Split('/');
        fields[3] = Value.ToString(CultureInfo.InvariantCulture);
        return before with { Criteria = string.Join('/', fields) };
    }

    internal bool MatchesResult(RefreshTarget before, RefreshTarget after) => Valid && Supports(before) && Supports(after) &&
        RefreshEvidence.SameRecruitment(Expected(before), after) && ListingJobEdit.SameSlots(before.Slots, after.Slots);
}
