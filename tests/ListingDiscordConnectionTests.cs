using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using Discord;
using Discord.Net;
using Discord.Net.Rest;
using Discord.Net.WebSockets;
using Discord.WebSocket;
using PartyViewKR;

static partial class ListingDiscordConnectionTests
{
    // No real token or network: exercise Discord.Net's REST/Gateway initialization itself.
    private const string FakeToken = "NTAw.fake.only-for-offline-tests-not-a-real-token";
    private static void Wait(Func<bool> condition)
    {
        if (!SpinWait.SpinUntil(condition, TimeSpan.FromSeconds(5))) throw new Exception("Discord fixture timed out");
    }
    internal static void Run(Action<string, Action> test, Action<bool, string> check)
    {
        test("discord SDK waits for READY application ID before registering pvkr", () => {
            var transport = new OfflineTransport();
            using var client = new ListingDiscordClient(new("test"), FakeToken, transport.Config());
            Wait(() => transport.Identified.Task.IsCompleted || !client.Running);
            check(transport.Identified.Task.IsCompleted, "Gateway was not reached: " + client.Status);
            check(transport.Commands.IsEmpty && transport.EmojiCalls.IsEmpty && !client.Connected, "application API called before READY");
            transport.Ready().GetAwaiter().GetResult();
            Wait(() => client.Connected || !client.Running);
            check(client.Connected, client.Status);
            check(transport.Commands.SequenceEqual(["applications/500/commands", "applications/500/commands"]), "missing or incorrect application ID");
            check(client.Status.StartsWith("연결됨") && client.InviteUrl.Contains("client_id=500"), "connected status missing");
            transport.Ready().GetAwaiter().GetResult();
            check(transport.Commands.Count == 2, "repeated READY registered the commands again");
            client.Dispose(); client.Completion.WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
        });
        test("discord cancellation before READY never registers commands", () => {
            var transport = new OfflineTransport();
            using var client = new ListingDiscordClient(new("test"), FakeToken, transport.Config());
            Wait(() => transport.Identified.Task.IsCompleted || !client.Running);
            check(transport.Identified.Task.IsCompleted, client.Status);
            client.Dispose(); client.Completion.WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
            check(transport.Commands.IsEmpty && !client.Running && !client.Connected, "cancelled connection continued");
        });
        test("discord command registration failure reports stage and safe numeric codes", () => {
            var transport = new OfflineTransport { RejectCommands = true };
            using var client = new ListingDiscordClient(new("test"), FakeToken, transport.Config());
            Wait(() => transport.Identified.Task.IsCompleted || !client.Running);
            check(transport.Identified.Task.IsCompleted, client.Status);
            transport.Ready().GetAwaiter().GetResult();
            client.Completion.WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
            check(!client.Connected && !client.Running && client.Status.Contains("/pvkr 명령어 등록"), "failure stage lost");
            check(client.Status.Contains("HTTP 403") && client.Status.Contains("Discord 50001"), "error codes lost");
            check(!client.Status.Contains(FakeToken), "server error body leaked");
        });
        test("discord invalid token response identifies bot information stage", () => {
            var transport = new OfflineTransport { RejectAuth = true };
            using var client = new ListingDiscordClient(new("test"), FakeToken, transport.Config());
            client.Completion.WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
            check(!client.Running && client.Status.Contains("봇 정보 확인") && client.Status.Contains("HTTP 401"), "auth error unclear");
            check(!transport.Identified.Task.IsCompleted && transport.Commands.IsEmpty && !client.Status.Contains(FakeToken), "invalid login proceeded or leaked token");
        });
        test("discord diagnostics never include exception messages or request contents", () => {
            foreach (var error in new Exception[] { new IOException(FakeToken), new WebSocketClosedException(4004, FakeToken),
                new HttpException(HttpStatusCode.BadRequest, null!, (DiscordErrorCode)50035, FakeToken) }) {
                var text = ListingDiscordClient.FailureDetails("test", error);
                check(text.Contains("test") && !text.Contains(FakeToken), "exception details leaked");
            }
        });
        test("discord SDK lists application emojis and uploads a missing embedded PNG after READY", () => {
            var transport = new OfflineTransport { MissingEmoji = "join_tank_healer_role" };
            using var client = new ListingDiscordClient(new("test"), FakeToken, transport.Config());
            Wait(() => transport.Identified.Task.IsCompleted || !client.Running);
            check(transport.EmojiCalls.IsEmpty, "emoji API called before application ID is known");
            transport.Ready().GetAwaiter().GetResult();
            Wait(() => client.Status.Contains("이모지 준비 완료") || !client.Running);
            check(client.Connected && client.Status.Contains("80/80"), client.Status);
            check(transport.EmojiCalls.SequenceEqual(new[] { "GET", "POST:join_tank_healer_role" }), "unexpected emoji API calls");
            check(transport.UploadedEmojiPng, "embedded PNG not uploaded as image data");
            transport.Ready().GetAwaiter().GetResult();
            check(transport.EmojiCalls.Count == 2 && client.Status.Contains("80/80"), "Gateway resume uploaded twice or lost status");
            client.Dispose(); client.Completion.WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
        });
        test("discord emoji permission failures preserve connection and expose only safe codes", () => {
            foreach (var rejectList in new[] { false, true }) {
                var transport = new OfflineTransport { RejectEmojiList = rejectList, RejectEmojiCreate = !rejectList,
                    MissingEmoji = "join_tank_healer_role" };
                using var client = new ListingDiscordClient(new("test"), FakeToken, transport.Config());
                Wait(() => transport.Identified.Task.IsCompleted || !client.Running);
                transport.Ready().GetAwaiter().GetResult();
                Wait(() => client.Status.Contains("일부 미준비") || !client.Running);
                check(client.Connected && client.Running && client.Status.Contains("HTTP 403"), client.Status);
                check(client.Status.Contains(rejectList ? "0/80" : "79/80") && !client.Status.Contains(FakeToken), "partial progress or redaction lost");
                check(transport.EmojiCalls.Count == (rejectList ? 1 : 2), "failed list or upload retried blindly");
                client.Dispose(); client.Completion.WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
            }
        });
        test("discord SDK migrates the legacy emoji name using PATCH and preserves its ID", () => {
            foreach (var reject in new[] { false, true }) {
                var transport = new OfflineTransport { LegacyEmoji = "join_tank_healer_role", RejectEmojiRename = reject };
                using var client = new ListingDiscordClient(new("test"), FakeToken, transport.Config());
                Wait(() => transport.Identified.Task.IsCompleted || !client.Running);
                check(transport.EmojiCalls.IsEmpty, "rename ran before READY");
                transport.Ready().GetAwaiter().GetResult();
                Wait(() => client.Status.Contains(reject ? "일부 미준비" : "이모지 준비 완료") || !client.Running);
                check(client.Connected && client.Status.Contains(reject ? "79/80" : "80/80"), client.Status);
                check(transport.EmojiCalls.Count == 2 && transport.EmojiCalls.First() == "GET" &&
                    transport.EmojiCalls.Last().StartsWith("PATCH:") && transport.EmojiCalls.Last().EndsWith(":join_tank_healer_role"), "legacy emoji re-uploaded or wrong API used");
                check(!client.Status.Contains(FakeToken) && (!reject || client.Status.Contains("HTTP 403")), "rename failure diagnostics leaked or stopped connection");
                client.Dispose(); client.Completion.WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
            }
        });
        test("discord full restart restores saved token and reuses all server emojis without uploads", () => {
            var saved = new RelayConfiguration();
            check(saved.PrepareDiscordConnection(FakeToken, true, DiscordSettingsTests.Protect, DiscordSettingsTests.Unprotect, out var token),
                "initial connection settings rejected");
            var serverEmojis = ListingDiscordEmojiAssets.All.Take(79).Select((a, i) => (a.Name, Id: (ulong)(10000 + i)))
                .ToDictionary(e => e.Name, e => e.Id);
            var first = new OfflineTransport { SavedEmojis = serverEmojis };
            using (var client = new ListingDiscordClient(new("test"), token, first.Config())) {
                Wait(() => first.Identified.Task.IsCompleted || !client.Running);
                first.Ready().GetAwaiter().GetResult();
                Wait(() => client.Status.Contains("이모지 준비 완료") || !client.Running);
                check(client.Connected && client.Status.Contains("기존 79개 재사용") && client.Status.Contains("새 업로드 1개"), client.Status);
                check(first.EmojiCalls.Count == 2 && serverEmojis.Count == 80, "initial missing emoji was not registered");
                client.Dispose(); client.Completion.WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
            }
            var idsBefore = serverEmojis.ToDictionary(e => e.Key, e => e.Value);
            // Recreate settings, client and in-memory icon registry; only the
            // encrypted config and Discord's server-side emoji list survive.
            var restored = Newtonsoft.Json.JsonConvert.DeserializeObject<RelayConfiguration>(Newtonsoft.Json.JsonConvert.SerializeObject(saved))!;
            check(restored.ShouldAutoConnectDiscord && restored.PrepareDiscordConnection("", true,
                DiscordSettingsTests.Protect, DiscordSettingsTests.Unprotect, out token), "saved connection did not restore");
            var second = new OfflineTransport { SavedEmojis = serverEmojis };
            using (var client = new ListingDiscordClient(new("test"), token, second.Config())) {
                Wait(() => second.Identified.Task.IsCompleted || !client.Running);
                second.Ready().GetAwaiter().GetResult();
                Wait(() => client.Status.Contains("이모지 준비 완료") || !client.Running);
                check(client.Connected && client.Status.Contains("기존 80개 재사용") && client.Status.Contains("새 업로드 0개"), client.Status);
                check(second.EmojiCalls.SequenceEqual(["GET"]) && serverEmojis.All(e => idsBefore[e.Key] == e.Value),
                    "restart uploaded/renamed emojis or changed IDs");
                client.Dispose(); client.Completion.WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
            }
        });
        RunInteractions(test, check);
        RunNotificationSdk(test, check);
        RunChatSdk(test, check);
    }

