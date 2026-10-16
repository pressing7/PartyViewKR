namespace PartyViewKR;

internal sealed record ListingSnapshot(string Status, ListingOwner Owner, string Leader, string Duty,
    string Description, string Remaining, DateTime? ReadAt, bool IsLive, string Note, string Diagnostic)
{
    // Bind cached data to the character that read it, even across read failures.
    internal ulong ViewerContentId { get; init; }
    internal ListingSlots Slots { get; init; } = ListingSlots.Unavailable("not_read");
    internal byte? Objective { get; init; }
    internal byte? Completion { get; init; }
    internal int? ItemLevel { get; init; }
    internal byte? DutyFlags { get; init; }
    internal byte? LootRule { get; init; }
    internal int? RuleValue(ListingRuleKind kind) => kind == ListingRuleKind.Loot ? LootRule :
        DutyFlags.HasValue && ListingRules.Mask(kind) != 0 ? (DutyFlags.Value & ListingRules.Mask(kind)) != 0 ? 1 : 0 : null;
    internal bool? BeginnerFriendly { get; init; }
    internal bool? UniqueJobs { get; init; }
    internal bool? Private { get; init; }
    internal static ListingSnapshot Empty { get; } = new("아직 읽은 모집글 없음", ListingOwner.Unknown,
        "", "", "", "", null, false, "내 모집글 조회를 누르거나 게임에서 모집글 상세 창을 열어주세요.", "listingRead=not_started");
}
