using PartyViewKR;

static class GameChatCaptureTests
{
    internal static void Run(Action<string, Action> test, Action<bool, string> check)
    {
        test("chat capture accepts party cross world party incoming tells and alliance only", () => {
            var source = new LegacySource(); var captured = new List<GameChatMessage>();
            using var capture = new GameChatCapture(source, typeof(LegacySource), _ => true, captured.Add, id => "초코보", _ => { });
            foreach (var type in new ushort[] { 10, 11, 12, 13, 14, 15, 16, 24, 32, 37, 80 }) source.Fire(type);
            check(captured.Select(c => c.Kind).SequenceEqual([GameChatKind.Tell, GameChatKind.Party, GameChatKind.Alliance, GameChatKind.Party]), "unexpected chat categories");
            check(captured.All(c => c.Name == "모험가" && c.World == "초코보" && c.Text == "안녕하세요." && c.At.ToUnixTimeSeconds() == 1792935309), "sender payload or timestamp lost");
        });
        test("chat legacy callback preserves handled flag sender and message references", () => {
            var source = new LegacySource(); using var capture = new GameChatCapture(source, typeof(LegacySource), _ => true, _ => { }, _ => "모그리", _ => { });
            source.Fire(14, handled: true); check(source.Unchanged, "chat event was modified");
        });
        test("chat object event reads inherited explicit public interface members without consuming chat", () => {
            var source = new CurrentSource(); var captured = new List<GameChatMessage>();
            using var capture = new GameChatCapture(source, typeof(CurrentSource), _ => true, captured.Add, _ => "카벙클", _ => { });
            var message = new CurrentMessage(); source.Fire(message);
            check(captured.Count == 1 && captured[0].World == "카벙클" && !message.Prevented, "current event not copied or consumed");
        });
        test("chat disabled capture does not read payloads and disposal unsubscribes both event shapes", () => {
            var source = new CurrentSource(); var message = new CurrentMessage { ThrowPayload = true }; var errors = 0;
            var capture = new GameChatCapture(source, typeof(CurrentSource), _ => false, _ => throw new Exception(), _ => "", _ => errors++);
            source.Fire(message); check(message.Reads == 0 && errors == 0, "disabled capture read chat");
            capture.Dispose(); capture.Dispose(); check(source.Subscribers == 0, "object listener retained");
            var legacy = new LegacySource(); using var old = new GameChatCapture(legacy, typeof(LegacySource), _ => true, _ => { }, _ => "", _ => { });
            old.Dispose(); check(legacy.Subscribers == 0, "legacy listener retained");
        });
        test("chat missing server metadata remains unknown instead of inventing the local server", () => {
            var source = new LegacySource(); var captured = new List<GameChatMessage>();
            using var capture = new GameChatCapture(source, typeof(LegacySource), _ => true, captured.Add, _ => throw new Exception(), _ => { });
            source.Fire(13, payload: false); check(captured.Count == 1 && captured[0].World == "" && captured[0].Name == "보낸사람", "guessed world or dropped fallback sender");
        });
        test("same-world and zero-world senders use home world while explicit remote senders keep their world", () => {
            var source = new LegacySource(); var captured = new List<GameChatMessage>();
            using var capture = new GameChatCapture(source, typeof(LegacySource), _ => true, captured.Add,
                id => id == 71 ? "카벙클" : "", _ => { }, () => "초코보");
            source.Fire(13, payload: false);
            source.Fire(14, customPayloads: [new PlayerPayload { World = new() { RowId = 0 } }]);
            source.Fire(15);
            source.Fire(13, customPayloads: [new PlayerPayload { World = new() { RowId = 999 } }]);
            check(captured.Select(c => c.World).SequenceEqual(new[] { "초코보", "초코보", "카벙클", "" }), "local fallback overrode remote metadata");
        });
        test("unresolved cross-world sender markers never fall back to home world", () => {
            var source = new LegacySource(); var captured = new List<GameChatMessage>();
            using var capture = new GameChatCapture(source, typeof(LegacySource), _ => true, captured.Add, _ => "", _ => { }, () => "초코보");
            source.Fire(14, payload: false, name: "모험가@다른서버");
            source.Fire(14, customPayloads: [new PlayerPayload { World = new() { RowId = 0 } }, new IconPayload()]);
            check(captured.Count == 2 && captured.All(c => c.World == ""), "unresolved remote sender mislabeled local");
        });
        test("chat identity survives zoning without AFK and clears across logout and character changes", () => {
            var session = new GameChatSession(); var reads = 0;
            string Home() { reads++; return "초코보"; }
            session.Observe(true, false, 0, Home); check(session.SelfId == 0 && reads == 0, "invented login identity");
            session.Observe(true, true, 10, Home); session.Observe(true, true, 10, Home);
            session.Observe(true, false, 0, Home);
            check(session.SelfId == 10 && session.HomeWorld == "초코보" && reads == 1, "zoning discarded identity or re-read world every frame");
            session.Observe(false, false, 0, Home);
            check(session.SelfId == 0 && session.HomeWorld == "", "logout retained character");
            session.Observe(true, false, 0, Home); check(session.SelfId == 0, "new login reused old identity");
            session.Observe(true, true, 20, () => "카벙클");
            check(session.SelfId == 20 && session.HomeWorld == "카벙클", "new character inherited home world");
            session.Observe(true, true, 30, () => "");
            check(session.HomeWorld == "", "unreadable new world kept old server");
            session.Observe(true, true, 30, () => "모그리"); check(session.HomeWorld == "모그리", "home world lookup never recovered");
        });
        test("chat capture failures expose no chat text and do not escape the event callback", () => {
            var source = new CurrentSource(); var error = "";
            using var capture = new GameChatCapture(source, typeof(CurrentSource), _ => true, _ => { }, _ => "", e => error = e);
            source.Fire(new CurrentMessage { ThrowPayload = true });
            check(error.Length > 0 && !error.Contains("sensitive"), "raw message escaped diagnostics");
        });
    }
    public sealed class SeString
    {
        public string TextValue { get; init; } = "";
        public object[] Payloads { get; init; } = [];
    }
    public sealed class PlayerPayload
    {
        public string PlayerName => "모험가";
        public RowRef World { get; init; } = new();
    }
    public sealed class RowRef { public uint RowId { get; init; } = 71; }
    public sealed class IconPayload { public string Icon => "CrossWorld"; }
    public delegate void Legacy(ushort type, int timestamp, ref SeString sender, ref SeString message, ref bool handled);
    public sealed class LegacySource
    {
        public event Legacy? ChatMessage;
        internal int Subscribers => ChatMessage?.GetInvocationList().Length ?? 0;
        internal bool Unchanged;
        internal void Fire(ushort type, bool handled = false, bool payload = true, object[]? customPayloads = null, string name = "보낸사람")
        {
            var sender = new SeString { TextValue = name, Payloads = customPayloads ?? (payload ? [new PlayerPayload()] : []) };
            var message = new SeString { TextValue = "안녕하세요." }; var s = sender; var m = message; var h = handled;
            ChatMessage?.Invoke(type, 1792935309, ref s, ref m, ref h);
            Unchanged = ReferenceEquals(sender, s) && ReferenceEquals(message, m) && h == handled;
        }
    }
    public interface IMessage
    {
        ushort LogKind { get; }
        int Timestamp { get; }
        SeString Sender { get; }
        SeString Message { get; }
    }
    public interface IHandleable : IMessage { void PreventOriginal(); }
    public sealed class CurrentMessage : IHandleable
    {
        internal bool Prevented, ThrowPayload;
        internal int Reads;
        ushort IMessage.LogKind => 14;
        int IMessage.Timestamp => 1792935309;
        SeString IMessage.Sender { get { Reads++; if (ThrowPayload) throw new Exception("sensitive"); return new() { Payloads = [new PlayerPayload()] }; } }
        SeString IMessage.Message => new() { TextValue = "내용" };
        public void PreventOriginal() => Prevented = true;
    }
    public sealed class CurrentSource
    {
        public event Action<IHandleable>? ChatMessage;
        internal int Subscribers => ChatMessage?.GetInvocationList().Length ?? 0;
        internal void Fire(IHandleable message) => ChatMessage?.Invoke(message);
    }
}
