using Discord;
using PartyViewKR;

static class GameChatSendingTests
{
    internal sealed class Host : IGameChatSendHost
    {
        public ulong SelfId { get; set; } = 10;
        private string? blockReason;
        public string? BlockReason { get => ReadThrows ? throw new IOException() : blockReason; set => blockReason = value; }
        internal readonly List<(string Command, ChatReplyTarget? Reply)> Calls = [];
        internal bool Reject, Throw, ReadThrows;
        public bool TrySend(string command, ChatReplyTarget? reply, out string error)
        {
            Calls.Add((command, reply)); error = "game_rejected";
            if (Throw) throw new IOException("private input");
            return !Reject;
        }
    }
    private static ChatSendDraft Draft(GameChatSendBridge bridge, GameChatSendKind kind, long now = 100) {
        var state = bridge.Read(now); return new(kind, state.Session, state.Self, kind == GameChatSendKind.Reply ? state.Reply : null);
    }
    internal static GameChatMessage Tell(string name = "모험가", string world = "펜리르") => new(GameChatKind.Tell, name, world, "안녕하세요", DateTimeOffset.UtcNow);
    private static void Done(Task task) => task.WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();

    internal static void Run(Action<string, Action> test, Action<bool, string> check)
    {
        foreach (var (kind, expected) in new[] {
            (GameChatSendKind.Party, "/p 안녕하세요 <se.1>"), (GameChatSendKind.Alliance, "/a 안녕하세요 <se.1>"),
            (GameChatSendKind.Tell, "/tell 모험가@펜리르 안녕하세요 <se.1>"), (GameChatSendKind.Reply, "/r 안녕하세요 <se.1>") }) {
            test("chat send builds explicit channel " + kind, () => {
                var bridge = new GameChatSendBridge(); var host = new Host(); bridge.Update(host, 100); bridge.ObserveTell(10, Tell());
                var work = bridge.Queue(new(Draft(bridge, kind), "안녕하세요 <se.1>", "모험가@펜리르"), 100, CancellationToken.None);
                check(host.Calls.Count == 0 && !work.IsCompleted, "game called off framework");
                bridge.Update(host, 101); bridge.Update(host, 102); Done(work);
                check(work.Result.Requested && host.Calls.Single().Command == expected, "incorrect route or repeated send");
                check((host.Calls[0].Reply != null) == (kind == GameChatSendKind.Reply), "tell fell back to implicit reply");
            });
        }
        test("chat send uses full UTF8 byte budget including tell target and permits literal slash body", () => {
            var draft = new ChatSendDraft(GameChatSendKind.Party, "s", 10, null);
            check(GameChatSendText.TryBuild(new(draft, new string('가', 165) + "aa"), out _, out _), "500 byte message rejected");
            check(!GameChatSendText.TryBuild(new(draft, new string('가', 166)), out _, out _), "overlong Korean message sent");
            check(!GameChatSendText.TryBuild(new(draft with { Kind = GameChatSendKind.Tell }, new string('a', 480), "모험가@펜리르"), out _, out _), "recipient not in byte budget");
            check(GameChatSendText.TryBuild(new(draft, "/logout"), out var command, out _) && command == "/p /logout", "arbitrary command escaped channel");
        });
        test("chat send rejects control payload multiline invalid Unicode and target injection", () => {
            var d = new ChatSendDraft(GameChatSendKind.Party, "s", 10, null);
            foreach (var text in new[] { "", "  ", "one\n/logout", "x\r/r hi", "x\0y", "x\u0002y", "x\u2028y", "x\ud800", "x\u202ey" })
                check(!GameChatSendText.TryBuild(new(d, text), out _, out _), "accepted invalid text");
            foreach (var target in new[] { "", "모험가", "@펜리르", "모험가@", "모험가@펜리르@초코보", "<t>@펜리르", "/r@펜리르", "모험가@펜리르 hi", "모험가\n/r@펜리르" })
                check(!GameChatSendText.TryBuild(new(d with { Kind = GameChatSendKind.Tell }, "hello", target), out _, out _), "accepted target injection");
            check(GameChatSendText.TryTarget("First Last@Alpha", out _) && GameChatSendText.TryTarget("압이@펜리르", out _), "valid localized name rejected");
        });
        test("chat reply observes incoming tells independently and clears unknown identities", () => {
            var b = new GameChatSendBridge(); var h = new Host(); b.Update(h, 100);
            b.ObserveTell(10, Tell()); b.ObserveTell(10, Tell("다른사람") with { Kind = GameChatKind.Party });
            b.ObserveTell(99, Tell("다른사람")); check(b.Read(100).Reply == new ChatReplyTarget("모험가", "펜리르"), "non-tell changed reply");
            b.ObserveTell(10, Tell("모험가", "")); check(b.Read(100).Reply == null, "unknown sender retained previous recipient");
            b.ObserveTell(10, Tell()); b.InvalidateReply(); check(b.Read(100).Reply == null, "read error retained recipient");
        });
        test("chat reply stops if a different person whispers while drafting or after queueing", () => {
            foreach (var queued in new[] { false, true }) {
                var b = new GameChatSendBridge(); var h = new Host(); b.Update(h, 100); b.ObserveTell(10, Tell());
                var d = Draft(b, GameChatSendKind.Reply);
                var work = queued ? b.Queue(new(d, "답장"), 100, CancellationToken.None) : null;
                b.ObserveTell(10, Tell("새상대")); work ??= b.Queue(new(d, "답장"), 100, CancellationToken.None);
                b.Update(h, 101); Done(work); check(!work.Result.Requested && h.Calls.Count == 0, "reply switched recipient");
            }
        });
        test("chat reply permits more messages from same recipient but never substitutes tell", () => {
            var b = new GameChatSendBridge(); var h = new Host { Reject = true }; b.Update(h, 100); b.ObserveTell(10, Tell());
            var d = Draft(b, GameChatSendKind.Reply); b.ObserveTell(10, Tell() with { Text = "추가 문의" });
            var work = b.Queue(new(d, "답장"), 100, CancellationToken.None); b.Update(h, 101); b.Update(h, 5000); Done(work);
            check(!work.Result.Requested && work.Result.Message == "game_rejected" && h.Calls.Single().Command == "/r 답장", "fallback or automatic retry");
        });
        test("chat send never survives logout character switch or same-character relogin", () => {
            foreach (var relog in new[] { false, true }) {
                var b = new GameChatSendBridge(); var h = new Host(); b.Update(h, 100); b.ObserveTell(10, Tell());
                var d = Draft(b, GameChatSendKind.Party); var work = b.Queue(new(d, "대기"), 100, CancellationToken.None);
                h.SelfId = relog ? 0UL : 11UL; b.Update(h, 101); h.SelfId = 10; b.Update(h, 102); Done(work);
                check(!work.Result.Requested && h.Calls.Count == 0 && b.Read(102).Reply == null, "queued send moved to another session");
                check(!b.Queue(new(d, "이전창"), 102, CancellationToken.None).Result.Requested, "old modal survived relog");
            }
        });
        test("chat send cancels while queued and never resumes after game stall or stop", () => {
            foreach (var mode in new[] { "cancel", "expired", "stop", "blocked", "read_failed" }) {
                var b = new GameChatSendBridge(); var h = new Host(); b.Update(h, 100);
                using var c = new CancellationTokenSource(); var work = b.Queue(new(Draft(b, GameChatSendKind.Party), "대기"), 100, c.Token);
                if (mode == "cancel") c.Cancel();
                if (mode == "stop") b.Stop();
                if (mode == "blocked") h.BlockReason = "지역 이동 중";
                if (mode == "read_failed") h.ReadThrows = true;
                b.Update(h, mode == "expired" ? 5200 : 101); Done(work);
                check(!work.Result.Requested && h.Calls.Count == 0, "stale queued work executed " + mode);
                h.BlockReason = null; h.ReadThrows = false; b.Update(h, 5300); check(h.Calls.Count == 0, "cancelled work replayed");
            }
        });
        test("chat send bounded queue stale snapshot and rate limit", () => {
            var b = new GameChatSendBridge(); var h = new Host(); b.Update(h, 100); var d = Draft(b, GameChatSendKind.Party);
            var first = b.Queue(new(d, "first"), 100, CancellationToken.None);
            check(!b.Queue(new(d, "second"), 100, CancellationToken.None).Result.Requested, "queue unbounded");
            b.Update(h, 101); Done(first);
            check(!b.Queue(new(d, "fast"), 102, CancellationToken.None).Result.Requested, "flood allowed");
            check(!b.Queue(new(d, "stale"), 6000, CancellationToken.None).Result.Requested, "stale framework accepted");
            check(b.Read(6000).Reason.Length > 0 && h.Calls.Count == 1, "stale publication looked ready");
        });
        test("chat native exception cannot replay a one-shot send or expose input", () => {
            var b = new GameChatSendBridge(); var h = new Host { Throw = true }; b.Update(h, 100);
            var work = b.Queue(new(Draft(b, GameChatSendKind.Party), "private input"), 100, CancellationToken.None);
            b.Update(h, 101); b.Update(h, 1100); Done(work);
            check(h.Calls.Count == 1 && !work.Result.Requested && !work.Result.Message.Contains("private input"), "ambiguous send replayed or leaked");
        });
        RunSessions(test, check);
    }

