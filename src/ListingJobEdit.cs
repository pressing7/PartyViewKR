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
    internal static bool Supports(RefreshTarget target) => target.JobEditingAllowed &&
        target.Slots.TotalSlots == target.TotalSlots &&
        ListingSlotRoute.Classify(target.Slots) != RecruitmentSlotLayout.Unknown;

    internal bool Valid => Supports(Original) && Rules.Complete && Position >= 1 && Position <= Original.TotalSlots &&
        !Original.Slots.Slots[Position - 1].HasMember && Selected != 0 && (Selected & ~Rules.AllFlags) == 0 &&
        Selected != (Original.Slots.Slots[Position - 1].SlotFlags & Rules.AllFlags);

    // A slot edited by the game may normalize an unrestricted raw mask. Compare
    // the requested jobs, then retain the game's actual raw value for submission.
    internal bool MatchesSelectedFlags(ulong actual) => Rules.Complete && Selected != 0 &&
        (Selected & ~Rules.AllFlags) == 0 && (actual & Rules.AllFlags) == Selected;

    internal bool MatchesOriginal(RefreshTarget current) => Valid && Supports(current) &&
        current.ListingId == Original.ListingId && RefreshEvidence.SameRecruitment(Original, current) &&
        SameSlots(Original.Slots, current.Slots);

    internal RefreshTarget Expected(RefreshTarget fresh) => Expected(fresh, Selected);

    internal RefreshTarget Expected(RefreshTarget fresh, ulong actualFlags)
    {
        if (!Supports(fresh) || Position < 1 || Position > fresh.TotalSlots || !MatchesSelectedFlags(actualFlags))
            throw new InvalidDataException("unexpected_recruitment_job_flags");
        var fields = fresh.Criteria.Split('/');
        if (fields.Length != 9) throw new InvalidDataException("invalid_recruitment_criteria");
        var flags = Convert.FromHexString(fields[^1]);
        if (flags.Length != fresh.TotalSlots * sizeof(ulong)) throw new InvalidDataException("invalid_recruitment_slots");
        BinaryPrimitives.WriteUInt64LittleEndian(flags.AsSpan((Position - 1) * sizeof(ulong)), actualFlags);
        fields[^1] = Convert.ToHexString(flags);
        var slots = fresh.Slots.Slots.ToArray();
        slots[Position - 1] = slots[Position - 1] with { SlotFlags = actualFlags, AllowedJobs = Rules.Describe(actualFlags) };
        return fresh with { Criteria = string.Join('/', fields), Slots = fresh.Slots with { Slots = slots } };
    }

    internal bool MatchesResult(RefreshTarget before, RefreshTarget after)
    {
        if (!Valid || !Supports(before) || !Supports(after) ||
            Position > before.TotalSlots || Position > after.TotalSlots) return false;
        var raw = after.Slots.Slots[Position - 1].SlotFlags;
        if (!MatchesSelectedFlags(raw)) return false;
        // Only the selected slot can normalize. Every other slot, member, job
        // and recruitment setting must still match the bound original snapshot.
        var expected = Expected(before, raw);
        return RefreshEvidence.SameRecruitment(expected, after) && SameSlots(expected.Slots, after.Slots);
    }

    internal static bool SameSlots(ListingSlots a, ListingSlots b) => a.Readable && b.Readable && a.CountsAgree && b.CountsAgree &&
        a.TotalSlots == b.TotalSlots && a.SlotsFilled == b.SlotsFilled && a.IsAlliance == b.IsAlliance &&
        a.NumberOfParties == b.NumberOfParties && a.Slots.Length == b.Slots.Length &&
        a.Slots.Zip(b.Slots).All(pair => pair.First.Position == pair.Second.Position &&
            pair.First.MemberContentId == pair.Second.MemberContentId && pair.First.JobId == pair.Second.JobId &&
            pair.First.SlotFlags == pair.Second.SlotFlags);
}
