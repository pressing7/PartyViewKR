using System.Text;
using System.Text.Json;

namespace PartyViewKR;

// The relay can request only these managed operations. It never supplies a
// native address, node ID, callback, arbitrary HTTP URL or framework delegate.
internal static class ListingRelayProtocol
{
    internal const int MaxFrameBytes = 524288;
    internal static bool TryServer(string value, out Uri server, bool localTest = false)
    {
        server = null!;
        if (value.Length > 512 || !Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) ||
            uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0 || uri.AbsolutePath != "/") return false;
        var local = localTest && uri.Scheme == "http" && uri.IsLoopback;
        if (!local && (uri.Scheme != "https" || !uri.IsDefaultPort || uri.HostNameType != UriHostNameType.Dns || uri.IsLoopback)) return false;
        server = new Uri(uri.GetLeftPart(UriPartial.Authority)); return true;
    }
    internal static bool IsKey(string value) => value.Length == 64 && value.All(c => c is >= '0' and <= '9' or >= 'A' and <= 'F');
    internal static byte[] Handle(ListingWebBridge bridge, ReadOnlyMemory<byte> frame, long utcNow, long tickNow)
    {
        var id = "";
        WebReply reply;
        try
        {
            if (frame.Length > MaxFrameBytes) throw new JsonException();
            using var document = JsonDocument.Parse(frame, new JsonDocumentOptions { MaxDepth = 8 });
            var root = document.RootElement;
            var fields = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in root.EnumerateObject())
                if (!fields.Add(property.Name) || property.Name is not ("v" or "id" or "method" or "path" or "requestId" or "session" or "body" or "expiresAt")) throw new JsonException();
            if (fields.Count != 8 || root.GetProperty("v").GetInt32() != 1) throw new JsonException();
            id = root.GetProperty("id").GetString() ?? "";
            if (!Guid.TryParseExact(id, "D", out _)) throw new JsonException();
            var expires = root.GetProperty("expiresAt").GetInt64();
            if (expires < utcNow || expires - utcNow > 10000)
                reply = ListingWebBridge.Error(408, "요청 시간이 지났습니다. PC 시간을 확인하고 다시 조회해 주세요.");
            else
            {
                var method = root.GetProperty("method").GetString();
                var path = root.GetProperty("path").GetString() ?? "";
                var requestId = root.GetProperty("requestId").GetString() ?? "";
                var session = root.GetProperty("session").GetString() ?? "";
                var body = root.GetProperty("body").GetString() ?? "";
                if (method == "GET" && body.Length == 0 && path == "/api/state")
                {
                    using var state = JsonDocument.Parse(bridge.ReadState());
                    reply = new(200, state.RootElement.Clone());
                }
                else if (method == "GET" && body.Length == 0 && path.StartsWith("/api/operations/", StringComparison.Ordinal) && Guid.TryParseExact(path[16..], "D", out _))
                    reply = bridge.ReadOperation(path[16..]);
                else if (method == "GET" && body.Length == 0 && path.StartsWith("/api/queries/", StringComparison.Ordinal) && Guid.TryParseExact(path[13..], "D", out _))
                    reply = bridge.ReadQuery(path[13..]);
                else if (method == "POST" && path == "/api/query" && body.Length == 0)
                    reply = bridge.QueueQuery(requestId, session, tickNow);
                else if (method == "POST" && path == "/api/apply" && Encoding.UTF8.GetByteCount(body) is > 0 and <= 65536)
                {
                    var bytes = Encoding.UTF8.GetBytes(body);
                    try { reply = bridge.QueueApply(requestId, session, bytes, tickNow); }
                    finally { System.Security.Cryptography.CryptographicOperations.ZeroMemory(bytes); }
                }
                else reply = ListingWebBridge.Error(400, "지원하지 않는 중계 요청입니다.");
            }
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or KeyNotFoundException or FormatException or OverflowException)
        { reply = ListingWebBridge.Error(400, "중계 요청 형식이 올바르지 않습니다."); }
        return JsonSerializer.SerializeToUtf8Bytes(new { v = 1, id, status = reply.Status, body = reply.Body }, ListingWebBridge.JsonOptions);
    }
}
