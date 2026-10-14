namespace PartyViewKR;

internal enum ListingOwner { Unknown, Self, PartyLeader, Other }

internal static class ListingIdentity
{
    internal static ListingOwner Resolve(ulong leaderId, string leaderName, bool displayAgrees,
        ulong selfId, string selfName, IReadOnlyList<(ulong Id, string Name, bool IsLeader)> party)
    {
        if (!displayAgrees || leaderId == 0 || string.IsNullOrWhiteSpace(leaderName)) return ListingOwner.Unknown;
        if (leaderId == selfId)
            return leaderName == selfName ? ListingOwner.Self : ListingOwner.Unknown;
        var leader = party.FirstOrDefault(p => p.IsLeader && p.Id == leaderId);
        if (leader.Id != 0)
            return leader.Name == leaderName ? ListingOwner.PartyLeader : ListingOwner.Unknown;
        return ListingOwner.Other;
    }

    internal static bool SameText(string a, string b) => Normalize(a) == Normalize(b);
    private static string Normalize(string text) => text.Replace("\r\n", "\n").Replace('\r', '\n').Trim();
}
