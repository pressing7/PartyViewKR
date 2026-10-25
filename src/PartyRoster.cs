namespace PartyViewKR;

// Identity only: current jobs, names, order, levels and positions deliberately
// cannot participate in AFK change detection. Recruitment jobs stay in ListingSlots.
internal sealed record PartyRoster(ulong SelfId, string Fingerprint, int Count)
{
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
