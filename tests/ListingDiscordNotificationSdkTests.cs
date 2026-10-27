using System.Collections.Concurrent;
using System.Text.Json;
using PartyViewKR;

static partial class ListingDiscordConnectionTests
{
    private static void RunNotificationSdk(Action<string, Action> test, Action<bool, string> check)
    {
        test("Discord SDK completion POST serializes exact everyone mention once to selected server channel", () => {
            var transport = new OfflineTransport();
            using var client = new ListingDiscordClient(new("test"), FakeToken, transport.Config(), new(500, 700, 800));
            Wait(() => transport.Identified.Task.IsCompleted || !client.Running);
            transport.Ready(true).GetAwaiter().GetResult();
            transport.NotificationGuild().GetAwaiter().GetResult();
            Wait(() => client.Connected || !client.Running); check(client.Connected, client.Status);
            client.ObserveCompletion(true, 10, 1, true);
            Wait(() => transport.NotificationMessages.Count > 0 || client.NotificationStatus.Contains("실패"));
            check(transport.NotificationMessages.TryPeek(out var body), client.NotificationStatus);
            check(body.GetProperty("content").GetString() == "@everyone 파티모집완료", "wrong content");
            var mentions = body.GetProperty("allowed_mentions");
            check(mentions.GetProperty("parse").EnumerateArray().Select(x => x.GetString()).SequenceEqual(["everyone"]), "everyone not explicitly allowed");
            check(!body.TryGetProperty("flags", out var flags) || flags.GetInt32() == 0, "notification suppressed or ephemeral");
            for (var i = 0; i < 10; i++) client.ObserveCompletion(true, 10, 1, true);
            check(transport.NotificationMessages.Count == 1, "repeated ping");
            client.Dispose(); client.Completion.WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
        });
        test("Discord SDK rejects missing everyone channel permission before posting", () => {
            var transport = new OfflineTransport();
            using var client = new ListingDiscordClient(new("test"), FakeToken, transport.Config(), new(500, 700, 800));
            Wait(() => transport.Identified.Task.IsCompleted || !client.Running);
            transport.Ready(true).GetAwaiter().GetResult();
            transport.NotificationGuild(3072).GetAwaiter().GetResult();
            Wait(() => client.Connected || !client.Running);
            client.ObserveCompletion(true, 10, 1, true); Wait(() => client.NotificationStatus.Contains("실패"));
            check(transport.NotificationMessages.IsEmpty && client.NotificationStatus.Contains("권한"), "silently posted without mention permission");
            client.Dispose(); client.Completion.WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
        });
        test("Discord SDK reserved-seat completion uses its own text and deduplicates everyone", () => {
            var t = new OfflineTransport();
            using var client = new ListingDiscordClient(new("test"), FakeToken, t.Config(), new(500, 700, 800));
            Wait(() => t.Identified.Task.IsCompleted || !client.Running);
            t.Ready(true).GetAwaiter().GetResult(); t.NotificationGuild().GetAwaiter().GetResult();
            Wait(() => client.Connected || !client.Running);
            client.ObserveCompletion(true, 10, 1, true, true);
            Wait(() => client.NotificationStatus.Contains("전송 완료") || client.NotificationStatus.Contains("실패"));
            check(client.NotificationStatus.Contains("전송 완료") && t.NotificationMessages.Single().GetProperty("content").GetString() ==
                "@everyone 단일 직업 빈자리를 제외한 파티모집완료", "wrong partial-completion alert");
            for (var i = 0; i < 10; i++) client.ObserveCompletion(true, 10, 1, true, true);
            check(t.NotificationMessages.Count == 1, "duplicate reserved-seat notification");
            client.Dispose(); client.Completion.GetAwaiter().GetResult();
        });
        test("Discord SDK does not report success or repeat POST if the server did not confirm everyone", () => {
            var transport = new OfflineTransport { DropEveryoneMention = true };
            using var client = new ListingDiscordClient(new("test"), FakeToken, transport.Config(), new(500, 700, 800));
            Wait(() => transport.Identified.Task.IsCompleted || !client.Running);
            transport.Ready(true).GetAwaiter().GetResult(); transport.NotificationGuild().GetAwaiter().GetResult();
            Wait(() => client.Connected || !client.Running);
            client.ObserveCompletion(true, 10, 1, true); Wait(() => client.NotificationStatus.Contains("실패"));
            for (var i = 0; i < 10; i++) client.ObserveCompletion(true, 10, 1, true);
            check(transport.NotificationMessages.Count == 1, "unconfirmed mention POST repeated");
            client.Dispose(); client.Completion.WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
        });
    }

    private sealed partial class OfflineTransport
    {
        internal readonly ConcurrentQueue<JsonElement> NotificationMessages = new();
        internal bool DropEveryoneMention;
        internal Task NotificationGuild(ulong permissions = 134144, ulong[]? channelIds = null) => Receive(JsonSerializer.Serialize(new {
            op = 0, s = ++sequence, t = "GUILD_CREATE", d = new {
                id = "700", name = "offline notification tests", owner_id = "600", unavailable = false,
                roles = new[] { new { id = "700", name = "@everyone", permissions = permissions.ToString(), position = 0,
                    color = 0, hoist = false, managed = false, mentionable = false } },
                channels = (channelIds ?? [800UL]).Select(id => new { id = id.ToString(), guild_id = "700", type = 0, name = "chat-" + id, position = 0,
                    permission_overwrites = Array.Empty<object>() }).ToArray(),
                members = new[] { new { user = new { id = "500", username = "PVKR", discriminator = "0", bot = true },
                    roles = Array.Empty<string>(), joined_at = DateTimeOffset.UtcNow, deaf = false, mute = false } },
                guild_scheduled_events = Array.Empty<object>(), emojis = Array.Empty<object>(), features = Array.Empty<string>(), voice_states = Array.Empty<object>(),
                presences = Array.Empty<object>(), threads = Array.Empty<object>(), member_count = 1, large = false,
                verification_level = 0, default_message_notifications = 0, explicit_content_filter = 0, mfa_level = 0,
                afk_timeout = 300, system_channel_flags = 0, premium_tier = 0, nsfw_level = 0, preferred_locale = "ko"
            }
        }));
    }
}
