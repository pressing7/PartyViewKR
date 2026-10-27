using System.Text.Json;
using Discord;
using PartyViewKR;

static class ListingDiscordNotificationTests
{
    private static readonly DiscordNotificationTarget Target = new(500, 700, 800);
    private static void Done(Task task) => task.WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
    internal static void Run(Action<string, Action> test, Action<bool, string> check)
    {
        ListingDiscordNotifications Service(NotificationFakeTransport t, DiscordNotificationTarget? target = null,
            Func<DiscordNotificationTarget, CancellationToken, Task>? save = null) =>
            new(500, t, target, save ?? ((_, _) => Task.CompletedTask), CancellationToken.None);

        test("Discord completion is an everyone only mention with the requested fixed text", () => {
            var mentions = ListingDiscordClient.CompletionMentions();
            check(ListingDiscordNotifications.CompletionText == "@everyone 파티모집완료" &&
                mentions.AllowedTypes == AllowedMentionTypes.Everyone &&
                (mentions.UserIds == null || mentions.UserIds.Count == 0) &&
                (mentions.RoleIds == null || mentions.RoleIds.Count == 0), "unexpected alert content or broad allowed mentions");
        });
        test("Discord notification target persists with bot identity while unrelated settings survive", () => {
            var config = new RelayConfiguration { EncryptedDiscordToken = "encrypted", AfkPeriodMinutes = 55 };
            config.WriteNotificationTarget(Target);
            var restored = JsonSerializer.Deserialize<RelayConfiguration>(JsonSerializer.Serialize(config))!;
            check(restored.ReadNotificationTarget() == Target && restored.EncryptedDiscordToken == "encrypted" && restored.AfkPeriodMinutes == 55,
                "target persistence changed unrelated configuration");
        });
        test("Discord alerts require AFK completion and an explicitly selected matching bot channel", () => {
            foreach (var target in new DiscordNotificationTarget?[] { null, Target with { ApplicationId = 9 }, Target with { GuildId = 0 } }) {
                var t = new NotificationFakeTransport(); using var n = Service(t, target);
                n.Observe(true, 10, 1, true); Done(n.Delivery); check(t.Sends == 0, "invalid target received alert");
            }
            var transport = new NotificationFakeTransport(); using var notifier = Service(transport, Target);
            notifier.Observe(false, 10, 1, true); notifier.Observe(true, 10, 0, true); notifier.Observe(true, 10, 1, false);
            notifier.Observe(true, 0, 1, true); Done(notifier.Delivery); check(transport.Sends == 0, "invalid completion sent");
        });
        test("Discord repeated full roster polls send once and a new completion episode sends again", () => {
            var t = new NotificationFakeTransport(); using var n = Service(t, Target);
            n.Observe(true, 10, 1, true); Done(n.Delivery);
            for (var i = 0; i < 30; i++) n.Observe(true, 10, 1, true);
            Done(n.Delivery); check(t.Sends == 1 && n.Status.Contains("전송 완료"), "repeated everyone alert");
            n.Observe(true, 10, 1, false); n.Observe(true, 10, 2, true); Done(n.Delivery);
            check(t.Sends == 2, "new completion was suppressed");
        });
        test("Discord preflight is cancelled when AFK stops a vacancy appears or the character changes", () => {
            foreach (var mode in new[] { 0, 1, 2 }) {
                var t = new NotificationFakeTransport { BlockCheck = new(TaskCreationOptions.RunContinuationsAsynchronously) };
                using var n = Service(t, Target); n.Observe(true, 10, 1, true); Done(t.CheckStarted.Task);
                n.Observe(mode != 0, mode == 2 ? 20UL : 10UL, 1, false); Done(n.Delivery);
                check(t.Sends == 0, "stale notification posted");
            }
        });
        test("Discord temporary unreadability before POST can recover without losing the completion alert", () => {
            var t = new NotificationFakeTransport { BlockCheck = new(TaskCreationOptions.RunContinuationsAsynchronously) };
            using var n = Service(t, Target); n.Observe(true, 10, 1, true); Done(t.CheckStarted.Task);
            n.Observe(true, 0, 1, false); Done(n.Delivery); t.BlockCheck = null;
            n.Observe(true, 10, 1, true); Done(n.Delivery); check(t.Sends == 1, "preflight cancellation consumed unsent alert");
        });
        test("Discord ambiguous notification POST failures never retry on every roster poll", () => {
            var t = new NotificationFakeTransport { ThrowSend = true }; using var n = Service(t, Target);
            n.Observe(true, 10, 1, true); Done(n.Delivery);
            for (var i = 0; i < 30; i++) n.Observe(true, 10, 1, true);
            Done(n.Delivery); check(t.Sends == 1 && n.Status.Contains("실패") && !n.Status.Contains("sensitive"), "uncertain POST replayed or exception leaked");
        });
        test("Discord unavailable connection waits for reconnect without consuming an alert", () => {
            var t = new NotificationFakeTransport { Connected = false }; using var n = Service(t, Target);
            n.Observe(true, 10, 1, true); Done(n.Delivery); check(t.Sends == 0, "sent offline");
            t.Connected = true; n.Observe(true, 10, 1, true); Done(n.Delivery); check(t.Sends == 1, "offline state consumed alert");
        });
        test("Discord missing everyone permission blocks sending and channel selection without silent fallback", () => {
            var t = new NotificationFakeTransport { Error = "everyone 권한 필요" }; var saves = 0;
            using var n = Service(t, Target, (_, _) => { saves++; return Task.CompletedTask; });
            var result = n.SelectChannel(700, 900, CancellationToken.None).GetAwaiter().GetResult();
            n.Observe(true, 10, 1, true); Done(n.Delivery);
            check(saves == 0 && t.Sends == 0 && result.Contains("권한") && n.Status.Contains("실패"), "permission failure silently succeeded");
            t.Error = null; Done(n.SelectChannel(700, 800, CancellationToken.None));
            n.Observe(true, 10, 1, true); Done(n.Delivery); check(t.Sends == 1, "explicit reselection cannot recover permission failure");
        });
        test("Discord DM selection and failed persistence preserve the old notification target", () => {
            var t = new NotificationFakeTransport(); var saves = 0;
            using var n = Service(t, Target, (_, _) => { saves++; throw new IOException("sensitive"); });
            var dm = n.SelectChannel(0, 900, CancellationToken.None).GetAwaiter().GetResult();
            check(saves == 0 && dm.Contains("DM"), "DM target accepted");
            var failure = n.SelectChannel(700, 900, CancellationToken.None).GetAwaiter().GetResult();
            n.Observe(true, 10, 1, true); Done(n.Delivery);
            check(saves == 1 && t.LastSent == Target && failure.Contains("저장") && !failure.Contains("sensitive"), "failed save redirected alert");
        });
        test("Discord channel selection alone never posts a message but can deliver a pending full roster", () => {
            var t = new NotificationFakeTransport(); DiscordNotificationTarget? saved = null;
            using var n = Service(t, save: (target, _) => { saved = target; return Task.CompletedTask; });
            n.Observe(true, 10, 1, true); Done(n.SelectChannel(700, 800, CancellationToken.None));
            check(saved == Target && t.Sends == 0, "select channel posted without AFK observation");
            n.Observe(true, 10, 1, true); Done(n.Delivery); check(t.Sends == 1 && t.LastSent == Target, "pending completion lost");
        });
        test("Discord cancellation while waiting for settings save never activates the selected channel", () => {
            var t = new NotificationFakeTransport(); var queued = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var saved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var cancellation = new CancellationTokenSource();
            using var n = Service(t, Target, (_, _) => { queued.TrySetResult(); return saved.Task; });
            var select = n.SelectChannel(700, 900, cancellation.Token); Done(queued.Task); cancellation.Cancel();
            try { Done(select); throw new Exception("cancelled selection completed"); } catch (OperationCanceledException) { }
            saved.TrySetResult(); n.Observe(true, 10, 1, true); Done(n.Delivery);
            check(t.LastSent == Target, "cancelled save redirected notification");
        });
        test("Discord notification channel controls reject wrong owner app stale session and failed ACK", () => {
            var (b, _, view) = ListingWebEditTests.Ready(); var t = new NotificationFakeTransport(); var saves = 0;
            using var n = Service(t, save: (_, _) => { saves++; return Task.CompletedTask; });
            var s = new ListingDiscordSession(b, 500, 600, CancellationToken.None, notifications: n);
            DiscordInput Input(ulong id) => new(id, 500, 600, "", ListingDiscordPanel.NotificationPrefix + view.Session, [], []) { GuildId = 700, ChannelId = 800 };
            foreach (var bad in new[] { Input(1) with { UserId = 9 }, Input(2) with { ApplicationId = 9 }, Input(3) with { CustomId = ListingDiscordPanel.NotificationPrefix + Guid.NewGuid().ToString("N") } })
                Done(s.Process(bad, new NotificationResponse()));
            Done(s.Process(Input(4), new NotificationResponse { FailAck = true }));
            check(saves == 0, "unauthorized or unacknowledged settings mutation");
            Done(s.Process(Input(5), new NotificationResponse())); Done(s.Process(Input(5), new NotificationResponse()));
            check(saves == 1 && t.Sends == 0, "selection duplicate replay or unexpected message");
        });
    }

