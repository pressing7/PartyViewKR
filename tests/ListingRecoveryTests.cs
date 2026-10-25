using System.Text.Json;
using Discord;
using PartyViewKR;

static class ListingRecoveryTests
{
    private static (ListingWebBridge b, RecoveryFakeHost h, string id, string session) Ready()
    {
        var (b, native, view) = ListingWebEditTests.Ready(); var h = new RecoveryFakeHost(native);
        return (b, h, Guid.NewGuid().ToString("D"), view.Session);
    }
    internal static void Run(Action<string, Action> test, Action<bool, string> check)
    {
        test("recovery closes the role child before the editor and confirms disappearance", () => {
            var (b, h, id, session) = Ready(); h.Show(true);
            check(b.QueueCloseEditor(id, session, 1100).Status == 202 && h.Cancels == 0, "off-framework close");
            b.BeforeReadRecovery(h, 1200); check(h.Cancels == 1 && h.Closes.SequenceEqual(new[] { RecruitmentWindow.Roles }), "wrong close order");
            b.BeforeReadRecovery(h, 1700); check(h.Closes.SequenceEqual(new[] { RecruitmentWindow.Roles, RecruitmentWindow.Editor }) && b.RecoveryActive, "no verification wait");
            b.BeforeReadRecovery(h, 2200); check(b.ReadRecovery(id).Body is WebRecovery { State: "complete" } && !b.RecoveryActive, "close not verified");
            check(h.Game.Native.SubmitCalls == 0, "recovery submitted");
        });
        test("recovery supersedes a queued refresh before it can dispatch", () => {
            var (b, h, id, session) = Ready(); var v = b.ReadView(); var apply = Guid.NewGuid().ToString("D");
            b.QueueApply(apply, session, JsonSerializer.SerializeToUtf8Bytes(new { revision = v.Editor!.Revision, changes = Array.Empty<object>() }), 1100);
            b.QueueCloseEditor(id, session, 1150); b.BeforeReadRecovery(h, 1200); b.Update(h, 1200); h.Game.Advance(2000);
            check(h.Game.StartCalls == 0 && h.Game.Native.SubmitCalls == 0 && b.ReadOperation(apply).Body is WebOperation { State: "cancelled" }, "queued refresh survived recovery");
        });
        test("recovery cancels an active refresh before the next submit step", () => {
            var (b, h, id, session) = Ready(); var v = b.ReadView();
            b.QueueApply(Guid.NewGuid().ToString("D"), session, JsonSerializer.SerializeToUtf8Bytes(new { revision = v.Editor!.Revision, changes = Array.Empty<object>() }), 1100);
            b.Update(h, 1200); h.Game.Advance(1500); h.Game.Advance(2000); h.Show();
            b.QueueCloseEditor(id, session, 2100); b.BeforeReadRecovery(h, 2200);
            for (var t = 2500; t <= 10000; t += 500) h.Game.Advance(t);
            check(!h.Game.Flow.Busy && h.Game.Native.SubmitCalls == 0 && h.Cancels == 1, "cancelled flow continued");
        });
        test("recovery does not need a readable listing or enabled refresh button", () => {
            var (b, h, id, session) = Ready(); h.Game.Live = false; h.Game.Blocked = "editor stuck"; h.Show(); b.Update(h, 1100);
            check(!b.ReadView().CanRefresh && b.QueueCloseEditor(id, session, 1200).Status == 202, "recovery used refresh preflight");
            b.BeforeReadRecovery(h, 1300); b.BeforeReadRecovery(h, 1800);
            check(b.ReadRecovery(id).Body is WebRecovery { State: "complete" }, "failed listing blocked close");
        });
        test("recovery request IDs are idempotent and concurrent closes are rejected", () => {
            var (b, h, id, session) = Ready(); h.Show();
            b.QueueCloseEditor(id, session, 1100); check(b.QueueCloseEditor(id, session, 1101).Status == 200, "duplicate not returned");
            check(b.QueueCloseEditor(Guid.NewGuid().ToString("D"), session, 1102).Status == 409, "parallel close accepted");
            b.BeforeReadRecovery(h, 1200); b.BeforeReadRecovery(h, 1700); b.QueueCloseEditor(id, session, 1800); b.BeforeReadRecovery(h, 1800);
            check(h.Cancels == 1 && h.Closes.Count == 1, "close replayed");
        });
        test("recovery rejects stale sessions publications and expired queued requests", () => {
            var (b, h, id, session) = Ready(); h.Show();
            check(b.QueueCloseEditor(id, "old", 1100).Status == 409 && b.QueueCloseEditor(id, session, 7000).Status == 409, "stale request accepted");
            b.QueueCloseEditor(id, session, 1100); b.BeforeReadRecovery(h, 6101);
            check(h.Cancels == 0 && h.Closes.Count == 0 && b.ReadRecovery(id).Body is WebRecovery { State: "failed" }, "expired request executed");
        });
        test("recovery character switches never close another character window", () => {
            var (b, h, id, session) = Ready(); h.Show(); b.QueueCloseEditor(id, session, 1100); h.Game.Native.SelfId = 99;
            b.BeforeReadRecovery(h, 1200); check(h.Cancels == 0 && h.Closes.Count == 0, "foreign character closed");
        });
        test("recovery respects native guards while still stopping the refresh", () => {
            var (b, h, id, session) = Ready(); h.Show(); h.Block = "native incompatible";
            b.QueueCloseEditor(id, session, 1100); b.BeforeReadRecovery(h, 1200);
            check(h.Cancels == 1 && h.Closes.Count == 0 && b.ReadRecovery(id).Body is WebRecovery { State: "failed" }, "native guard bypassed");
        });
        test("recovery refuses replacement editors and never retries uncertain close calls", () => {
            foreach (var throws in new[] { false, true }) {
                var (b, h, id, session) = Ready(); h.Show(true); h.ThrowClose = throws;
                b.QueueCloseEditor(id, session, 1100); b.BeforeReadRecovery(h, 1200);
                h.Windows = h.Windows with { Editor = new(101, 1) };
                b.BeforeReadRecovery(h, 1700); b.BeforeReadRecovery(h, 2200);
                check(h.Closes.Count == 1 && b.ReadRecovery(id).Body is WebRecovery { State: "failed" }, "new/uncertain window closed again");
            }
        });
        test("recovery reports timeout instead of success if the window stays visible", () => {
            var (b, h, id, session) = Ready(); h.Show(true); h.HideOnClose = false;
            b.QueueCloseEditor(id, session, 1100);
            for (var t = 1200; t <= 6200; t += 500) b.BeforeReadRecovery(h, t);
            check(h.Closes.Count == 1 && b.ReadRecovery(id).Body is WebRecovery { State: "failed" }, "timeout repeated close or claimed success");
        });
        test("recovery makes an old acknowledged Discord request unusable after completion", () => {
            var (b, h, id, session) = Ready(); var p = new ListingDiscordPanel(b, 500, 600);
            var old = p.Handle(new(1, 500, 600, "pvkr", "", [], []), 1100).Work!;
            b.QueueCloseEditor(id, session, 1150); b.BeforeReadRecovery(h, 1200); b.Update(h, 1300);
            check(p.Start(old, 1400).Status == 409 && h.Game.Native.QueryCalls == 0, "late ACK restarted cancelled query");
        });
        test("discord recovery works during a blocked normal ACK and requires its own successful ACK", () => {
            var (b, h, _, sessionId) = Ready(); h.Show();
            using var cts = new CancellationTokenSource(); var session = new ListingDiscordSession(b, 500, 600, cts.Token, () => 1100);
            var ack = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var normal = session.Process(new(10, 500, 600, "pvkr", "", [], []), new RecoveryResponse { Ack = ack.Task });
            var closeInput = new DiscordInput(11, 500, 600, "", ListingDiscordPanel.ClosePrefix + sessionId, [], []);
            var failed = new RecoveryResponse { FailAck = true };
            session.Process(closeInput, failed).GetAwaiter().GetResult(); b.BeforeReadRecovery(h, 1200);
            check(h.Cancels == 0, "failed ACK closed editor");
            var response = new RecoveryResponse { Pump = () => { b.BeforeReadRecovery(h, 1200); b.BeforeReadRecovery(h, 1700); } };
            session.Process(closeInput with { Id = 12 }, response).GetAwaiter().GetResult();
            check(h.Cancels == 1 && h.Closes.Count == 1 && response.Last!.Text.Contains("닫았습니다"), "normal handling blocked recovery");
            ack.SetResult(); normal.GetAwaiter().GetResult();
            check(b.ReadView().Query == null, "late ACK queued old work");
        });
        test("discord recovery rejects another owner app and stale card", () => {
            var (b, h, _, sessionId) = Ready(); var session = new ListingDiscordSession(b, 500, 600, CancellationToken.None, () => 1100);
            var input = new DiscordInput(20, 500, 600, "", ListingDiscordPanel.ClosePrefix + sessionId, [], []);
            foreach (var bad in new[] { input with { UserId = 1 }, input with { ApplicationId = 1 }, input with { CustomId = ListingDiscordPanel.ClosePrefix + Guid.NewGuid().ToString("N") } })
                session.Process(bad, new RecoveryResponse()).GetAwaiter().GetResult();
            b.BeforeReadRecovery(h, 1200); check(h.Cancels == 0 && h.Closes.Count == 0, "unauthorized recovery");
        });
    }

