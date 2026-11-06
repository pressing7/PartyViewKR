namespace PartyViewKR;

// One definition for publishing, query confirmation and AFK capacity evidence.
// Ownership for editing remains Self; being a member grants read access only.
internal static class ListingPartyScope
{
    internal static bool ReadableOwner(ListingOwner owner) => owner is ListingOwner.Self or ListingOwner.PartyLeader;

    internal static bool BelongsTo(ListingSnapshot listing, PartyRoster roster) =>
        ReadableOwner(listing.Owner) && listing.ViewerContentId == roster.SelfId && listing.PartyKey == roster.PartyKey &&
        roster.Contains(listing.LeaderContentId) && roster.QueryCandidates.Contains(listing.LeaderContentId) &&
        (listing.Owner == ListingOwner.Self ? listing.LeaderContentId == roster.SelfId :
            listing.Slots.Slots.Any(s => s.HasMember && s.MemberContentId == roster.SelfId));

    internal static bool MatchesRoster(ListingSnapshot listing, PartyRoster roster) =>
        listing.IsLive && BelongsTo(listing, roster) && listing.Slots.CountsAgree && listing.Slots.MappingValid &&
        PartyRoster.TryCreate(roster.SelfId, listing.Slots.Slots.Where(s => s.HasMember).Select(s => s.MemberContentId), out var listed) &&
        roster.SameMembers(listed!);

    internal static bool CanConfirm(ulong requestedLeader, bool ownOnly, ListingSnapshot listing, PartyRoster? current) =>
        listing.IsLive && listing.LeaderContentId == requestedLeader &&
        (ownOnly ? listing.Owner == ListingOwner.Self && listing.LeaderContentId == listing.ViewerContentId :
            current != null && BelongsTo(listing, current));
}
