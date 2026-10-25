namespace PartyViewKR;

public sealed partial class Plugin
{
    private readonly AfkListingMonitor afk = new();
    private readonly PartyRosterReader afkReader = new();
    private AfkHost? afkHost;
    internal bool AfkEnabled => afk.Enabled;
    internal string AfkStatus => afk.Status;

    internal void SetAfkEnabled(bool enabled)
    {
        afkHost ??= new(this);
        afk.SetEnabled(enabled, afkHost, Environment.TickCount64);
        RequestRead();
    }

    private sealed class AfkHost(Plugin plugin) : IAfkListingHost
    {
        public bool LoggedIn => ClientState.IsLoggedIn;
        public ulong SelfId => LoggedIn && PlayerState.IsLoaded ? PlayerState.ContentId : 0;
        public bool TryReadRoster(out PartyRoster? roster) => plugin.afkReader.TryRead(out roster);
        public string? QueryBlockReason(bool ownQueryInProgress) => plugin.GetQueryUnavailableReason(ownQueryInProgress) ??
            (plugin.PendingChanges.Items.Count > 0 ? "적용 대기 중인 변경사항을 먼저 처리해 주세요." :
             plugin.Refresh.CoolingDown(Environment.TickCount64) ? "갱신 결과를 기다리는 중입니다." : null);
        public bool QueryBusy => plugin.listingReader.Busy;
        public bool QuerySucceeded => plugin.listingReader.LastOwnRequestSucceeded;
        public long RequestRevision => plugin.listingReader.RequestRevision;
        public long DetailRevision => plugin.listingReader.DetailRevision;
        public bool OwnListingMatches(PartyRoster roster)
        {
            var listing = plugin.Listing;
            return listing.IsLive && listing.Owner == ListingOwner.Self && listing.ViewerContentId == roster.SelfId &&
                listing.Slots.CountsAgree && PartyRoster.TryCreate(roster.SelfId,
                    listing.Slots.Slots.Where(s => s.HasMember).Select(s => s.MemberContentId), out var listed) && listed == roster;
        }
        public bool RequestQuery(out string error) => plugin.TryQueryOwnListing(out error);
        public void CancelQuery(long revision)
        {
            if (plugin.listingReader.RequestRevision == revision && plugin.listingReader.Busy)
                plugin.listingReader.FailRequest();
        }
    }
}
