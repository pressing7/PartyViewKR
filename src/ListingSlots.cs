namespace PartyViewKR;

// A managed copy of the recruitment detail arrays. These jobs come from
// LastViewedListing, never from the current party or the player's current job.
internal sealed record ListingSlot(int Position, ulong MemberContentId, byte JobId,
    string Job, ulong SlotFlags)
{
    internal bool HasMember => MemberContentId != 0 || JobId != 0;
}

internal sealed record ListingSlots(bool Readable, int TotalSlots, int SlotsFilled,
    int NumberOfParties, bool IsAlliance, ListingSlot[] Slots, string Diagnostic)
{
    internal int ObservedMembers => Slots.Count(s => s.HasMember);
    internal bool CountsAgree => Readable && ObservedMembers == SlotsFilled;

    internal static ListingSlots Unavailable(string reason) =>
        new(false, 0, 0, 0, false, [], $"listingSlotsReadable=False; listingSlotsReason={reason}");

    internal static ListingSlots Read(bool detailAgrees, int totalSlots, int slotsFilled,
        int numberOfParties, bool isAlliance, ReadOnlySpan<byte> jobs,
        ReadOnlySpan<ulong> memberContentIds, ReadOnlySpan<ulong> slotFlags, Func<uint, string> jobName)
    {
        if (!detailAgrees) return Unavailable("detail_ui_mismatch");
        if (totalSlots is < 1 or > 48 || slotsFilled < 0 || slotsFilled > totalSlots)
            return Unavailable("invalid_slot_count");
        if (jobs.Length < totalSlots || memberContentIds.Length < totalSlots || slotFlags.Length < totalSlots)
            return Unavailable("incomplete_arrays");

        var slots = new ListingSlot[totalSlots];
        for (var i = 0; i < totalSlots; i++)
        {
            var job = jobs[i];
            // Do not filter empty slots, sort by live party order, or replace
            // missing job IDs with current jobs. Preserve the listing's positions.
            slots[i] = new(i + 1, memberContentIds[i], job,
                job == 0 ? "" : jobName(job), slotFlags[i]);
        }
        var observed = slots.Count(s => s.HasMember);
        var diagnostic = $"listingSlotsReadable=True; listingSlotCount={totalSlots}; listingSlotsFilled={slotsFilled}; " +
            $"listingNumberOfParties={numberOfParties}; listingIsAlliance={isAlliance}; " +
            $"listingObservedMembers={observed}; listingSlotCountsAgree={observed == slotsFilled}\n" +
            $"listingJobIds={string.Join(",", slots.Select(s => s.JobId))}\n" +
            $"listingMemberPresent={string.Join(",", slots.Select(s => s.MemberContentId != 0 ? 1 : 0))}\n" +
            $"listingSlotFlags={string.Join(",", slots.Select(s => s.SlotFlags.ToString("X16")))}";
        return new(true, totalSlots, slotsFilled, numberOfParties, isAlliance, slots, diagnostic);
    }
}
