using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;

namespace PartyViewKR;

// Outbound WSS only. Background tasks touch managed snapshots/queues, never
// Dalamud services or native objects. A reconnect retains the same bridge and
// accepted operation IDs; it cannot replay a mutation.
internal sealed class ListingRelayClient : IDisposable
{
    private readonly CancellationTokenSource stop = new();
    private readonly Task worker;
    private readonly Uri server;
    private string connectKey;
    private readonly string room = Guid.NewGuid().ToString("N");
    private string browserKey = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    private string status = "중계 서버에 연결하는 중입니다.";
    private int disposed, connected;
    internal ListingWebBridge Bridge { get; }
    internal bool Running => Volatile.Read(ref disposed) == 0 && !worker.IsCompleted;
    internal bool Connected => Running && Volatile.Read(ref connected) != 0;
    internal string Status => Volatile.Read(ref status);
    internal string ConnectionCode => room + "." + browserKey;
    internal string ConnectionUrl => server.GetLeftPart(UriPartial.Authority) + "/#room=" + room + "&key=" + browserKey;
    internal Task Completion => worker;

    internal ListingRelayClient(ListingWebBridge bridge, string address, string key, bool localTest = false)
    {
        if (!ListingRelayProtocol.TryServer(address, out server, localTest) || !ListingRelayProtocol.IsKey(key))
            throw new ArgumentException("Invalid relay settings");
        Bridge = bridge; connectKey = key;
        worker = Task.Run(Run);
    }
    private async Task Run()
    {
        var retry = 0;
        try
        {
            while (!stop.IsCancellationRequested)
            {
                using var socket = new ClientWebSocket();
                socket.Options.SetRequestHeader("Authorization", "Bearer " + connectKey);
                socket.Options.SetRequestHeader("X-PVKR-Room", room);
                socket.Options.SetRequestHeader("X-PVKR-Browser-Hash", Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(browserKey))));
                socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(20);
                socket.Options.KeepAliveTimeout = TimeSpan.FromSeconds(10);
                socket.Options.CollectHttpResponseDetails = true;
                var url = new UriBuilder(server) { Scheme = server.Scheme == "https" ? "wss" : "ws", Path = "/relay/host" }.Uri;
                try
                {
                    Volatile.Write(ref status, "중계 서버에 연결하는 중입니다.");
                    using (var opening = CancellationTokenSource.CreateLinkedTokenSource(stop.Token))
                    {
                        opening.CancelAfter(TimeSpan.FromSeconds(10));
                        await socket.ConnectAsync(url, opening.Token).ConfigureAwait(false);
                    }
                    retry = 0; Volatile.Write(ref connected, 1);
                    Volatile.Write(ref status, "외부 연결됨 · 휴대폰에서 접속 링크 또는 연결 코드를 사용하세요.");
                    if (!await Receive(socket).ConfigureAwait(false))
                    {
                        Volatile.Write(ref status, "다른 PC가 이 서버에 연결하여 외부 연결이 종료됐습니다.");
                        return;
                    }
                }
                catch (Exception) when (!stop.IsCancellationRequested)
                {
                    // Never log exception messages: an HTTP error can contain
                    // credentials supplied by a proxy or server.
                    Volatile.Write(ref status, socket.HttpStatusCode == System.Net.HttpStatusCode.Unauthorized
                        ? "서버 연결 키가 맞지 않습니다. 연결을 끄고 키를 확인해 주세요."
                        : "중계 서버에 연결하지 못했습니다. 주소·서버 연결 키를 확인해 주세요. 자동 재연결을 기다립니다.");
                }
                finally { Volatile.Write(ref connected, 0); socket.Abort(); }
                if (!stop.IsCancellationRequested)
                {
                    if (Status.StartsWith("외부 연결됨", StringComparison.Ordinal)) Volatile.Write(ref status, "연결이 끊겼습니다. 자동으로 다시 연결합니다.");
                    await Task.Delay(TimeSpan.FromSeconds(Math.Min(30, 1 << Math.Min(++retry, 5))), stop.Token).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception) { Volatile.Write(ref status, "외부 연결이 종료됐습니다. 연결을 다시 켜 주세요."); }
        finally { Volatile.Write(ref connected, 0); Bridge.Stop(); connectKey = ""; browserKey = ""; }
    }
    private async Task<bool> Receive(ClientWebSocket socket)
    {
        var buffer = new byte[ListingRelayProtocol.MaxFrameBytes];
        try
        {
            while (!stop.IsCancellationRequested && socket.State == WebSocketState.Open)
            {
                var count = 0;
                ValueWebSocketReceiveResult message;
                do
                {
                    if (count == buffer.Length) throw new InvalidDataException();
                    message = await socket.ReceiveAsync(buffer.AsMemory(count), stop.Token).ConfigureAwait(false);
                    if (message.MessageType == WebSocketMessageType.Close) return (int?)socket.CloseStatus != 4001;
                    if (message.MessageType != WebSocketMessageType.Text) throw new InvalidDataException();
                    count += message.Count;
                } while (!message.EndOfMessage);
                byte[] reply;
                try
                {
                    reply = ListingRelayProtocol.Handle(Bridge, buffer.AsMemory(0, count),
                        DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), Environment.TickCount64);
                }
                finally { CryptographicOperations.ZeroMemory(buffer.AsSpan(0, count)); }
                if (reply.Length > ListingRelayProtocol.MaxFrameBytes) throw new InvalidDataException();
                using var sending = CancellationTokenSource.CreateLinkedTokenSource(stop.Token);
                sending.CancelAfter(TimeSpan.FromSeconds(3));
                await socket.SendAsync(reply.AsMemory(), WebSocketMessageType.Text, true, sending.Token).ConfigureAwait(false);
            }
            return true;
        }
        finally { CryptographicOperations.ZeroMemory(buffer); }
    }
    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;
        stop.Cancel(); Bridge.Stop();
        // Do not synchronously wait for networking on the framework/UI thread.
    }
}
