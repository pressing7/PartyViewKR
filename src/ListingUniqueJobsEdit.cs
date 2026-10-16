using System.Globalization;

namespace PartyViewKR;

internal static class ListingUniqueJobs
{
    // AgentLookingForGroup.JoinCondition.OnePlayerPerJob in the supplied KR
    // structure DLL. The enum and this exact bit are compatibility-checked.
    internal const byte Flag = 0x20;
    internal static bool FromFlags(byte flags) => (flags & Flag) != 0;
    internal static bool? Read(string extraCriteria) => TryFlags(extraCriteria, out var flags) ? FromFlags(flags) : null;
    internal static bool TryFlags(string extraCriteria, out byte flags)
    {
        flags = 0;
        var fields = extraCriteria.Split('/');
        return fields.Length == 4 && byte.TryParse(fields[1], NumberStyles.None, CultureInfo.InvariantCulture, out flags);
    }
    internal static string WithEnabled(string extraCriteria, bool enabled)
    {
        if (!TryFlags(extraCriteria, out var flags)) throw new InvalidDataException("invalid_join_conditions");
        var fields = extraCriteria.Split('/');
        fields[1] = (enabled ? (byte)(flags | Flag) : (byte)(flags & ~Flag)).ToString(CultureInfo.InvariantCulture);
        return string.Join('/', fields);
    }
}

internal sealed class ListingUniqueJobsDraft(RefreshTarget original)
{
    internal RefreshTarget Original { get; } = original;
    internal bool Enabled { get; set; } = ListingUniqueJobs.Read(original.ExtraCriteria) == true;
    internal ListingUniqueJobsEdit Request => new(Original, Enabled);
}

internal sealed record ListingUniqueJobsEdit(RefreshTarget Original, bool Enabled)
{
    internal bool Changed => ListingUniqueJobs.Read(Original.ExtraCriteria) is { } original && original != Enabled;
    internal static bool Supports(RefreshTarget target) => ListingJobEdit.Supports(target) && ListingUniqueJobs.Read(target.ExtraCriteria).HasValue;
    internal bool Valid => Supports(Original) && Changed;
    internal bool MatchesOriginal(RefreshTarget current) => Valid && Supports(current) &&
        current.ListingId == Original.ListingId && RefreshEvidence.SameRecruitment(Original, current) &&
        ListingJobEdit.SameSlots(Original.Slots, current.Slots);

    internal RefreshTarget Expected(RefreshTarget before)
    {
        if (!Valid || !Supports(before)) throw new InvalidDataException("invalid_unique_jobs_edit");
        return before with { ExtraCriteria = ListingUniqueJobs.WithEnabled(before.ExtraCriteria, Enabled) };
    }

    internal bool MatchesResult(RefreshTarget before, RefreshTarget after) => Valid && Supports(before) && Supports(after) &&
        RefreshEvidence.SameRecruitment(Expected(before), after) && ListingJobEdit.SameSlots(before.Slots, after.Slots);
}
