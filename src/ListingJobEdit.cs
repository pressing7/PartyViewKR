using System.Buffers.Binary;

namespace PartyViewKR;

internal enum SlotEditProgress { Waiting, Complete, Failed }

// A selection is managed data only. Opening this draft never opens a game editor.
internal sealed class ListingJobDraft(RefreshTarget original, int position, ListingJobRules rules)
{
    internal RefreshTarget Original { get; } = original;
    internal int Position { get; } = position;
    internal ListingJobRules Rules { get; } = rules;
    internal ulong Selected { get; set; } = original.Slots.Slots[position - 1].SlotFlags & rules.AllFlags;
    internal bool Changed => Selected != (Original.Slots.Slots[Position - 1].SlotFlags & Rules.AllFlags);
    internal ListingJobEdit Request => new(Original, Position, Selected, Rules);
}

internal sealed record ListingJobEdit(RefreshTarget Original, int Position, ulong Selected, ListingJobRules Rules)
{
    internal static bool Supports(RefreshTarget target) => target.SinglePartyEditable &&
        target.TotalSlots is >= 1 and <= 8 && target.Slots.Readable && target.Slots.CountsAgree &&
        !target.Slots.IsAlliance && target.Slots.NumberOfParties <= 1 &&
        target.Slots.TotalSlots == target.TotalSlots && target.Slots.Slots.Length == target.TotalSlots;

    internal bool Valid => Supports(Original) && Rules.Complete && Position >= 1 && Position <= Original.TotalSlots &&
        !Original.Slots.Slots[Position - 1].HasMember && Selected != 0 && (Selected & ~Rules.AllFlags) == 0 &&
        Selected != (Original.Slots.Slots[Position - 1].SlotFlags & Rules.AllFlags);

    // Only replace understood job bits. Reserved bits are retained exactly.
    internal ulong DesiredFlags => (Original.Slots.Slots[Position - 1].SlotFlags & ~Rules.AllFlags) | Selected;

    internal bool MatchesOriginal(RefreshTarget current) => Valid && Supports(current) &&
        current.ListingId == Original.ListingId && RefreshEvidence.SameRecruitment(Original, current) &&
        SameSlots(Original.Slots, current.Slots);

    internal RefreshTarget Expected(RefreshTarget fresh)
    {
        var fields = fresh.Criteria.Split('/');
        if (fields.Length != 9) throw new InvalidDataException("invalid_recruitment_criteria");
        var flags = Convert.FromHexString(fields[^1]);
        if (flags.Length != fresh.TotalSlots * sizeof(ulong)) throw new InvalidDataException("invalid_recruitment_slots");
        BinaryPrimitives.WriteUInt64LittleEndian(flags.AsSpan((Position - 1) * sizeof(ulong)), DesiredFlags);
        fields[^1] = Convert.ToHexString(flags);
        var slots = fresh.Slots.Slots.ToArray();
        slots[Position - 1] = slots[Position - 1] with { SlotFlags = DesiredFlags, AllowedJobs = Rules.Describe(DesiredFlags) };
        return fresh with { Criteria = string.Join('/', fields), Slots = fresh.Slots with { Slots = slots } };
    }

    internal static bool SameSlots(ListingSlots a, ListingSlots b) => a.Readable && b.Readable && a.CountsAgree && b.CountsAgree &&
        a.TotalSlots == b.TotalSlots && a.SlotsFilled == b.SlotsFilled && a.IsAlliance == b.IsAlliance &&
        a.NumberOfParties == b.NumberOfParties && a.Slots.Length == b.Slots.Length &&
        a.Slots.Zip(b.Slots).All(pair => pair.First.Position == pair.Second.Position &&
            pair.First.MemberContentId == pair.Second.MemberContentId && pair.First.JobId == pair.Second.JobId &&
            pair.First.SlotFlags == pair.Second.SlotFlags);
}
