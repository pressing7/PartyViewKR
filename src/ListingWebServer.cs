using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PartyViewKR;

// A deliberately small loopback-only HTTP/1.1 endpoint, with one request per
// connection and one bounded JSON mutation route; no uploads or proxy support.
// TcpListener avoids Windows HTTP.sys URL reservations and extra runtime DLLs.
// Do not expose this listener on a LAN/public interface without a separate design.
internal sealed class ListingWebServer : IDisposable
{
    private readonly TcpListener listener = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource stop = new();
    private readonly SemaphoreSlim capacity = new(8);
    private readonly object gate = new();
    private readonly Dictionary<int, TcpClient> clients = [];
    private readonly Dictionary<int, Task> handlers = [];
    private readonly Dictionary<string, (string Type, byte[] Body)> assets;
    private readonly byte[] authorization;
    private readonly Task worker;
    private readonly string authority;
    private int nextClient, disposed;
    private string failure = "";
    internal ListingWebBridge Bridge { get; }
    internal string BaseUrl { get; }
    internal string ConnectionUrl { get; }
    internal bool Running => !stop.IsCancellationRequested && !worker.IsCompleted;
    internal string Failure => Volatile.Read(ref failure);
    internal Task Completion => worker;

    internal ListingWebServer(ListingWebBridge bridge)
    {
        Bridge = bridge;
        assets = new() {
            ["/"] = ("text/html; charset=utf-8", Asset("index.html")),
            ["/app.js"] = ("text/javascript; charset=utf-8", Asset("app.js")),
            ["/editor.js"] = ("text/javascript; charset=utf-8", Asset("editor.js")),
            ["/style.css"] = ("text/css; charset=utf-8", Asset("style.css"))
        };
        var key = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        authorization = Encoding.ASCII.GetBytes("Bearer " + key);
        listener.ExclusiveAddressUse = true;
        listener.Start(8);
        authority = "127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port.ToString(CultureInfo.InvariantCulture);
        BaseUrl = "http://" + authority;
        // Fragment is handled in the browser, never sent in the HTTP URL/logs.
        ConnectionUrl = BaseUrl + "/#key=" + key;
        worker = Task.Run(AcceptLoop);
    }

