namespace PartyViewKR;

// A managed copy of the recruitment detail arrays. These jobs come from
// LastViewedListing, never from the current party or the player's current job.
internal sealed record ListingSlot(int Position, ulong MemberContentId, byte JobId,
    string Job, ulong SlotFlags)
{
    // Position is the visible row; friendly matches retain holes in the native arrays.
    internal int NativeIndex { get; init; } = Position - 1;
    internal bool HasMember => MemberContentId != 0 || JobId != 0;
    internal ListingAllowedJobs AllowedJobs { get; init; } = ListingAllowedJobs.Unavailable;
}

internal sealed record ListingSlots(bool Readable, int TotalSlots, int SlotsFilled,
    int NumberOfParties, bool IsAlliance, ListingSlot[] Slots, string Diagnostic)
{
    internal ListingJobRules JobRules { get; init; } = ListingJobRules.Unavailable;
    internal int ObservedMembers => Slots.Count(s => s.HasMember);
    internal bool CountsAgree => Readable && ObservedMembers == SlotsFilled;
    internal bool IsFriendlyMatch => IsFriendlyMatchShape(TotalSlots, NumberOfParties, IsAlliance);
    internal int NativeSlotCount => IsFriendlyMatch ? 24 : TotalSlots;
    internal bool MappingValid => Readable && Slots.Length == TotalSlots &&
        Slots.Select((slot, i) => slot.Position == i + 1 &&
            slot.NativeIndex == NativeIndexAt(i, IsFriendlyMatch)).All(valid => valid);

    internal static bool IsFriendlyMatchShape(int total, int parties, bool alliance) =>
        total == 18 && parties == 3 && alliance;

    private static int NativeIndexAt(int index, bool friendly) => !friendly ? index :
        index < 5 ? index : index < 10 ? index + 3 : index + 6;

    internal string PositionLabel(ListingSlot slot) => IsFriendlyMatch
        ? $"{(slot.NativeIndex < 8 ? "별빛팀" : slot.NativeIndex < 16 ? "그림자팀" : "관전")} {slot.NativeIndex % 8 + 1}"
        : slot.Position.ToString();

    internal bool MatchesMembers(ReadOnlySpan<ulong> members)
    {
        if (!MappingValid || members.Length < NativeSlotCount) return false;
        foreach (var slot in Slots)
            if (members[slot.NativeIndex] != slot.MemberContentId) return false;
        if (IsFriendlyMatch)
            foreach (var index in new[] { 5, 6, 7, 13, 14, 15 })
                if (members[index] != 0) return false;
        return true;
    }

    internal static ListingSlots Unavailable(string reason) =>
        new(false, 0, 0, 0, false, [], $"listingSlotsReadable=False; listingSlotsReason={reason}");

    internal static ListingSlots Read(bool detailAgrees, int totalSlots, int slotsFilled,
        int numberOfParties, bool isAlliance, ReadOnlySpan<byte> jobs,
        ReadOnlySpan<ulong> memberContentIds, ReadOnlySpan<ulong> slotFlags, Func<uint, string> jobName,
        ListingJobRules? jobRules = null)
    {
        if (!detailAgrees) return Unavailable("detail_ui_mismatch");
        if (totalSlots is < 1 or > 48 || slotsFilled < 0 || slotsFilled > totalSlots)
            return Unavailable("invalid_slot_count");
        var friendly = IsFriendlyMatchShape(totalSlots, numberOfParties, isAlliance);
        var nativeCount = friendly ? 24 : totalSlots;
        if (jobs.Length < nativeCount || memberContentIds.Length < nativeCount || slotFlags.Length < nativeCount)
            return Unavailable("incomplete_arrays");
        // KR traces: teams use 0..4 and 8..12; spectators use 16..23.
        // A different layout must not silently lose occupied or recruitable slots.
        if (friendly)
            foreach (var index in new[] { 5, 6, 7, 13, 14, 15 })
                if (jobs[index] != 0 || memberContentIds[index] != 0 || slotFlags[index] != 0)
                    return Unavailable($"friendly_padding_not_empty; nativeIndex={index}");

        jobRules ??= ListingJobRules.Unavailable;
        var slots = new ListingSlot[totalSlots];
        for (var i = 0; i < totalSlots; i++)
        {
            var nativeIndex = NativeIndexAt(i, friendly);
            var job = jobs[nativeIndex];
            // Do not filter empty slots, sort by live party order, or replace
            // missing job IDs with current jobs. Preserve the listing's positions.
            slots[i] = new(i + 1, memberContentIds[nativeIndex], job,
                job == 0 ? "" : jobName(job), slotFlags[nativeIndex])
            {
                NativeIndex = nativeIndex,
                AllowedJobs = memberContentIds[nativeIndex] == 0 && job == 0
                    ? jobRules.Describe(slotFlags[nativeIndex]) : ListingAllowedJobs.Unavailable,
            };
        }
        var observed = slots.Count(s => s.HasMember);
        var diagnostic = $"listingSlotsReadable=True; listingSlotCount={totalSlots}; listingSlotsFilled={slotsFilled}; " +
            $"listingNumberOfParties={numberOfParties}; listingIsAlliance={isAlliance}; " +
            $"listingObservedMembers={observed}; listingSlotCountsAgree={observed == slotsFilled}\n" +
            $"listingFriendlyMatch={friendly}; listingNativeSlotCount={nativeCount}; " +
            $"listingNativeSlotIndices={string.Join(",", slots.Select(s => s.NativeIndex))}\n" +
            $"listingJobIds={string.Join(",", slots.Select(s => s.JobId))}\n" +
            $"listingMemberPresent={string.Join(",", slots.Select(s => s.MemberContentId != 0 ? 1 : 0))}\n" +
            $"listingSlotFlags={string.Join(",", slots.Select(s => s.SlotFlags.ToString("X16")))}\n" +
            $"listingJobRulesComplete={jobRules.Complete}; listingRecruitmentJobCount={jobRules.Count}; " +
            $"listingAllJobsMask={jobRules.AllFlags:X16}";
        return new(true, totalSlots, slotsFilled, numberOfParties, isAlliance, slots, diagnostic) { JobRules = jobRules };
    }
}
