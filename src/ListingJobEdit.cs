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

// Each UI toggle is dispatched once. Observe its result before choosing the
// next one, so a delayed callback or manual interference cannot invert it twice.
internal sealed class JobToggleSequence(ulong initial, ulong desired, IReadOnlyList<RecruitmentJobOption> jobs)
{
    internal ulong Expected { get; private set; } = initial;
    private ulong previous = initial;
    private bool pending;
    private long sentAt;

    internal bool Next(ulong observed, long now, out RecruitmentJobOption? next, out bool complete, out string error)
    {
        next = null; complete = false; error = "";
        if (pending && observed == previous && observed != Expected)
        {
            if (now - sentAt > 2500) error = "직업 선택 결과를 확인하지 못했습니다.";
            return error.Length == 0;
        }
        if (observed != Expected) { error = "게임 직업 선택이 도중에 변경되어 중단했습니다."; return false; }
        pending = false;
        // Add new choices first so switching a single allowed job never creates
        // an intermediate selection with no jobs in the game's editor.
        next = jobs.FirstOrDefault(job => (desired & job.Flag) != 0 && (observed & job.Flag) == 0) ??
            jobs.FirstOrDefault(job => (desired & job.Flag) == 0 && (observed & job.Flag) != 0);
        if (next == null) { complete = observed == desired; return complete; }
        previous = observed;
        Expected = observed ^ next.Flag;
        sentAt = now;
        pending = true;
        return true;
    }
}
