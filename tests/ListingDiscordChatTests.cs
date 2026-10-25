using System.Collections.Concurrent;
using System.Text;
using Discord;
using PartyViewKR;

static class ListingDiscordChatTests
{
    internal static readonly DiscordChatRoute Party = new() { Kind = "party", GuildId = 700, ChannelId = 800 };
    internal static DiscordChatSettings Settings(params DiscordChatRoute[] routes) => new() { ApplicationId = 500, Routes = routes };
    internal static GameChatMessage Chat(GameChatKind kind = GameChatKind.Party, string text = "안녕하세요.") => new(kind, "모험가", "초코보", text, DateTimeOffset.FromUnixTimeSeconds(1792935309));
    private static void Done(Task task) => task.WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
    private static void Wait(Func<bool> condition) { if (!SpinWait.SpinUntil(condition, TimeSpan.FromSeconds(5))) throw new Exception("chat fixture timed out"); }
    private static void Drain(ListingDiscordChat chat) => Wait(() => chat.PendingCount == 0);
    internal static void Run(Action<string, Action> test, Action<bool, string> check)
    {
        ListingDiscordChat Service(Transport t, DiscordChatSettings? settings = null,
            Func<DiscordChatSettings, CancellationToken, Task>? save = null) => new(500, t, settings,
                save ?? ((_, _) => Task.CompletedTask), CancellationToken.None);
        test("chat routes survive configuration reload without changing credentials or notification target", () => {
            var config = new RelayConfiguration { EncryptedDiscordToken = "encrypted", DiscordNotificationChannelId = 999,
                DiscordChat = Settings(Party, Party with { Kind = "tell", ChannelId = 801, MentionEveryone = true }) };
            var loaded = Newtonsoft.Json.JsonConvert.DeserializeObject<RelayConfiguration>(Newtonsoft.Json.JsonConvert.SerializeObject(config))!;
            check(loaded.DiscordChat!.ForApplication(500).Routes.SequenceEqual(config.DiscordChat.Routes) &&
                loaded.EncryptedDiscordToken == "encrypted" && loaded.DiscordNotificationChannelId == 999, "routing did not survive reload");
            check(loaded.DiscordChat.ForApplication(501).Routes.Length == 0, "new bot inherited old channels");
        });
        test("chat capture requires AFK a matching character and an explicitly configured category", () => {
            var t = new Transport(); using var n = Service(t, Settings(Party));
            check(!n.Receive(10, Chat()) && !n.Wants(GameChatKind.Party), "captured outside AFK");
            n.Observe(true, 10);
            check(!n.Receive(20, Chat()) && !n.Receive(10, Chat(GameChatKind.Tell)), "unconfigured or wrong character forwarded");
            check(n.Receive(10, Chat()), "configured AFK channel rejected"); Drain(n); check(t.Sent.Count == 1, "no AFK delivery");
        });
        test("chat independent category routes preserve receive order and duplicate text messages", () => {
            var t = new Transport(); using var n = Service(t, Settings(Party,
                Party with { Kind = "tell", ChannelId = 801, MentionEveryone = true }, Party with { Kind = "alliance", ChannelId = 802 }));
            n.Observe(true, 10);
            n.Receive(10, Chat()); n.Receive(10, Chat(GameChatKind.Tell)); n.Receive(10, Chat(GameChatKind.Alliance)); n.Receive(10, Chat()); Drain(n);
            check(t.Sent.Select(s => s.Route.ChannelId).SequenceEqual(new ulong[] { 800, 801, 802, 800 }), "messages reordered or text deduplicated");
            check(t.Sent.Select(s => s.Route.MentionEveryone).SequenceEqual([false, true, false, false]), "mentions leaked between categories");
        });
        test("chat AFK stop and character switch cancel preflight and clear queued messages", () => {
            foreach (var different in new[] { false, true }) {
                var t = new Transport { Gate = new(TaskCreationOptions.RunContinuationsAsynchronously) }; using var n = Service(t, Settings(Party));
                n.Observe(true, 10); n.Receive(10, Chat()); Done(t.CheckStarted.Task); n.Receive(10, Chat());
                n.Observe(different, different ? 20UL : 0UL); Drain(n);
                check(t.Sent.Count == 0, "old AFK session sent chat");
            }
        });
        test("chat configuration never posts messages and omitted mention defaults off then preserves its setting", () => {
            var t = new Transport(); DiscordChatSettings? saved = null;
            using var n = Service(t, save: (s, _) => { saved = s; return Task.CompletedTask; });
            Done(n.Configure(new("설정", "party", 800), 700, CancellationToken.None));
            check(saved!.Routes.Single().MentionEveryone == false && t.Sent.IsEmpty, "default mention or config test post");
            Done(n.Configure(new("멘션", "party", MentionEveryone: true), 700, CancellationToken.None));
            Done(n.Configure(new("설정", "party", 801), 700, CancellationToken.None));
            check(saved.Routes.Single() is { ChannelId: 801, MentionEveryone: true }, "mention command not preserved");
        });
        test("chat failed persistence and missing channel permissions leave the original route intact", () => {
            foreach (var permission in new[] { false, true }) {
                var t = new Transport { Error = permission ? "권한 필요" : null };
                using var n = Service(t, Settings(Party), (_, _) => throw new IOException("sensitive"));
                var result = n.Configure(new("설정", "party", 900), 700, CancellationToken.None).GetAwaiter().GetResult();
                check(!result.Contains("sensitive"), "exception body leaked");
                t.Error = null; n.Observe(true, 10); n.Receive(10, Chat()); Drain(n);
                check(t.Sent.Single().Route.ChannelId == 800, "failed configuration redirected chat");
            }
        });
        test("chat changing one route discards its queued messages without redirecting other categories", () => {
            var t = new Transport { Connected = false }; using var n = Service(t, Settings(Party, Party with { Kind = "tell", ChannelId = 801 }));
            n.Observe(true, 10); n.Receive(10, Chat()); n.Receive(10, Chat(GameChatKind.Tell));
            Done(n.Configure(new("설정", "party", 802), 700, CancellationToken.None));
            t.Connected = true; Drain(n); n.Receive(10, Chat()); Drain(n);
            check(t.Sent.Select(s => s.Route.ChannelId).SequenceEqual(new ulong[] { 801, 802 }), "old messages redirected or unrelated chat lost");
        });
        test("chat remove command stops only its category and settings can be inspected without AFK", () => {
            var t = new Transport(); using var n = Service(t, Settings(Party, Party with { Kind = "tell", ChannelId = 801 }));
            Done(n.Configure(new("해제", "party"), 700, CancellationToken.None)); n.Observe(true, 10);
            check(!n.Receive(10, Chat()) && n.Receive(10, Chat(GameChatKind.Tell)), "wrong category disabled"); Drain(n);
            var status = n.Configure(new("상태"), 700, CancellationToken.None).GetAwaiter().GetResult();
            check(status.Contains("파티 · 수신 꺼짐") && status.Contains("<#801>"), "missing route status");
        });
        test("chat rejects DM wrong server mutations invalid categories and missing mention options", () => {
            var t = new Transport(); var saves = 0; using var n = Service(t, Settings(Party), (_, _) => { saves++; return Task.CompletedTask; });
            Done(n.Configure(new("설정", "party", 800), 0, CancellationToken.None));
            Done(n.Configure(new("멘션", "party", MentionEveryone: true), 900, CancellationToken.None));
            Done(n.Configure(new("해제", "party"), 900, CancellationToken.None));
            Done(n.Configure(new("설정", "shout", 800), 700, CancellationToken.None));
            Done(n.Configure(new("멘션", "party"), 700, CancellationToken.None));
            check(saves == 0 && t.Sent.IsEmpty, "invalid settings mutation");
        });
        test("chat temporary disconnect queues messages until reconnect with bounded overflow reporting", () => {
            var t = new Transport { Connected = false }; using var n = Service(t, Settings(Party)); n.Observe(true, 10);
            for (var i = 0; i < 201; i++) n.Receive(10, Chat(text: i.ToString()));
            check(n.PendingCount == 200 && n.Status.Contains("제외 1") && t.Sent.IsEmpty, "unbounded or silent offline queue");
            t.Connected = true; Drain(n);
            check(t.Sent.Count == 200 && t.Sent.First().Message.Text == "1" && t.Sent.Last().Message.Text == "200", "reconnect lost queue order");
        });
        test("chat uncertain POST failure is not retried and does not stop later messages", () => {
            var t = new Transport { Fail = true }; using var n = Service(t, Settings(Party)); n.Observe(true, 10);
            n.Receive(10, Chat()); Drain(n); check(t.Sent.Count == 1 && n.Status.Contains("실패 1") && !n.Status.Contains("sensitive"), "failure hidden or replayed");
            t.Fail = false; n.Receive(10, Chat(text: "다음")); Drain(n);
            check(t.Sent.Count == 2 && t.Sent.Last().Message.Text == "다음", "queue stopped after failure");
        });
        test("chat settings cancellation during save does not activate a new destination", () => {
            var t = new Transport(); var queued = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var finish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var c = new CancellationTokenSource();
            using var n = Service(t, Settings(Party), (_, _) => { queued.TrySetResult(); return finish.Task; });
            var result = n.Configure(new("설정", "party", 900), 700, c.Token); Done(queued.Task); c.Cancel();
            try { Done(result); throw new Exception("cancel ignored"); } catch (OperationCanceledException) { }
            finish.TrySetResult(); n.Observe(true, 10); n.Receive(10, Chat()); Drain(n);
            check(t.Sent.Single().Route.ChannelId == 800, "late save activated route");
        });
        test("chat cards use three distinct colors and preserve complete Unicode text across parts", () => {
            check(Enum.GetValues<GameChatKind>().Select(DiscordChatFormatting.Color).SequenceEqual(new uint[] { 0x4D9CFF, 0xFF77B7, 0xFFA34D }), "chat colors changed");
            var body = string.Concat(Enumerable.Repeat("한글😀 @everyone <@123> **내용**\n", 350));
            var cards = DiscordChatFormatting.Render(Chat(text: body));
            var combined = string.Concat(cards.Select(c => c.Description)).Replace("\u200b", "");
            var plain = new StringBuilder(); for (var i = 0; i < combined.Length; i++) { if (combined[i] == '\\') i++; plain.Append(combined[i]); }
            check(plain.ToString() == "모험가@초코보 : " + body && cards.Length > 1 && cards.All(c => c.Description.Length <= 4000 && c.Timestamp == Chat().At), "truncated chat or invalid split");
            check(cards.All(c => !c.Description.Contains("@everyone") && !c.Description.Contains("<@123>")), "game text still contains active mention syntax");
        });
        test("chat command authorization ACK failure and duplicate IDs never mutate routes", () => {
            var t = new Transport(); var saves = 0; using var n = Service(t, save: (_, _) => { saves++; return Task.CompletedTask; });
            var session = new ListingDiscordSession(new("test"), 500, 600, CancellationToken.None, chat: n);
            DiscordInput Input(ulong id) => new(id, 500, 600, "pvkr-chat", "", [], []) { GuildId = 700, ChatCommand = new("설정", "party", 800) };
            Done(session.Process(Input(1) with { UserId = 9 }, new Response())); Done(session.Process(Input(2) with { ApplicationId = 9 }, new Response()));
            Done(session.Process(Input(3), new Response { FailAck = true })); check(saves == 0, "unauthorized config or action before ACK");
            Done(session.Process(Input(4), new Response())); Done(session.Process(Input(4), new Response()));
            check(saves == 1 && t.Sent.IsEmpty, "duplicate mutation or command posted chat");
        });
    }
    private sealed class Response : IDiscordResponse
    {
        internal bool FailAck;
        public Task Defer() => FailAck ? Task.FromException(new IOException()) : Task.CompletedTask;
        public Task Modal(Modal modal) => throw new NotSupportedException();
        public Task Screen(DiscordScreen screen, bool replace, bool deferred) => Task.CompletedTask;
    }
    private sealed class Transport : IDiscordChatTransport
    {
        public bool Connected { get; set; } = true;
        internal readonly ConcurrentQueue<(DiscordChatRoute Route, GameChatMessage Message)> Sent = new();
        internal readonly TaskCompletionSource CheckStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource? Gate;
        internal string? Error;
        internal bool Fail;
        public async Task<string?> CheckChannel(DiscordChatRoute route, CancellationToken cancellation)
        {
            CheckStarted.TrySetResult(); if (Gate != null) await Gate.Task.WaitAsync(cancellation);
            cancellation.ThrowIfCancellationRequested(); return Error;
        }
        public Task Send(DiscordChatRoute route, GameChatMessage message, CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested(); Sent.Enqueue((route, message));
            return Fail ? Task.FromException(new IOException("sensitive")) : Task.CompletedTask;
        }
    }
}