    private sealed class RecoveryResponse : IDiscordResponse
    {
        internal Task? Ack; internal bool FailAck; internal Action? Pump; internal DiscordScreen? Last;
        public Task Defer() => FailAck ? Task.FromException(new IOException()) : Ack ?? Task.CompletedTask;
        public Task Modal(Modal modal) => Task.CompletedTask;
        public Task Screen(DiscordScreen screen, bool replace, bool deferred)
        { Last = screen; if (deferred) Pump?.Invoke(); return Task.CompletedTask; }
    }
}

sealed class RecoveryFakeHost(WebEditFakeHost game) : IListingWebEditHost, IListingRecoveryHost
{
    internal WebEditFakeHost Game => game;
    internal RecruitmentWindows Windows = new(new(0, 0), new(0, 0));
    internal int Cancels; internal bool HideOnClose = true, ThrowClose; internal string? Block;
    internal readonly List<RecruitmentWindow> Closes = [];
    internal void Show(bool role = false) { Windows = new(new(100, 0), new(role ? 200 : 0, 0)); game.Native.EditorVisible = true; }
    public ulong SelfId => game.SelfId;
    public string? RecoveryBlockReason => Block;
    public void StopForRecovery() { Cancels++; game.Flow.Cancel(); game.Native.CancelQuery(); }
    public bool ReadRecoveryWindows(out RecruitmentWindows windows) { windows = Windows; return true; }
    public bool CloseRecoveryWindow(RecruitmentWindow kind, RecruitmentWindowTicket ticket, out string error)
    {
        Closes.Add(kind); error = "";
        if (HideOnClose) {
            Windows = kind == RecruitmentWindow.Roles ? Windows with { Roles = new(0, 1) } : Windows with { Editor = new(0, 1) };
            game.Native.EditorVisible = Windows.Editor.Address != 0;
        }
        if (ThrowClose) throw new IOException("uncertain close");
        return true;
    }
    public string? QueryBlockReason => Windows.Editor.Address != 0 ? "editor open" : game.QueryBlockReason;
    public bool QueryBusy => game.QueryBusy;
    public bool QuerySucceeded => game.QuerySucceeded;
    public long DetailRevision => game.DetailRevision;
    public ListingSnapshot Listing => game.Listing;
    public bool RequestQuery(out string error) => game.RequestQuery(out error);
    public string? RefreshBlockReason => QueryBlockReason ?? game.RefreshBlockReason;
    public RefreshTarget? CaptureEditTarget() => Windows.Editor.Address != 0 ? null : game.CaptureEditTarget();
    public bool StartRefresh(RefreshTarget source, ListingChange[] changes, out string error) => game.StartRefresh(source, changes, out error);
    public WebRefreshProgress? ReadRefresh() => game.ReadRefresh();
}
