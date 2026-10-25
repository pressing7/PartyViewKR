using System.Collections.Concurrent;
using System.Text.Json;
using PartyViewKR;

static partial class ListingDiscordConnectionTests
{
    private static void RunChatSdk(Action<string, Action> test, Action<bool, string> check)
    {
        ListingDiscordClient ConnectedChat(OfflineTransport transport, DiscordChatSettings? settings = null,
            Func<DiscordChatSettings, CancellationToken, Task>? save = null, ulong permissions = 150528)
        {
            var client = new ListingDiscordClient(new("test"), FakeToken, transport.Config(), chatSettings: settings, saveChatSettings: save);
            Wait(() => transport.Identified.Task.IsCompleted || !client.Running);
            transport.Ready(true).GetAwaiter().GetResult(); transport.NotificationGuild(permissions, [800, 801, 802]).GetAwaiter().GetResult();
            Wait(() => client.Connected || !client.Running); check(client.Connected, client.Status); return client;
        }
        var party = ListingDiscordChatTests.Party;
        test("Discord SDK registers pvkr chat separately with localized choices and text channel picker", () => {
            var t = new OfflineTransport(); using var client = ConnectedChat(t);
            check(t.CommandBodies.Select(b => b.GetProperty("name").GetString()).SequenceEqual(["pvkr", "pvkr-chat"]), "pvkr command replaced");
            var options = t.CommandBodies.Last().GetProperty("options").EnumerateArray().ToArray();
            check(options.Select(o => o.GetProperty("name").GetString()).SequenceEqual(["설정", "멘션", "해제", "상태"]), "missing chat commands");
            var channel = options[0].GetProperty("options").EnumerateArray().Single(o => o.GetProperty("name").GetString() == "채널");
            check(channel.GetProperty("type").GetInt32() == 7 && channel.GetProperty("channel_types")[0].GetInt32() == 0, "channel picker not restricted to text");
            client.Dispose(); client.Completion.GetAwaiter().GetResult();
        });
        test("Discord SDK posts persistent colored chat with exact sender and no mentions by default", () => {
            var t = new OfflineTransport(); using var client = ConnectedChat(t, ListingDiscordChatTests.Settings(party));
            client.ObserveChat(true, 10); client.ReceiveChat(10, ListingDiscordChatTests.Chat(text: "안녕 @everyone <@600>"));
            Wait(() => t.ChatMessages.Count == 1 || client.ChatStatus.Contains("실패 1"));
            check(t.ChatMessages.TryPeek(out var result), client.ChatStatus); var body = result.Body;
            check(result.Endpoint == "channels/800/messages" && (!body.TryGetProperty("content", out var content) || string.IsNullOrEmpty(content.GetString())), "chat text escaped embed or wrong target");
            check(body.GetProperty("allowed_mentions").GetProperty("parse").GetArrayLength() == 0, "game text can trigger mention");
            check(!body.TryGetProperty("flags", out var flags) || flags.GetInt32() == 0, "ephemeral chat message");
            var card = body.GetProperty("embeds")[0];
            check(card.GetProperty("color").GetUInt32() == 0x4D9CFF && card.GetProperty("description").GetString()!.Contains("모험가@\u200b초코보 : 안녕"), "sender or color missing");
            check(t.Responses.IsEmpty && t.NotificationMessages.IsEmpty, "chat used temporary interaction response");
            client.Dispose(); client.Completion.GetAwaiter().GetResult();
        });
        test("Discord SDK delivers each chat category to its own channel and only configured everyone mentions", () => {
            var t = new OfflineTransport(); using var client = ConnectedChat(t, ListingDiscordChatTests.Settings(party,
                party with { Kind = "tell", ChannelId = 801, MentionEveryone = true }, party with { Kind = "alliance", ChannelId = 802 }));
            client.ObserveChat(true, 10);
            foreach (var kind in Enum.GetValues<GameChatKind>()) client.ReceiveChat(10, ListingDiscordChatTests.Chat(kind));
            Wait(() => t.ChatMessages.Count == 3 || client.ChatStatus.Contains("실패 1"));
            var sent = t.ChatMessages.ToArray(); check(sent.Length == 3, client.ChatStatus);
            check(sent.Select(s => s.Endpoint).SequenceEqual(["channels/800/messages", "channels/801/messages", "channels/802/messages"]), "category destination changed");
            check(sent.Select(s => s.Body.GetProperty("embeds")[0].GetProperty("color").GetUInt32()).SequenceEqual(new uint[] { 0x4D9CFF, 0xFF77B7, 0xFFA34D }), "wrong embed colors");
            check(sent[1].Body.GetProperty("content").GetString() == "@everyone" &&
                sent[1].Body.GetProperty("allowed_mentions").GetProperty("parse")[0].GetString() == "everyone", "configured mention missing");
            client.Dispose(); client.Completion.GetAwaiter().GetResult();
        });
        test("Discord SDK chat permission checks require embeds and everyone only when selected", () => {
            foreach (var permissions in new ulong[] { 3072, 19456 }) {
                var t = new OfflineTransport(); using var client = ConnectedChat(t, ListingDiscordChatTests.Settings(party with { MentionEveryone = true }), permissions: permissions);
                client.ObserveChat(true, 10); client.ReceiveChat(10, ListingDiscordChatTests.Chat()); Wait(() => client.ChatStatus.Contains("실패 1"));
                check(t.ChatMessages.IsEmpty && client.ChatStatus.Contains("권한"), "missing channel permission ignored");
                client.Dispose(); client.Completion.GetAwaiter().GetResult();
            }
            var ordinary = new OfflineTransport(); using var withoutPing = ConnectedChat(ordinary, ListingDiscordChatTests.Settings(party), permissions: 19456);
            withoutPing.ObserveChat(true, 10); withoutPing.ReceiveChat(10, ListingDiscordChatTests.Chat()); Wait(() => ordinary.ChatMessages.Count == 1);
            withoutPing.Dispose(); withoutPing.Completion.GetAwaiter().GetResult();
        });
        test("Discord SDK slash routing channel mention and removal work from guild interactions", () => {
            var t = new OfflineTransport(); var saved = new ConcurrentQueue<DiscordChatSettings>();
            using var client = ConnectedChat(t, save: (s, _) => { saved.Enqueue(s); return Task.CompletedTask; });
            t.ChatInteraction("설정", [new { name = "종류", type = 3, value = "tell" }, new { name = "채널", type = 7, value = "801" }, new { name = "멘션", type = 3, value = "everyone" }]);
            Wait(() => saved.Count == 1 || t.Responses.Any(r => r.Method == "PATCH"));
            check(saved.Count == 1 && saved.Last().Routes.Single() is { Kind: "tell", GuildId: 700, ChannelId: 801, MentionEveryone: true }, "slash settings not decoded");
            Wait(() => t.Responses.Any(r => r.Method == "PATCH"));
            check(t.Responses.First().Body.GetProperty("type").GetInt32() == 5 && t.ChatMessages.IsEmpty, "settings not ACKed or sent chat");
            t.ChatInteraction("멘션", [new { name = "종류", type = 3, value = "tell" }, new { name = "설정", type = 3, value = "off" }]);
            Wait(() => saved.Count == 2); check(!saved.Last().Routes.Single().MentionEveryone, "mention command ignored");
            t.ChatInteraction("해제", [new { name = "종류", type = 3, value = "tell" }]); Wait(() => saved.Count == 3);
            check(saved.Last().Routes.Length == 0, "disable command ignored");
            t.ChatInteraction("상태", []); Wait(() => t.Responses.Count(r => r.Method == "PATCH") == 4);
            check(saved.Count == 3 && t.ChatMessages.IsEmpty, "status changed config");
            client.Dispose(); client.Completion.GetAwaiter().GetResult();
        });
    }
    private sealed partial class OfflineTransport
    {
        internal readonly ConcurrentQueue<(string Endpoint, JsonElement Body)> ChatMessages = new();
        internal void ChatInteraction(string action, object[] options)
        {
            var id = Discord.SnowflakeUtils.ToSnowflake(DateTimeOffset.UtcNow) + (ulong)Interlocked.Increment(ref sequence);
            var data = new { id = "701", name = "pvkr-chat", type = 1,
                options = new[] { new { name = action, type = 1, options } },
                resolved = new { channels = new Dictionary<string, object> { ["801"] = new { id = "801", type = 0, name = "tells", permissions = "150528" } } } };
            Receive(JsonSerializer.Serialize(new { op = 0, s = sequence, t = "INTERACTION_CREATE", d = new {
                id = id.ToString(), application_id = "500", type = 2, data, guild_id = "700", channel_id = "800",
                token = "offline-chat-" + id, version = 1,
                member = new { user = new { id = "600", username = "owner", discriminator = "0" }, roles = Array.Empty<string>(),
                    joined_at = DateTimeOffset.UtcNow, deaf = false, mute = false, permissions = "150528" },
                entitlements = Array.Empty<object>(), authorizing_integration_owners = new Dictionary<string, string>(), app_permissions = "150528", context = 0
            } })).GetAwaiter().GetResult();
        }
    }
}
