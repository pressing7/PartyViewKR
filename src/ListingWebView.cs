namespace PartyViewKR;

// Only managed display data crosses the HTTP boundary: no content IDs, native
// flags, diagnostic dump, pending edits, or recruitment password.
internal sealed record WebSetting(string Key, string Label, string Value)
{
    // Discord hides inactive settings using the captured value, not translated
    // display text or a newer editing draft. This does not change the web JSON.
    internal bool Active { get; init; }
}
internal sealed record WebSlot(int NativeIndex, string Position, bool HasMember, string Job,
    bool Anyone, bool RulesComplete, string[] AllowedJobs)
{
    // Registered participant job is separate from the slot's allowed jobs.
    public uint JobId { get; init; }
    public uint[] AllowedJobIds { get; init; } = [];
}
internal sealed record WebGroup(string Name, WebSlot[] Slots);
internal sealed record WebListing(string Leader, string Duty, string Description, string Remaining,
    DateTimeOffset? ReadAt, bool Live, int? Filled, int? Total, bool SlotsReadable, bool CountsAgree,
    WebSetting[] Settings, WebGroup[] Groups);
internal sealed record WebQuery(string Id, string State, string Message);
internal sealed record WebView(int ApiVersion, string Version, string Session, bool LoggedIn, bool CanQuery,
    string Reason, string Status, DateTimeOffset UpdatedAt, WebListing? Listing, WebQuery? Query)
{
    public bool CanRefresh { get; init; }
    public string RefreshReason { get; init; } = "내 모집글을 조회해 주세요.";
    public WebEditModel? Editor { get; init; }
    public WebOperation? Operation { get; init; }
}

internal static class ListingWebView
{
    internal static WebListing? From(ListingSnapshot listing, bool loggedIn, bool queryBusy)
    {
        if (!loggedIn || listing.Owner != ListingOwner.Self || listing.ReadAt == null) return null;
        var slots = listing.Slots;
        var readable = slots.Readable && slots.MappingValid;
        var groups = readable ? slots.Slots.GroupBy(s => slots.NumberOfParties > 1 ? s.NativeIndex / 8 : 0)
            .Select(g => new WebGroup(slots.IsFriendlyMatch ? g.Key switch { 0 => "별빛팀", 1 => "그림자팀", _ => "관전" } :
                slots.NumberOfParties > 1 ? $"{(char)('A' + g.Key)}파티" : "파티",
                g.Select(s => new WebSlot(s.NativeIndex, slots.PositionLabel(s), s.HasMember, s.Job,
                    s.AllowedJobs.Anyone && s.AllowedJobs.Complete, s.AllowedJobs.Complete,
                    s.AllowedJobs.Names.ToArray()) { JobId = s.JobId, AllowedJobIds = s.AllowedJobs.Ids.ToArray() }).ToArray())).ToArray() : [];
        WebSetting[] settings = [
            new("objective", "모집 목적", ListingObjectives.Label(listing.Objective)) { Active = ListingObjectives.Index(listing.Objective) > 0 },
            new("beginner", "초행 및 초보자 환영", ListingCheckboxValues.Label(listing.BeginnerFriendly)) { Active = listing.BeginnerFriendly == true },
            new("uniqueJobs", "잡 중복 없음", ListingCheckboxValues.Label(listing.UniqueJobs)) { Active = listing.UniqueJobs == true },
            new("private", "비공개 설정", ListingCheckboxValues.Label(listing.Private)) { Active = listing.Private == true },
            new("completion", "완료조건", ListingCompletions.Label(listing.Completion)) { Active = listing.Completion is ListingCompletions.Complete or ListingCompletions.Incomplete or 8 },
            new("itemLevel", "평균 아이템 레벨", ListingItemLevels.Label(listing.ItemLevel)) { Active = listing.ItemLevel is >= ListingItemLevels.Minimum and <= ListingItemLevels.Maximum },
            .. Enum.GetValues<ListingRuleKind>().Select(kind => new WebSetting(ListingRules.Id(kind), ListingRules.Title(kind), ListingRules.Label(kind, listing.RuleValue(kind))) {
                Active = listing.RuleValue(kind) is > 0 and var value && ListingRules.ValidValue(kind, value)
            })
        ];
        return new(listing.Leader, listing.Duty, listing.Description, listing.Remaining,
            new DateTimeOffset(listing.ReadAt.Value).ToUniversalTime(), listing.IsLive && !queryBusy,
            readable ? slots.SlotsFilled : null, readable ? slots.TotalSlots : null, readable, slots.CountsAgree, settings, groups);
    }
}