    private static void RunSessions(Action<string, Action> test, Action<bool, string> check)
    {
        test("discord send opens recipient-bound modal and queues only after ACK once", () => {
            var b = new GameChatSendBridge(); var h = new Host(); b.Update(h, 100); b.ObserveTell(10, Tell());
            var s = new ListingDiscordSession(new("test"), 500, 600, CancellationToken.None, () => 100, chatSending: b);
            var open = new Response(); Done(s.Process(Input(1, GameChatSendKind.Reply), open));
            check(open.Form != null && h.Calls.Count == 0, "opening form sent chat");
            var submit = Submit(2, open.Form!.CustomId); var response = new Response(); var task = s.Process(submit, response);
            check(response.Acks == 1 && !task.IsCompleted && h.Calls.Count == 0, "no defer or native access on Discord thread");
            b.Update(h, 101); Done(task); Done(s.Process(submit, new Response())); Done(s.Process(submit with { Id = 3 }, new Response())); b.Update(h, 1200);
            check(h.Calls.Single().Command == "/r 답장" && response.Text.Contains("요청"), "duplicate submit or false delivery confirmation");
        });
        test("discord send rejects unauthorized owner app and failed acknowledgement", () => {
            var b = new GameChatSendBridge(); var h = new Host(); b.Update(h, 100);
            var s = new ListingDiscordSession(new("test"), 500, 600, CancellationToken.None, () => 100, chatSending: b);
            var r = new Response(); Done(s.Process(Input(1, GameChatSendKind.Party) with { UserId = 99 }, r));
            check(r.Form == null, "unauthorized form"); Done(s.Process(Input(2, GameChatSendKind.Party) with { ApplicationId = 99 }, r));
            check(r.Form == null, "wrong app form"); Done(s.Process(Input(3, GameChatSendKind.Party), r));
            Done(s.Process(Submit(4, r.Form!.CustomId), new Response { FailAck = true })); b.Update(h, 101);
            check(h.Calls.Count == 0, "send before successful ACK");
        });
        test("discord send modal expiry unknown fields and missing reply never send", () => {
            var b = new GameChatSendBridge(); var h = new Host(); long now = 100; b.Update(h, now);
            var s = new ListingDiscordSession(new("test"), 500, 600, CancellationToken.None, () => now, chatSending: b);
            var r = new Response(); Done(s.Process(Input(1, GameChatSendKind.Reply), r)); check(r.Form == null, "reply without recipient");
            Done(s.Process(Input(2, GameChatSendKind.Party), r)); var bad = Submit(3, r.Form!.CustomId); bad.Fields["target"] = "other@Alpha";
            Done(s.Process(bad, new Response()));
            Done(s.Process(Input(4, GameChatSendKind.Party), r)); now += 900001; b.Update(h, now);
            Done(s.Process(Submit(5, r.Form!.CustomId), new Response())); b.Update(h, now + 1); check(h.Calls.Count == 0, "invalid or expired form executed");
        });
    }
    private static DiscordInput Input(ulong id, GameChatSendKind kind) => new(id, 500, 600, "pvkr-send", "", [], []) { SendKind = kind };
    private static DiscordInput Submit(ulong id, string custom) => new(id, 500, 600, "", custom, [], new() { ["message"] = "답장" });
    private sealed class Response : IDiscordResponse
    {
        internal Modal? Form; internal bool FailAck; internal int Acks; internal string Text = "";
        public Task Defer() { Acks++; return FailAck ? Task.FromException(new IOException()) : Task.CompletedTask; }
        public Task Modal(Modal modal) { Form = modal; return Task.CompletedTask; }
        public Task Screen(DiscordScreen screen, bool replace, bool deferred) { Text = screen.Text; return Task.CompletedTask; }
    }
}