    private sealed partial class OfflineTransport : IRestClient, IWebSocketClient
    {
        internal readonly TaskCompletionSource Identified = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal readonly ConcurrentQueue<string> Commands = new();
        internal readonly ConcurrentQueue<JsonElement> CommandBodies = new();
        internal readonly ConcurrentQueue<string> EmojiCalls = new();
        internal Dictionary<string, ulong>? SavedEmojis;
        internal string? MissingEmoji, LegacyEmoji;
        internal bool RejectEmojiList, RejectEmojiCreate, RejectEmojiRename, UploadedEmojiPng;
        internal readonly ConcurrentQueue<(string Method, string Endpoint, JsonElement Body)> Responses = new();
        internal JsonElement? Message;
        private int sequence = 1;
        private int messageId = 900;
        internal bool RejectCommands, RejectAuth;
        public event Func<string, Task>? TextMessage;
        public event Func<byte[], int, int, Task>? BinaryMessage { add { } remove { } }
        public event Func<Exception, Task>? Closed { add { } remove { } }
        internal DiscordSocketConfig Config() => new() {
            GatewayIntents = GatewayIntents.Guilds, AlwaysDownloadUsers = false, AlwaysDownloadDefaultStickers = false,
            MessageCacheSize = 0, LogLevel = LogSeverity.Critical,
            RestClientProvider = _ => this, WebSocketProvider = () => this
        };
        public void SetHeader(string key, string value) { }
        public void SetCancelToken(CancellationToken cancelToken) { }
        public void Dispose() { }
        public Task ConnectAsync(string host) => Receive("{\"op\":10,\"d\":{\"heartbeat_interval\":60000}}");
        public Task DisconnectAsync(int closeCode = 1000) => Task.CompletedTask;
        public Task SendAsync(byte[] data, int index, int count, bool isText)
        {
            using var doc = JsonDocument.Parse(data.AsMemory(index, count));
            var op = doc.RootElement.GetProperty("op").GetInt32();
            if (op == 2) Identified.TrySetResult();
            return op == 1 ? Receive("{\"op\":11,\"d\":null}") : Task.CompletedTask;
        }
        private Task Receive(string json) => TextMessage?.Invoke(json) ?? Task.CompletedTask;
        internal Task Ready(bool notificationGuild = false) => Receive("""
            {"op":0,"s":1,"t":"READY","d":{"v":10,"user":{"id":"500","username":"PVKR","discriminator":"0","bot":true},
            "guilds":[],"private_channels":[],"session_id":"offline","resume_gateway_url":"wss://offline.invalid",
            "application":{"id":"500","flags":0}}}
            """.Replace("\"guilds\":[]", notificationGuild ? "\"guilds\":[{\"id\":\"700\",\"unavailable\":true}]" : "\"guilds\":[]"));
        internal void Interaction(int type, object data, ulong user = 600)
        {
            var id = SnowflakeUtils.ToSnowflake(DateTimeOffset.UtcNow) + (ulong)Interlocked.Increment(ref sequence);
            var interaction = new Dictionary<string, object> {
                ["id"] = id.ToString(), ["application_id"] = "500", ["type"] = type, ["data"] = data,
                ["channel_id"] = "800", ["token"] = "offline-interaction-" + id, ["version"] = 1,
                ["user"] = new { id = user.ToString(), username = "owner", discriminator = "0" },
                ["entitlements"] = Array.Empty<object>(), ["authorizing_integration_owners"] = new Dictionary<string, string>(),
                ["app_permissions"] = "0", ["context"] = 1
            };
            if (type != 2 && Message is { } message) interaction["message"] = message;
            Receive(JsonSerializer.Serialize(new { op = 0, s = sequence, t = "INTERACTION_CREATE", d = interaction })).GetAwaiter().GetResult();
        }
        public Task<RestResponse> SendAsync(string method, string endpoint, CancellationToken cancelToken, bool headerOnly = false,
            string? reason = null, IEnumerable<KeyValuePair<string, IEnumerable<string>>>? requestHeaders = null) => Reply(method, endpoint);
        public Task<RestResponse> SendAsync(string method, string endpoint, string json, CancellationToken cancelToken, bool headerOnly = false,
            string? reason = null, IEnumerable<KeyValuePair<string, IEnumerable<string>>>? requestHeaders = null) => Reply(method, endpoint, json);
        public Task<RestResponse> SendAsync(string method, string endpoint, IReadOnlyDictionary<string, object> multipartParams,
            CancellationToken cancelToken, bool headerOnly = false, string? reason = null,
            IEnumerable<KeyValuePair<string, IEnumerable<string>>>? requestHeaders = null) => throw new NotSupportedException();
        private Task<RestResponse> Reply(string method, string endpoint, string? json = null)
        {
            var code = HttpStatusCode.OK;
            string body;
            if (endpoint == "oauth2/applications/@me") {
                if (RejectAuth) { code = HttpStatusCode.Unauthorized; body = JsonSerializer.Serialize(new { code = 0, message = FakeToken }); }
                else body = """{"id":"500","name":"PVKR","owner":{"id":"600","username":"owner","discriminator":"0","bot":false}}""";
            }
            else if (endpoint == "gateway/bot") body = """{"url":"wss://offline.invalid","shards":1,"session_start_limit":{"total":1000,"remaining":999,"reset_after":1000,"max_concurrency":1}}""";
            else if (method == "GET" && endpoint == "applications/500/emojis") {
                EmojiCalls.Enqueue("GET");
                if (RejectEmojiList) { code = HttpStatusCode.Forbidden; body = JsonSerializer.Serialize(new { code = 50001, message = FakeToken }); }
                else if (SavedEmojis != null) body = JsonSerializer.Serialize(new { items = SavedEmojis
                    .Select(e => new { id = e.Value.ToString(), name = e.Key, animated = false }).ToArray() });
                else body = JsonSerializer.Serialize(new { items = ListingDiscordEmojiAssets.All
                    .Where(a => a.Name != MissingEmoji).Select((a, i) => new { id = (10000 + i).ToString(),
                        name = a.Name == LegacyEmoji ? a.LegacyName! : a.Name, animated = false }).ToArray() });
            }
            else if (method == "POST" && endpoint == "applications/500/emojis") {
                using var parsed = JsonDocument.Parse(json!);
                var name = parsed.RootElement.GetProperty("name").GetString()!;
                EmojiCalls.Enqueue("POST:" + name);
                var image = parsed.RootElement.GetProperty("image").GetString()!;
                var png = Convert.FromBase64String(image[(image.IndexOf(',') + 1)..]);
                // Discord.Net 3.20.2 labels every Image stream image/jpeg; it
                // sends the original bytes. Verify PNG/alpha data is unmodified.
                using var original = ListingDiscordEmojiAssets.All.Single(a => a.Name == name).Open();
                using var expected = new MemoryStream(); original.CopyTo(expected);
                UploadedEmojiPng = image.StartsWith("data:image/") && png.Length <= 256 * 1024 &&
                    png.Take(8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) &&
                    png.SequenceEqual(expected.ToArray());
                if (RejectEmojiCreate) { code = HttpStatusCode.Forbidden; body = JsonSerializer.Serialize(new { code = 50001, message = FakeToken }); }
                else {
                    var id = (ulong)(20000 + (SavedEmojis?.Count ?? 0));
                    if (SavedEmojis != null) SavedEmojis[name] = id;
                    body = JsonSerializer.Serialize(new { id = id.ToString(), name, animated = false });
                }
            }
            else if (method == "PATCH" && endpoint.StartsWith("applications/500/emojis/")) {
                using var parsed = JsonDocument.Parse(json!);
                var name = parsed.RootElement.GetProperty("name").GetString()!;
                var id = endpoint.Split('/')[^1];
                var asset = ListingDiscordEmojiAssets.All.Single(a => a.Name == LegacyEmoji);
                if (id != (10000 + ListingDiscordEmojiAssets.All.ToList().IndexOf(asset)).ToString() ||
                    name != asset.Name || parsed.RootElement.EnumerateObject().Count() != 1)
                    throw new Exception("rename must preserve ID and modify only name");
                EmojiCalls.Enqueue("PATCH:" + id + ":" + name);
                if (RejectEmojiRename) { code = HttpStatusCode.Forbidden; body = JsonSerializer.Serialize(new { code = 50001, message = FakeToken }); }
                else body = JsonSerializer.Serialize(new { id, name, animated = false });
            }
            else if (method == "POST" && endpoint.EndsWith("/commands")) {
                Commands.Enqueue(endpoint);
                using var command = JsonDocument.Parse(json!); CommandBodies.Enqueue(command.RootElement.Clone());
                if (endpoint != "applications/500/commands" || RejectCommands) {
                    code = HttpStatusCode.Forbidden; body = JsonSerializer.Serialize(new { code = 50001, message = FakeToken });
                }
                else body = JsonSerializer.Serialize(new { id = "700", application_id = "500", name = command.RootElement.GetProperty("name").GetString(), type = 1, description = "PVKR", version = "1" });
            }
            else if (method == "POST" && endpoint is "channels/800/messages" or "channels/801/messages" or "channels/802/messages") {
                using var parsed = JsonDocument.Parse(json!);
                if (parsed.RootElement.TryGetProperty("embeds", out var embeds) && embeds.GetArrayLength() > 0)
                    ChatMessages.Enqueue((endpoint, parsed.RootElement.Clone()));
                else NotificationMessages.Enqueue(parsed.RootElement.Clone());
                // Embed-only requests may omit content, but Discord's Message
                // response uses an empty string. Null crashes SDK tag parsing.
                var content = parsed.RootElement.TryGetProperty("content", out var text) ? text.GetString() ?? "" : "";
                body = JsonSerializer.Serialize(new {
                    id = (++messageId).ToString(), channel_id = endpoint.Split('/')[1], type = 0,
                    author = new { id = "500", username = "PVKR", discriminator = "0", bot = true },
                    timestamp = DateTimeOffset.UtcNow, content,
                    mention_everyone = !DropEveryoneMention && content.StartsWith("@everyone"), mentions = Array.Empty<object>(), mention_roles = Array.Empty<string>(),
                    attachments = Array.Empty<object>(), embeds = Array.Empty<object>(), flags = 0
                });
            }
            else if (method == "POST" && endpoint.StartsWith("interactions/") && endpoint.EndsWith("/callback")) {
                using var parsed = JsonDocument.Parse(json!);
                var response = parsed.RootElement.Clone();
                if (response.GetProperty("type").GetInt32() is 4 or 7) MakeMessage(response.GetProperty("data"), response.GetProperty("type").GetInt32() == 4);
                Responses.Enqueue((method, endpoint, response));
                code = HttpStatusCode.NoContent; body = "";
            }
            else if (method == "PATCH" && endpoint.StartsWith("webhooks/500/") && endpoint.EndsWith("/messages/@original")) {
                using var parsed = JsonDocument.Parse(json!);
                var response = parsed.RootElement.Clone();
                body = MakeMessage(response).GetRawText();
                Responses.Enqueue((method, endpoint, response));
            }
            else throw new InvalidOperationException("Unexpected offline REST endpoint: " + method + " " + endpoint);
            return Task.FromResult(new RestResponse(code, [], new MemoryStream(Encoding.UTF8.GetBytes(body))));
        }
        private JsonElement MakeMessage(JsonElement data, bool fresh = false)
        {
            if (fresh) messageId++;
            ValidateCard(data);
            Message = JsonSerializer.SerializeToElement(new {
                id = messageId.ToString(), channel_id = "800", type = 0,
                author = new { id = "500", username = "PVKR", discriminator = "0", bot = true },
                timestamp = DateTimeOffset.UtcNow, content = data.TryGetProperty("content", out var text) ? text.GetString() ?? "" : "",
                components = data.TryGetProperty("components", out var components) ? components : JsonSerializer.SerializeToElement(Array.Empty<object>()),
                flags = data.TryGetProperty("flags", out var flags) ? flags.GetInt32() : 0,
                attachments = Array.Empty<object>(), embeds = Array.Empty<object>(), mentions = Array.Empty<object>(), mention_roles = Array.Empty<object>()
            });
            return Message.Value;
        }
        private static void ValidateCard(JsonElement data)
        {
            if (!data.TryGetProperty("flags", out var flags) || (flags.GetInt32() & 32768) == 0) throw new Exception("Missing Components V2 flag");
            if (data.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.String && content.GetString()!.Length > 0)
                throw new Exception("Legacy content on V2 message");
            var all = WalkJson(data.GetProperty("components")).ToArray();
            if (all.Length > 40 || all.Where(c => c.GetProperty("type").GetInt32() == 10).Sum(c => c.GetProperty("content").GetString()!.Length) > 4000)
                throw new Exception("V2 message exceeds Discord limits");
            var ids = all.Where(c => c.TryGetProperty("custom_id", out _)).Select(c => c.GetProperty("custom_id").GetString()).ToArray();
            if (ids.Distinct().Count() != ids.Length) throw new Exception("Duplicate component ID");
        }
    }
}
