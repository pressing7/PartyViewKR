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
            check(transport.Commands.IsEmpty && !client.Connected, "command registered before READY");
            transport.Ready().GetAwaiter().GetResult();
            Wait(() => client.Connected || !client.Running);
            check(client.Connected, client.Status);
            check(transport.Commands.SequenceEqual(["applications/500/commands"]), "missing or incorrect application ID");
            check(client.Status.StartsWith("연결됨") && client.InviteUrl.Contains("client_id=500"), "connected status missing");
            transport.Ready().GetAwaiter().GetResult();
            check(transport.Commands.Count == 1, "repeated READY registered the command again");
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
        RunInteractions(test, check);
    }

    private sealed class OfflineTransport : IRestClient, IWebSocketClient
    {
        internal readonly TaskCompletionSource Identified = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal readonly ConcurrentQueue<string> Commands = new();
        internal readonly ConcurrentQueue<(string Method, string Endpoint, JsonElement Body)> Responses = new();
        internal JsonElement? Message;
        private int sequence = 1;
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
        internal Task Ready() => Receive("""
            {"op":0,"s":1,"t":"READY","d":{"v":10,"user":{"id":"500","username":"PVKR","discriminator":"0","bot":true},
            "guilds":[],"private_channels":[],"session_id":"offline","resume_gateway_url":"wss://offline.invalid",
            "application":{"id":"500","flags":0}}}
            """);
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
            else if (method == "POST" && endpoint.EndsWith("/commands")) {
                Commands.Enqueue(endpoint);
                if (endpoint != "applications/500/commands" || RejectCommands) {
                    code = HttpStatusCode.Forbidden; body = JsonSerializer.Serialize(new { code = 50001, message = FakeToken });
                }
                else body = """{"id":"700","application_id":"500","name":"pvkr","type":1,"description":"PVKR","version":"1"}""";
            }
            else if (method == "POST" && endpoint.StartsWith("interactions/") && endpoint.EndsWith("/callback")) {
                using var parsed = JsonDocument.Parse(json!);
                var response = parsed.RootElement.Clone();
                if (response.GetProperty("type").GetInt32() is 4 or 7) MakeMessage(response.GetProperty("data"));
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
        private JsonElement MakeMessage(JsonElement data)
        {
            Message = JsonSerializer.SerializeToElement(new {
                id = "900", channel_id = "800", type = 0,
                author = new { id = "500", username = "PVKR", discriminator = "0", bot = true },
                timestamp = DateTimeOffset.UtcNow, content = data.TryGetProperty("content", out var text) ? text.GetString() : "",
                components = data.TryGetProperty("components", out var components) ? components : JsonSerializer.SerializeToElement(Array.Empty<object>()),
                attachments = Array.Empty<object>(), embeds = Array.Empty<object>(), mentions = Array.Empty<object>(), mention_roles = Array.Empty<object>()
            });
            return Message.Value;
        }
    }
}
