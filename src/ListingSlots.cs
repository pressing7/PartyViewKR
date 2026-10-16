namespace PartyViewKR;

// A managed copy of the recruitment detail arrays. These jobs come from
// LastViewedListing, never from the current party or the player's current job.
internal sealed record ListingSlot(int Position, ulong MemberContentId, byte JobId,
    string Job, ulong SlotFlags)
{
    // Position is the visible row; groups retain unregistered tails in native arrays.
    internal int NativeIndex { get; init; } = Position - 1;
    internal bool HasMember => MemberContentId != 0 || JobId != 0;
    internal ListingAllowedJobs AllowedJobs { get; init; } = ListingAllowedJobs.Unavailable;
}

internal sealed record ListingSlots(bool Readable, int TotalSlots, int SlotsFilled,
    int NumberOfParties, bool IsAlliance, ListingSlot[] Slots, string Diagnostic)
{
    internal ListingJobRules JobRules { get; init; } = ListingJobRules.Unavailable;
    internal int ObservedMembers => Slots.Count(s => s.HasMember);
    internal bool CountsAgree => Readable && Slots.Length == TotalSlots && ObservedMembers == SlotsFilled;
    internal int NativeSlotCount { get; init; } = TotalSlots;
    internal int[] UnregisteredNativeIndices { get; init; } = [];
    internal byte JoinConditionFlags { get; init; }
    // The user's friendly-match capture has join flags 0x13 (DC/private/0x10).
    // Do not infer a friendly match from 18 players alone: an ordinary alliance
    // can also have six unregistered slots. Require its flag AND the 5/5/8 footprint.
    internal bool IsFriendlyMatch => NumberOfParties == 3 && NativeSlotCount == 24 &&
        (JoinConditionFlags & 0x10) != 0 && Slots.All(s => s.NativeIndex >= 16 || s.NativeIndex % 8 < 5);
    internal bool MappingValid
    {
        get
        {
            if (!Readable || TotalSlots is < 1 or > 48 || NativeSlotCount < TotalSlots || NativeSlotCount > 48 || Slots.Length != TotalSlots)
                return false;
            var seen = new HashSet<int>();
            var previous = -1;
            for (var i = 0; i < Slots.Length; i++)
            {
                var slot = Slots[i];
                if (slot.Position != i + 1 || slot.NativeIndex <= previous || slot.NativeIndex >= NativeSlotCount ||
                    !slot.HasMember && slot.SlotFlags == 0 || !seen.Add(slot.NativeIndex)) return false;
                previous = slot.NativeIndex;
            }
            foreach (var index in UnregisteredNativeIndices)
                if (index < 0 || index >= NativeSlotCount || !seen.Add(index)) return false;
            return seen.Count == NativeSlotCount;
        }
    }

    internal string PositionLabel(ListingSlot slot) => IsFriendlyMatch
        ? $"{(slot.NativeIndex < 8 ? "별빛팀" : slot.NativeIndex < 16 ? "그림자팀" : "관전")} {slot.NativeIndex % 8 + 1}"
        : NumberOfParties > 1 ? $"{(char)('A' + slot.NativeIndex / 8)}파티 {slot.NativeIndex % 8 + 1}"
        : (slot.NativeIndex + 1).ToString();

    internal bool MatchesMembers(ReadOnlySpan<ulong> members)
    {
        if (!MappingValid || members.Length < NativeSlotCount) return false;
        foreach (var slot in Slots)
            if (members[slot.NativeIndex] != slot.MemberContentId) return false;
        foreach (var index in UnregisteredNativeIndices)
            if (members[index] != 0) return false;
        return true;
    }

    internal static ListingSlots Unavailable(string reason) =>
        new(false, 0, 0, 0, false, [], $"listingSlotsReadable=False; listingSlotsReason={reason}");

    internal static ListingSlots Read(bool detailAgrees, int totalSlots, int slotsFilled,
        int numberOfParties, bool isAlliance, ReadOnlySpan<byte> jobs,
        ReadOnlySpan<ulong> memberContentIds, ReadOnlySpan<ulong> slotFlags, Func<uint, string> jobName,
        ListingJobRules? jobRules = null, byte joinConditionFlags = 0)
    {
        if (!detailAgrees) return Unavailable("detail_ui_mismatch");
        if (totalSlots is < 1 or > 48 || slotsFilled < 0 || slotsFilled > totalSlots)
            return Unavailable("invalid_slot_count");
        // TotalSlots counts registered positions; it is not the array extent.
        // Three/six groups retain 24/48 physical slots even when slots are removed.
        // The game compacts unregistered positions to each group's end at creation.
        // Always use the fetched layout, not the position clicked before creation.
        // A single party is already compacted and keeps its existing count boundary.
        var nativeCount = numberOfParties is 3 or 6 ? numberOfParties * 8 : totalSlots;
        if (nativeCount < totalSlots || jobs.Length < nativeCount || memberContentIds.Length < nativeCount || slotFlags.Length < nativeCount)
            return Unavailable("incomplete_arrays");

        jobRules ??= ListingJobRules.Unavailable;
        var registered = new List<ListingSlot>();
        var unregistered = new List<int>();
        for (var nativeIndex = 0; nativeIndex < nativeCount; nativeIndex++)
        {
            var job = jobs[nativeIndex];
            var member = memberContentIds[nativeIndex];
            var flags = slotFlags[nativeIndex];
            if (job == 0 && member == 0 && flags == 0)
            {
                unregistered.Add(nativeIndex);
                continue;
            }
            // Keep all occupied slots and recruitable empty slots in native order.
            // Never replace listing jobs with current jobs or renumber native indices.
            registered.Add(new(registered.Count + 1, member, job, job == 0 ? "" : jobName(job), flags) {
                NativeIndex = nativeIndex,
                AllowedJobs = member == 0 && job == 0 ? jobRules.Describe(flags) : ListingAllowedJobs.Unavailable,
            });
        }
        var slots = registered.ToArray();
        var result = new ListingSlots(true, totalSlots, slotsFilled, numberOfParties, isAlliance, slots, "") {
            JobRules = jobRules, NativeSlotCount = nativeCount, UnregisteredNativeIndices = unregistered.ToArray(),
            JoinConditionFlags = joinConditionFlags,
        };
        var observed = slots.Count(s => s.HasMember);
        var diagnostic = $"listingSlotsReadable=True; listingSlotCount={totalSlots}; listingSlotsFilled={slotsFilled}; " +
            $"listingNumberOfParties={numberOfParties}; listingIsAlliance={isAlliance}; " +
            $"listingObservedMembers={observed}; listingSlotCountsAgree={result.CountsAgree}\n" +
            $"listingFriendlyMatch={result.IsFriendlyMatch}; listingNativeSlotCount={nativeCount}; " +
            $"listingNativeSlotIndices={string.Join(",", slots.Select(s => s.NativeIndex))}\n" +
            $"listingRegisteredSlotCount={slots.Length}; listingUnregisteredSlotCount={unregistered.Count}; " +
            $"listingUnregisteredNativeIndices={string.Join(",", unregistered)}; listingSlotMappingValid={result.MappingValid}\n" +
            $"listingJobIds={string.Join(",", slots.Select(s => s.JobId))}\n" +
            $"listingMemberPresent={string.Join(",", slots.Select(s => s.MemberContentId != 0 ? 1 : 0))}\n" +
            $"listingSlotFlags={string.Join(",", slots.Select(s => s.SlotFlags.ToString("X16")))}\n" +
            $"listingJobRulesComplete={jobRules.Complete}; listingRecruitmentJobCount={jobRules.Count}; " +
            $"listingAllJobsMask={jobRules.AllFlags:X16}";
        return result with { Diagnostic = diagnostic };
    }
}
