namespace PartyViewKR;

// Identity only: current jobs, names, order, levels and positions deliberately
// cannot participate in AFK change detection. Recruitment jobs stay in ListingSlots.
internal sealed record PartyRoster(ulong SelfId, string Fingerprint, int Count)
{
    internal string PartyKey { get; init; } = "";
    internal string LeaderIds { get; init; } = "";
    internal ulong[] QueryCandidates => LeaderIds.Length == 0 ? [] : LeaderIds.Split(',').Select(ulong.Parse).ToArray();
    internal bool Contains(ulong id) => id != 0 && Fingerprint.Split(',').Contains(id.ToString(), StringComparer.Ordinal);
    internal bool SameMembers(PartyRoster other) => SelfId == other.SelfId && Fingerprint == other.Fingerprint;
    internal bool Continues(PartyRoster previous) => SelfId == previous.SelfId &&
        (PartyKey == previous.PartyKey && LeaderIds == previous.LeaderIds ||
         previous.PartyKey == "solo" && LeaderIds == SelfId.ToString());

    internal int CountJoinedSince(PartyRoster previous)
    {
        if (!Continues(previous) || Fingerprint == previous.Fingerprint) return 0;
        var before = previous.Fingerprint.Split(',').ToHashSet(StringComparer.Ordinal);
        return Fingerprint.Split(',').Count(id => !before.Contains(id));
    }

    internal static bool TryCreate(ulong selfId, IEnumerable<ulong> members, out PartyRoster? roster)
    {
        roster = null;
        var ids = members.Take(49).Order().ToArray();
        if (selfId == 0 || ids.Length is < 1 or > 48 || ids.Any(id => id == 0) ||
            !ids.Contains(selfId) || ids.Distinct().Count() != ids.Length) return false;
        roster = new(selfId, string.Join(",", ids), ids.Length);
        return true;
    }
}

// A managed observation, not a new listing query or a persisted player identity.
internal sealed record PartyJoinNotice(long Revision, int JoinedCount, int MemberCount);
