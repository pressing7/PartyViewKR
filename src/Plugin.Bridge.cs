namespace PartyViewKR;

public sealed partial class Plugin
{
    private DiscordHost? discordHost;

    internal string? QueryUnavailableReason => GetQueryUnavailableReason(false);
    private string? GetQueryUnavailableReason(bool ownQueryInProgress, bool ownQueuedRefresh = false)
    {
        if (disposed) return "플러그인이 종료 중입니다.";
        if (Recovering) return "수정창을 닫는 중입니다.";
        if (refreshHost.BlockReason is { } reason) return reason;
        if (!ownQueuedRefresh && Refresh.Busy || !ownQueryInProgress && ListingBusy) return "모집글 조회 또는 갱신을 처리하는 중입니다.";
        if (refreshHost.EditorVisible) return "게임의 파티 모집 수정창을 먼저 닫아 주세요.";
        if (JobDraft != null || DescriptionDraft != null || ObjectiveDraft != null || BeginnerDraft != null ||
            UniqueJobsDraft != null || PrivateDraft != null || CompletionDraft != null || ItemLevelDraft != null || RuleDraft != null ||
            requestedSlot.HasValue || requestedDescription || requestedObjective || requestedBeginner || requestedUniqueJobs ||
            requestedPrivate || requestedCompletion || requestedItemLevel || requestedRule.HasValue)
            return "PVKR에서 편집 중인 내용을 먼저 마쳐 주세요.";
        return null;
    }

    private bool TryQueryOwnListing(out string error)
    {
        error = QueryUnavailableReason ?? "";
        if (error.Length > 0) return false;
        listingReader.RequestOwn(); RequestRead(); return true;
    }

    private void UpdateDiscordBridge(long now)
    {
        var bridge = DiscordEnabled ? discordClient!.Bridge : null;
        if (bridge == null || discordHost == null) return;
        try { bridge.Update(discordHost, now); }
        catch (Exception) { bridge.PublicationFailed(); }
    }

    private sealed partial class DiscordHost(Plugin plugin) : IListingEditHost
    {
        private readonly ListingRequestGuard ownedQuery = new();
        private readonly ListingRefreshGuard ownedRefresh = new();
        private long refreshRevision = -1;
        public ulong SelfId => ClientState.IsLoggedIn && PlayerState.IsLoaded ? PlayerState.ContentId : 0;
        public string? QueryBlockReason => plugin.QueryUnavailableReason;
        public bool QueryBusy => plugin.listingReader.Busy;
        public bool QuerySucceeded => plugin.listingReader.LastOwnRequestSucceeded;
        public long DetailRevision => plugin.listingReader.DetailRevision;
        public ListingSnapshot Listing => plugin.Listing;
        public string? RefreshBlockReason => plugin.QueryUnavailableReason ??
            (plugin.PendingChanges.Items.Count > 0 ? "PVKR에 적용 대기 중인 변경사항을 먼저 처리해 주세요." :
            plugin.Refresh.CoolingDown(Environment.TickCount64) ? "잠시 후 다시 갱신해 주세요." : null);
        public RefreshTarget? CaptureEditTarget()
        {
            if (plugin.Refresh.Busy || QueryBusy || plugin.refreshHost.BlockReason != null ||
                !Listing.IsLive || Listing.Owner != ListingOwner.Self || Listing.ViewerContentId != SelfId) return null;
            return plugin.refreshHost.CaptureOwn();
        }
        public bool StartRefresh(RefreshTarget source, ListingChange[] changes, out string error)
        {
            error = RefreshBlockReason ?? "";
            if (error.Length > 0) return false;
            if (SelfId == 0 || source.SelfId != SelfId) { error = "게임 접속 상태가 바뀌었습니다."; return false; }
            var before = plugin.Refresh.RequestRevision;
            if (changes.Length == 0) plugin.Refresh.Request(Environment.TickCount64, source: source);
            else plugin.Refresh.RequestBatch(Environment.TickCount64, changes);
            if (plugin.Refresh.RequestRevision == before) { error = "갱신을 시작하지 못했습니다. 잠시 후 다시 확인해 주세요."; return false; }
            refreshRevision = plugin.Refresh.RequestRevision;
            ownedRefresh.Started(SelfId, refreshRevision, Environment.TickCount64);
            plugin.RequestRead(); return true;
        }
        public RemoteRefreshProgress? ReadRefresh() => refreshRevision == plugin.Refresh.RequestRevision
            ? RemoteRefreshProgress.From(plugin.Refresh) : null;
        public bool RequestQuery(out string error)
        {
            if (!plugin.TryQueryOwnListing(out error)) return false;
            ownedQuery.Started(SelfId, plugin.listingReader.RequestRevision, Environment.TickCount64); return true;
        }
        internal void BeforeRead(long now)
        {
            if (ownedRefresh.BeforeRead(SelfId, plugin.Refresh.RequestRevision, plugin.Refresh.Busy,
                plugin.DiscordEnabled, plugin.Refresh.Stage == RefreshStage.Queued, now)) CancelOwnedRefresh();
            // RequestOwn sets a flag; native dispatch happens in a later Read.
            // Recheck then too, especially if the character/editor changed in
            // the intervening frame. Never cancel a newer local request.
            if (ownedQuery.Owns(plugin.listingReader.RequestRevision, plugin.listingReader.Busy) &&
                ownedQuery.BeforeRead(SelfId, plugin.listingReader.RequestRevision, true, plugin.DiscordEnabled,
                    plugin.GetQueryUnavailableReason(true) != null, now))
                CancelOwnedQuery();
        }
        internal void CancelOwnedQuery()
        {
            // A later local query/refresh has its own serial and must survive.
            if (ownedQuery.Owns(plugin.listingReader.RequestRevision, plugin.listingReader.Busy))
                plugin.listingReader.FailRequest();
            ownedQuery.Clear();
        }
        internal void CancelOwnedRefresh()
        {
            if (ownedRefresh.Owns(plugin.Refresh.RequestRevision, plugin.Refresh.Busy)) plugin.Refresh.Cancel();
            ownedRefresh.Clear();
        }
    }
}