    private sealed class NotificationResponse : IDiscordResponse
    {
        internal bool FailAck;
        public Task Defer() => FailAck ? Task.FromException(new IOException()) : Task.CompletedTask;
        public Task Modal(Modal modal) => throw new NotSupportedException();
        public Task Screen(DiscordScreen screen, bool replace, bool deferred) => Task.CompletedTask;
    }
    private sealed class NotificationFakeTransport : IDiscordNotificationTransport
    {
        public bool Connected { get; set; } = true;
        internal int Sends;
        internal bool ThrowSend;
        internal string? Error;
        internal TaskCompletionSource? BlockCheck;
        internal TaskCompletionSource CheckStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal DiscordNotificationTarget? LastSent;
        public async Task<string?> CheckChannel(DiscordNotificationTarget target, CancellationToken cancellation)
        {
            CheckStarted.TrySetResult();
            if (BlockCheck != null) await BlockCheck.Task.WaitAsync(cancellation);
            cancellation.ThrowIfCancellationRequested(); return Error;
        }
        public Task SendCompletion(DiscordNotificationTarget target, CancellationToken cancellation, bool excludesSingleJob = false)
        {
            cancellation.ThrowIfCancellationRequested(); LastSent = target; Interlocked.Increment(ref Sends);
            return ThrowSend ? Task.FromException(new IOException("sensitive")) : Task.CompletedTask;
        }
    }
}
