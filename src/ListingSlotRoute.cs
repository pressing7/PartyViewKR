namespace PartyViewKR;

internal enum RecruitmentSlotLayout { Unknown, SingleParty, Alliance24, Alliance48, FriendlyMatch }

// Positions are 1-based visible rows, including recruitable empty slots.
// Native indices preserve friendly-match padding. The two alliance editors
// have different navigation; never extend 51..58 linearly across 48 slots.
internal sealed record ListingSlotRoute(RecruitmentSlotLayout Layout, int TotalSlots,
    int GroupIndex, int MemberIndex, uint OpenNodeId, int? OpenParam,
    uint MemberNodeId = 0, int MemberParam = 0)
{
    internal bool SelectMember => Layout == RecruitmentSlotLayout.Alliance48;
    internal int NativeIndex => GroupIndex * 8 + MemberIndex;

    // Settings other than jobs retain their previously supported layouts.
    internal static bool SupportsSettings(ListingSlots slots) => Classify(slots) is
        RecruitmentSlotLayout.SingleParty or RecruitmentSlotLayout.Alliance24 or RecruitmentSlotLayout.Alliance48;

    internal static RecruitmentSlotLayout Classify(ListingSlots slots)
    {
        if (!slots.Readable || !slots.CountsAgree || !slots.MappingValid)
            return RecruitmentSlotLayout.Unknown;
        if (slots.IsFriendlyMatch) return RecruitmentSlotLayout.FriendlyMatch;
        if (!slots.IsAlliance && slots.NumberOfParties <= 1 && slots.TotalSlots is >= 1 and <= 8)
            return RecruitmentSlotLayout.SingleParty;
        if (slots.IsAlliance && slots.TotalSlots == 24 && slots.NumberOfParties == 3)
            return RecruitmentSlotLayout.Alliance24;
        if (slots.IsAlliance && slots.TotalSlots == 48 && slots.NumberOfParties == 6)
            return RecruitmentSlotLayout.Alliance48;
        return RecruitmentSlotLayout.Unknown;
    }

    internal static bool TryCreate(ListingSlots slots, int position, out ListingSlotRoute route)
    {
        route = null!;
        if (position < 1 || position > slots.TotalSlots) return false;
        var layout = Classify(slots);
        if (layout == RecruitmentSlotLayout.Unknown) return false;
        var nativeIndex = slots.Slots[position - 1].NativeIndex;
        var group = nativeIndex / 8;
        var member = nativeIndex % 8;
        route = layout switch {
            RecruitmentSlotLayout.SingleParty => new(RecruitmentSlotLayout.SingleParty,
                slots.TotalSlots, 0, member, (uint)(50 + position), 8),
            // KR Inspector: A 51..58, B 60..67, C 69..76. Read each
            // button's registered click parameter; none was supplied for 24p.
            RecruitmentSlotLayout.Alliance24 => new(RecruitmentSlotLayout.Alliance24,
                24, group, member, (uint)(51 + group * 9 + member), null),
            // KR manual traces: 51..55 / 60..64 / 69..76, all Param 8.
            RecruitmentSlotLayout.FriendlyMatch => new(RecruitmentSlotLayout.FriendlyMatch,
                18, group, member, (uint)(51 + group * 9 + member), 8),
            // KR Inspector: A..F nodes 44..49 / params 9..14;
            // member 1..8 nodes 3..10 / params 4..11.
            RecruitmentSlotLayout.Alliance48 => new(RecruitmentSlotLayout.Alliance48,
                48, group, member, (uint)(44 + group), 9 + group, (uint)(3 + member), 4 + member),
            _ => null!,
        };
        return route != null;
    }

    internal bool MatchesEditorShape(int groups, int mainPartySlots) => Layout switch {
        RecruitmentSlotLayout.SingleParty => groups == 1 && mainPartySlots == TotalSlots,
        RecruitmentSlotLayout.Alliance24 => groups == 3 && mainPartySlots == 8,
        RecruitmentSlotLayout.Alliance48 => groups == 6 && mainPartySlots == 8,
        RecruitmentSlotLayout.FriendlyMatch => groups == 3 && mainPartySlots == 5,
        _ => false,
    };
}
