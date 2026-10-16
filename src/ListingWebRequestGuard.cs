namespace PartyViewKR;

// RequestOwn first sets a flag; the native request runs on a later frame.
// Track ownership across that gap so stop/session/editor changes cannot let
// deferred web work through or accidentally cancel a newer in-game request.
internal sealed class ListingWebRequestGuard
{
    private long revision = -1, started;
    private ulong self;
    private bool dispatched;
    internal void Started(ulong selfId, long requestRevision, long now)
    { self = selfId; revision = requestRevision; started = now; dispatched = false; }
    internal bool Owns(long requestRevision, bool busy) => revision >= 0 && requestRevision == revision && busy;
    internal bool BeforeRead(ulong selfId, long requestRevision, bool busy, bool enabled, bool blocked, long now)
    {
        if (!Owns(requestRevision, busy)) return false;
        if (!enabled || selfId == 0 || selfId != self || blocked || now - started > (dispatched ? 15000 : 5000)) return true;
        dispatched = true; return false;
    }
    internal void Clear() => revision = -1;
}