    private static byte[] Asset(string name)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("PartyViewKR.Web." + name)
            ?? throw new InvalidDataException("Missing browser asset: " + name);
        using var bytes = new MemoryStream(); stream.CopyTo(bytes); return bytes.ToArray();
    }

    private async Task AcceptLoop()
    {
        try
        {
            while (!stop.IsCancellationRequested)
            {
                await capacity.WaitAsync(stop.Token).ConfigureAwait(false);
                TcpClient client;
                try { client = await listener.AcceptTcpClientAsync(stop.Token).ConfigureAwait(false); }
                catch { capacity.Release(); throw; }
                var id = Interlocked.Increment(ref nextClient);
                lock (gate)
                {
                    clients[id] = client;
                    handlers[id] = Task.Run(async () => {
                        try { await Handle(client).ConfigureAwait(false); }
                        catch (Exception) { /* Broken/expired clients never affect the game loop. */ }
                        finally
                        {
                            client.Dispose();
                            lock (gate) { clients.Remove(id); handlers.Remove(id); }
                            capacity.Release();
                        }
                    });
                }
            }
        }
        catch (Exception ex)
        {
            if (!stop.IsCancellationRequested) Volatile.Write(ref failure, ex.GetType().Name);
        }
        finally
        {
            stop.Cancel(); listener.Stop(); Bridge.Stop();
            Task[] pending;
            lock (gate)
            {
                foreach (var client in clients.Values) client.Dispose();
                pending = handlers.Values.ToArray();
            }
            await Task.WhenAll(pending).ConfigureAwait(false);
        }
    }

    private async Task Handle(TcpClient client)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stop.Token);
        timeout.CancelAfter(TimeSpan.FromSeconds(3));
        var token = timeout.Token;
        var stream = client.GetStream();
        var buffer = new byte[8192]; var count = 0; var end = -1;
        while (count < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(count), token).ConfigureAwait(false);
            if (read == 0) return;
            count += read;
            end = buffer.AsSpan(0, count).IndexOf("\r\n\r\n"u8);
            if (end >= 0) break;
        }
        if (end < 0 || !TryParse(buffer.AsSpan(0, end), out var request) || count - end - 4 > request.BodyLength)
        { await Reply(stream, ListingWebBridge.Error(400, "지원하지 않는 요청 형식입니다."), token); return; }
        if (!request.Headers.TryGetValue("Host", out var host) || !string.Equals(host, authority, StringComparison.OrdinalIgnoreCase) ||
            request.Headers.TryGetValue("Origin", out var origin) && origin != BaseUrl ||
            request.Headers.TryGetValue("Sec-Fetch-Site", out var site) && site is not ("same-origin" or "none"))
        { await Reply(stream, ListingWebBridge.Error(403, "허용되지 않은 접속 주소입니다."), token); return; }
        if (request.Path.StartsWith("/api/", StringComparison.Ordinal))
        {
            if (!request.Headers.TryGetValue("Authorization", out var header) ||
                !CryptographicOperations.FixedTimeEquals(authorization, Encoding.ASCII.GetBytes(header)))
            { await Reply(stream, ListingWebBridge.Error(401, "PVKR에서 접속 링크를 다시 복사해 주세요."), token); return; }
            if (request.Method == "POST" && request.Path == "/api/apply")
            {
                // Authenticate before allocating/reading a body. Content-Length
                // is mandatory, bounded and unambiguous; chunking is rejected.
                var body = new byte[request.BodyLength];
                try
                {
                    var copied = count - end - 4;
                    buffer.AsMemory(end + 4, copied).CopyTo(body);
                    await stream.ReadExactlyAsync(body.AsMemory(copied), token).ConfigureAwait(false);
                    var reply = Bridge.QueueApply(request.Headers.GetValueOrDefault("X-PVKR-Request-Id", ""),
                        request.Headers.GetValueOrDefault("X-PVKR-Session", ""), body, Environment.TickCount64);
                    await Reply(stream, reply, token); return;
                }
                finally { CryptographicOperations.ZeroMemory(body); CryptographicOperations.ZeroMemory(buffer); }
            }
            if (request.Method == "GET" && request.Path == "/api/state")
            { await Send(stream, 200, "application/json; charset=utf-8", Bridge.ReadState(), token); return; }
            if (request.Method == "GET" && request.Path.StartsWith("/api/queries/", StringComparison.Ordinal))
            { await Reply(stream, Bridge.ReadQuery(request.Path[13..]), token); return; }
            if (request.Method == "GET" && request.Path.StartsWith("/api/operations/", StringComparison.Ordinal))
            { await Reply(stream, Bridge.ReadOperation(request.Path[16..]), token); return; }
            if (request.Method == "POST" && request.Path == "/api/query")
            {
                var reply = Bridge.QueueQuery(request.Headers.GetValueOrDefault("X-PVKR-Request-Id", ""),
                    request.Headers.GetValueOrDefault("X-PVKR-Session", ""), Environment.TickCount64);
                await Reply(stream, reply, token); return;
            }
        }
        if (request.Method == "GET" && assets.TryGetValue(request.Path, out var asset))
        { await Send(stream, 200, asset.Type, asset.Body, token); return; }
        await Reply(stream, ListingWebBridge.Error(404, "지원하지 않는 주소입니다."), token);
    }

    internal sealed record Request(string Method, string Path, Dictionary<string, string> Headers, int BodyLength);
    internal static bool TryParse(ReadOnlySpan<byte> bytes, out Request request)
    {
        request = null!;
        // HTTP header/request lines are ASCII here; reject control characters,
        // folded/duplicate headers and ambiguous body framing instead of guessing.
        if (bytes.Length is 0 or > 8188) return false;
        foreach (var b in bytes) if (b > 126 || b < 32 && b is not (13 or 10 or 9)) return false;
        var lines = Encoding.ASCII.GetString(bytes).Split("\r\n", StringSplitOptions.None);
        var first = lines[0].Split(' ');
        if (first.Length != 3 || first[0] is not ("GET" or "POST") || !first[1].StartsWith('/') ||
            first[1].Any(c => c <= 32 || c == '\\') || first[2] is not ("HTTP/1.1" or "HTTP/1.0")) return false;
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in lines.Skip(1))
        {
            var colon = line.IndexOf(':');
            if (colon <= 0 || line.Any(c => c is '\r' or '\n') || line[..colon].Any(c =>
                !char.IsAsciiLetterOrDigit(c) && c != '-') || !headers.TryAdd(line[..colon], line[(colon + 1)..].Trim(' ', '\t'))) return false;
        }
        if (headers.ContainsKey("Transfer-Encoding") || headers.ContainsKey("Expect") || headers.ContainsKey("Content-Encoding")) return false;
        var bodyLength = 0;
        if (first[0] == "POST" && first[1] == "/api/apply")
        {
            if (!headers.TryGetValue("Content-Length", out var length) || length.Length == 0 || length.Any(c => !char.IsAsciiDigit(c)) ||
                !int.TryParse(length, NumberStyles.None, CultureInfo.InvariantCulture, out bodyLength) || bodyLength is < 1 or > ListingWebEdit.MaxBodyBytes ||
                !headers.TryGetValue("Content-Type", out var contentType) ||
                !string.Equals(contentType, "application/json", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(contentType, "application/json; charset=utf-8", StringComparison.OrdinalIgnoreCase)) return false;
        }
        else if (headers.TryGetValue("Content-Length", out var length) && length != "0") return false;
        request = new(first[0], first[1], headers, bodyLength); return true;
    }

    private static Task Reply(NetworkStream stream, WebReply reply, CancellationToken token) =>
        Send(stream, reply.Status, "application/json; charset=utf-8", JsonSerializer.SerializeToUtf8Bytes(reply.Body, ListingWebBridge.JsonOptions), token);

    private static async Task Send(NetworkStream stream, int status, string type, byte[] body, CancellationToken token)
    {
        var label = status switch { 200 => "OK", 202 => "Accepted", 400 => "Bad Request", 401 => "Unauthorized",
            403 => "Forbidden", 404 => "Not Found", 409 => "Conflict", 429 => "Too Many Requests", _ => "Service Unavailable" };
        var headers = Encoding.ASCII.GetBytes($"HTTP/1.1 {status} {label}\r\nContent-Type: {type}\r\n" +
            "Content-Length: " + body.Length.ToString(CultureInfo.InvariantCulture) + "\r\nConnection: close\r\n" +
            "Cache-Control: no-store\r\nReferrer-Policy: no-referrer\r\nX-Content-Type-Options: nosniff\r\nX-Frame-Options: DENY\r\n" +
            "Content-Security-Policy: default-src 'none'; script-src 'self'; style-src 'self'; connect-src 'self'; base-uri 'none'; frame-ancestors 'none'; form-action 'none'\r\n\r\n");
        await stream.WriteAsync(headers, token).ConfigureAwait(false);
        await stream.WriteAsync(body, token).ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;
        Bridge.Stop(); stop.Cancel(); listener.Stop();
        lock (gate) foreach (var client in clients.Values) client.Dispose();
        // Never synchronously wait on a network task from the game thread.
    }
}
