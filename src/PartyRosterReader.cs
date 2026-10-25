using FFXIVClientStructs.FFXIV.Client.UI.Info;

namespace PartyViewKR;

// Used only by AFK. Does not read ClassJob/JobId, nearby actors or Excel rows.
internal sealed class PartyRosterReader
{
    internal string Diagnostic { get; private set; } = "afkRosterSource=not_started";

    internal unsafe bool TryRead(out PartyRoster? roster)
    {
        roster = null;
        if (!Plugin.ClientState.IsLoggedIn || !Plugin.PlayerState.IsLoaded) return Fail("player_unavailable");
        var self = Plugin.PlayerState.ContentId;
        try
        {
            if (NativeCompatibility.IsCompatible)
            {
                var proxy = InfoProxyCrossRealm.Instance();
                if (NativeReadGuard.Readable(proxy, (nuint)sizeof(InfoProxyCrossRealm)))
                {
                    if (proxy->IsCrossRealm || proxy->IsInCrossRealmParty)
                        return ReadCrossRealm(proxy, self, out roster);
                }
                else if (Plugin.PartyList.Length == 0)
                    return Fail("cross_realm_unreadable");
                // A readable regular party does not require a cross-realm proxy.
            }
            return ReadRegular(self, out roster);
        }
        catch (Exception ex) { return Fail("read_failed_" + ex.GetType().Name); }
    }

    private unsafe bool ReadCrossRealm(InfoProxyCrossRealm* proxy, ulong self, out PartyRoster? roster)
    {
        roster = null;
        var count = proxy->GroupCount;
        var local = proxy->LocalPlayerGroupIndex;
        if (count is < 1 or > 6 || local >= count) return Fail("cross_realm_shape_invalid");
        var ids = new List<ulong>();
        var localSelf = false;
        for (var groupIndex = 0; groupIndex < count; groupIndex++)
        {
            ref var group = ref proxy->CrossRealmGroups[groupIndex];
            var members = group.GroupMemberCount;
            if (members > 8) return Fail("cross_realm_count_invalid");
            for (var i = 0; i < members; i++)
            {
                var cid = group.GroupMembers[i].ContentId;
                ids.Add(cid);
                if (groupIndex == local && cid == self) localSelf = true;
            }
            if (group.GroupMemberCount != members) return Fail("cross_realm_changed_during_read");
        }
        if (!localSelf || proxy->GroupCount != count || proxy->LocalPlayerGroupIndex != local ||
            !(proxy->IsCrossRealm || proxy->IsInCrossRealmParty) || !PartyRoster.TryCreate(self, ids, out roster))
            return Fail("cross_realm_incomplete");
        Diagnostic = $"afkRosterSource=cross_realm; afkRosterGroups={count}; afkRosterMembers={roster!.Count}";
        return true;
    }

    private bool ReadRegular(ulong self, out PartyRoster? roster)
    {
        roster = null;
        var party = Plugin.PartyList;
        var count = party.Length;
        var alliance = party.IsAlliance;
        var partyId = party.PartyId;
        if (count is < 0 or > 8) return Fail("regular_count_invalid");
        var ids = new List<ulong>();
        if (count == 0 && !alliance) ids.Add(self);
        else
        {
            for (var i = 0; i < count; i++)
            {
                var member = party[i];
                if (member == null || member.ContentId == 0) return Fail("regular_member_unavailable");
                ids.Add(member.ContentId);
            }
            if (alliance)
            {
                // Dalamud's alliance API has 20 entries (also supports light-
                // party alliances). Empty entries have CID 0; this is not the
                // main-party indexer, which exposes only the local party.
                for (var i = 0; i < 20; i++)
                {
                    var address = party.GetAllianceMemberAddress(i);
                    if (address == 0) return Fail("alliance_address_unavailable");
                    var member = party.CreateAllianceMemberReference(address);
                    if (member == null) return Fail("alliance_member_unavailable");
                    if (member.ContentId != 0) ids.Add(member.ContentId);
                }
            }
        }
        if (party.Length != count || party.IsAlliance != alliance || party.PartyId != partyId ||
            !PartyRoster.TryCreate(self, ids, out roster)) return Fail("regular_incomplete");
        Diagnostic = $"afkRosterSource={(alliance ? "regular_alliance" : count == 0 ? "solo" : "regular")}; afkRosterMembers={roster!.Count}";
        return true;
    }

    private bool Fail(string reason) { Diagnostic = "afkRosterRead=unavailable; afkRosterReason=" + reason; return false; }
}
