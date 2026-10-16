using System.Text;
using System.Text.Json;
using PartyViewKR;

static class ListingRelayTests
{
    private static byte[] Frame(string path, string method = "GET", string body = "", string requestId = "", string session = "", long expiresAt = 6000) =>
        JsonSerializer.SerializeToUtf8Bytes(new { v = 1, id = Guid.NewGuid().ToString(), method, path, requestId, session, body, expiresAt });
    private static JsonElement Reply(ListingWebBridge bridge, byte[] frame, long tick = 1100, long utc = 1000)
    {
        using var doc = JsonDocument.Parse(ListingRelayProtocol.Handle(bridge, frame, utc, tick)); return doc.RootElement.Clone();
    }
    internal static void Run(Action<string, Action> test, Action<bool, string> check)
    {
        test("relay accepts only an HTTPS origin in production", () => {
            check(ListingRelayProtocol.TryServer("https://pvkr.example.workers.dev/", out var server) && server.Host == "pvkr.example.workers.dev", "valid origin");
            foreach (var url in new[] { "http://example.com", "https://example.com/path", "https://user:pass@example.com", "https://example.com?key=x", "https://example.com/#x", "https://example.com:8888", "https://127.0.0.1", "https://localhost", "file:///tmp/x" })
                check(!ListingRelayProtocol.TryServer(url, out _), "unsafe origin: " + url);
            check(ListingRelayProtocol.TryServer("http://127.0.0.1:4000", out _, true), "local fixture denied");
        });
        test("relay read protocol exposes only managed state and whitelisted paths", () => {
            var (b, h, _) = ListingWebEditTests.Ready();
            check(Reply(b, Frame("/api/state")).GetProperty("body").GetProperty("apiVersion").GetInt32() == 2 && h.StartCalls == 0, "state read performed native action");
            foreach (var path in new[] { "/api/state?x=1", "/callback", "/api/apply/extra", "https://other/api/state", "/api/operations/../apply" })
                check(Reply(b, Frame(path)).GetProperty("status").GetInt32() == 400, "arbitrary path accepted");
        });
        test("relay expires delayed commands before queueing and rejects clock skew", () => {
            var (b, h, v) = ListingWebEditTests.Ready();
            foreach (var expires in new long[] { 999, 11001 })
                check(Reply(b, Frame("/api/apply", "POST", Encoding.UTF8.GetString(ListingWebEditTests.Body(v.Editor!.Revision)), Guid.NewGuid().ToString(), v.Session, expires)).GetProperty("status").GetInt32() == 408, "delayed write accepted");
            b.Update(h, 1500); check(h.StartCalls == 0, "expired command ran");
        });
        test("relay duplicate delivery reuses the same operation and never calls game off-thread", () => {
            var (b, h, v) = ListingWebEditTests.Ready(); var id = Guid.NewGuid().ToString();
            var body = Encoding.UTF8.GetString(ListingWebEditTests.Body(v.Editor!.Revision, new { field = "description", value = "외부 수정" }));
            var frame = Frame("/api/apply", "POST", body, id, v.Session);
            var replies = Task.Run(() => new[] { Reply(b, frame), Reply(b, frame) }).GetAwaiter().GetResult();
            check(replies[0].GetProperty("status").GetInt32() == 202 && replies[1].GetProperty("status").GetInt32() == 200 && h.StartCalls == 0, "network thread executed or duplicate queued");
            for (var tick = 1500; tick < 40000; tick += 500) { h.Advance(tick); b.Update(h, tick); }
            check(h.StartCalls == 1 && h.Native.SubmitCalls == 1 && h.Native.SubmittedDescription == "외부 수정", "not a single refresh");
            check(Reply(b, Frame("/api/operations/" + id)).GetProperty("body").GetProperty("state").GetString() == "complete", "result not retained for reconnect");
        });
        test("relay rejects malformed envelopes, oversized bodies and unrecognized edits without echoing PIN", () => {
            var (b, h, v) = ListingWebEditTests.Ready();
            foreach (var raw in new[] { "{}", "[]", "null", "{\"v\":1,\"v\":1}", "{", "\"password9876\"" })
                check(Reply(b, Encoding.UTF8.GetBytes(raw)).GetProperty("status").GetInt32() == 400, "malformed envelope accepted");
            var bad = Reply(b, Frame("/api/apply", "POST", new string('x', 65537), Guid.NewGuid().ToString(), v.Session));
            check(bad.GetProperty("status").GetInt32() == 400, "body limit missing");
            var response = Reply(b, Frame("/api/apply", "POST", Encoding.UTF8.GetString(ListingWebEditTests.Body(v.Editor!.Revision,
                new { field = "private", value = true, password = "9876" })), Guid.NewGuid().ToString(), v.Session));
            check(response.GetProperty("status").GetInt32() == 202 && !response.GetRawText().Contains("9876"), "PIN reflected");
        });
        test("relay old sessions cannot mutate another character or a stopped bridge", () => {
            var (b, h, v) = ListingWebEditTests.Ready();
            var frame = Frame("/api/apply", "POST", Encoding.UTF8.GetString(ListingWebEditTests.Body(v.Editor!.Revision)), Guid.NewGuid().ToString(), v.Session);
            h.Native.SelfId = 20; b.Update(h, 1200);
            check(Reply(b, frame, 1300).GetProperty("status").GetInt32() == 409, "previous character write admitted");
            b.Stop(); check(Reply(b, frame).GetProperty("status").GetInt32() == 503, "stopped bridge accepted write");
        });
    }
}
