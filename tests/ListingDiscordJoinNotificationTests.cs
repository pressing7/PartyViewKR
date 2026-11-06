using PartyViewKR;

static partial class ListingDiscordNotificationTests
{
    private static void RunJoins(Action<string, Action> test, Action<bool, string> check)
    {
        ListingDiscordNotifications Service(NotificationFakeTransport t, DiscordNotificationTarget? target) =>
            new(500, t, target, (_, _) => Task.CompletedTask, CancellationToken.None);

        test("Discord joins require opt in connection identity and matching channel", () => {
            foreach (var target in new DiscordNotificationTarget?[] { null, Target with { ApplicationId = 9 }, Target with { GuildId = 0 } }) {
                var t = new NotificationFakeTransport(); using var n = Service(t, target);
                n.ObserveJoins(true, 10, null); n.ObserveJoins(true, 10, new(1, 1, 3)); Done(n.JoinDelivery);
                check(t.Joins.IsEmpty, "invalid channel received join");
            }
            var transport = new NotificationFakeTransport(); using var notifier = Service(transport, Target);
            notifier.ObserveJoins(false, 10, null); notifier.ObserveJoins(false, 10, new(1, 1, 3));
            notifier.ObserveJoins(true, 0, new(2, 1, 4)); Done(notifier.JoinDelivery);
            check(transport.Joins.IsEmpty, "disabled or missing identity sent join");
        });
        test("Discord joins ignore the initial event and send later revisions once", () => {
            var t = new NotificationFakeTransport(); using var n = Service(t, Target);
            n.ObserveJoins(true, 10, new(5, 2, 3)); Done(n.JoinDelivery); check(t.Joins.IsEmpty, "old event replayed");
            var notice = new PartyJoinNotice(6, 1, 4);
            n.ObserveJoins(true, 10, notice); Done(n.JoinDelivery);
            for (var i = 0; i < 20; i++) n.ObserveJoins(true, 10, notice);
            n.ObserveJoins(true, 10, new(4, 1, 3)); Done(n.JoinDelivery);
            check(t.Joins.Count == 1 && t.Joins.Single() == notice && n.Status.Contains("전송 완료"), "repeated or out-of-order event replayed");
        });
        test("Discord queues consecutive distinct joins during a pending send", () => {
            var t = new NotificationFakeTransport { BlockCheck = new(TaskCreationOptions.RunContinuationsAsynchronously) };
            using var n = Service(t, Target); n.ObserveJoins(true, 10, null); n.ObserveJoins(true, 10, new(1, 1, 3)); Done(t.CheckStarted.Task);
            n.ObserveJoins(true, 10, new(2, 2, 5)); n.ObserveJoins(true, 10, new(3, 1, 6));
            t.BlockCheck.SetResult(); Done(n.JoinDelivery);
            check(t.Joins.Select(x => x.Revision).SequenceEqual([1L, 2L, 3L]), "pending delivery swallowed arrivals");
        });
        test("Discord disabling joins changing character or disconnecting cancels pending alerts", () => {
            foreach (var mode in new[] { 0, 1, 2, 3 }) {
                var t = new NotificationFakeTransport { BlockCheck = new(TaskCreationOptions.RunContinuationsAsynchronously) };
                using var n = Service(t, Target); n.ObserveJoins(true, 10, null); n.ObserveJoins(true, 10, new(1, 1, 3)); Done(t.CheckStarted.Task);
                n.ObserveJoins(true, 10, new(2, 1, 4));
                if (mode == 2) t.Connected = false;
                if (mode == 3) n.Dispose(); else n.ObserveJoins(mode != 0, mode == 1 ? 20UL : 10UL, new(2, 1, 4));
                Done(n.JoinDelivery); check(t.Joins.IsEmpty, "cancelled join posted");
            }
        });
        test("Discord reconnect and channel reselection do not replay historical joins", () => {
            var t = new NotificationFakeTransport(); using var n = Service(t, Target);
            n.ObserveJoins(true, 10, null); n.ObserveJoins(true, 10, new(1, 1, 3)); Done(n.JoinDelivery);
            t.Connected = false; n.ObserveJoins(true, 10, new(2, 1, 4));
            t.Connected = true; n.ObserveJoins(true, 10, new(2, 1, 4)); Done(n.JoinDelivery);
            check(t.Joins.Count == 1, "offline event replayed");
            Done(n.SelectChannel(700, 900, CancellationToken.None)); n.ObserveJoins(true, 10, new(2, 1, 4));
            n.ObserveJoins(true, 10, new(3, 1, 5)); Done(n.JoinDelivery);
            check(t.Joins.Count == 2 && t.LastSent?.ChannelId == 900, "new channel replay or routing error");
        });
        test("Discord changing notification channel cancels queued joins in the old channel", () => {
            var t = new NotificationFakeTransport { BlockCheck = new(TaskCreationOptions.RunContinuationsAsynchronously) };
            using var n = Service(t, Target); n.ObserveJoins(true, 10, null); n.ObserveJoins(true, 10, new(1, 1, 3)); Done(t.CheckStarted.Task);
            n.ObserveJoins(true, 10, new(2, 1, 4));
            t.BlockCheck = null;
            Done(n.SelectChannel(700, 900, CancellationToken.None)); Done(n.JoinDelivery);
            n.ObserveJoins(true, 10, new(2, 1, 4)); Done(n.JoinDelivery);
            check(t.Joins.IsEmpty, "old pending alert redirected");
            n.ObserveJoins(true, 10, new(3, 1, 5)); Done(n.JoinDelivery);
            check(t.Joins.Count == 1 && t.LastSent?.ChannelId == 900, "new join did not use new channel");
        });
        test("Discord failed join POST is consumed once while later arrivals still send", () => {
            var t = new NotificationFakeTransport { ThrowSend = true }; using var n = Service(t, Target);
            n.ObserveJoins(true, 10, null); n.ObserveJoins(true, 10, new(1, 1, 3)); Done(n.JoinDelivery);
            for (var i = 0; i < 20; i++) n.ObserveJoins(true, 10, new(1, 1, 3)); Done(n.JoinDelivery);
            check(t.Joins.Count == 1 && n.Status.Contains("실패") && !n.Status.Contains("sensitive"), "uncertain POST retried or detail leaked");
            t.ThrowSend = false; n.ObserveJoins(true, 10, new(2, 1, 4)); Done(n.JoinDelivery);
            check(t.Joins.Count == 2, "failed POST blocked future events");
        });
        test("Discord missing everyone permission prevents join POST without repeated polling attempts", () => {
            var t = new NotificationFakeTransport { Error = "everyone 권한 필요" }; using var n = Service(t, Target);
            n.ObserveJoins(true, 10, null); n.ObserveJoins(true, 10, new(1, 1, 3)); Done(n.JoinDelivery);
            check(t.Joins.IsEmpty && n.Status.Contains("권한"), "permission failure silently sent");
            t.Error = null; n.ObserveJoins(true, 10, new(1, 1, 3)); Done(n.JoinDelivery);
            check(t.Joins.IsEmpty, "old failed event retried");
            n.ObserveJoins(true, 10, new(2, 1, 4)); Done(n.JoinDelivery); check(t.Joins.Count == 1, "new event could not send");
        });
        test("Discord malformed join counts cannot produce alerts", () => {
            var t = new NotificationFakeTransport(); using var n = Service(t, Target); n.ObserveJoins(true, 10, null);
            foreach (var bad in new[] { new PartyJoinNotice(1, 0, 3), new(2, 3, 3), new(3, 1, 49), new(4, 48, 48), new(5, 1, 1) }) n.ObserveJoins(true, 10, bad);
            Done(n.JoinDelivery); check(t.Joins.IsEmpty, "invalid counts sent");
        });
        test("Discord join queue stays bounded and exposes dropped pending events", () => {
            var t = new NotificationFakeTransport { BlockCheck = new(TaskCreationOptions.RunContinuationsAsynchronously) };
            using var n = Service(t, Target); n.ObserveJoins(true, 10, null); n.ObserveJoins(true, 10, new(1, 1, 3)); Done(t.CheckStarted.Task);
            for (var i = 2; i <= 70; i++) n.ObserveJoins(true, 10, new(i, 1, 3));
            t.BlockCheck.SetResult(); Done(n.JoinDelivery);
            check(t.Joins.Count == ListingDiscordNotifications.MaxPendingJoins + 1 && t.Joins.Last().Revision == 70 && n.Status.Contains("5건 제외"), "unbounded or silent queue overflow");
        });
    }
}
