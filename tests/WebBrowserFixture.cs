using System.Collections.Concurrent;
using PartyViewKR;

// Local browser smoke-test fixture. Not compiled into the plugin. Commands on
// stdin only change this fake host; the production listener exposes no test API.
static class WebBrowserFixture
{
    internal static async Task Run()
    {
        var host = new WebFakeHost { CheckThread = false, Listing = ListingWebTests.Sample(4) };
        var bridge = new ListingWebBridge("0.3.31.0");
        bridge.Update(host, Environment.TickCount64);
        using var server = new ListingWebServer(bridge);
        Console.WriteLine(server.ConnectionUrl);
        var commands = new ConcurrentQueue<string>();
        _ = Task.Run(async () => { while (await Console.In.ReadLineAsync() is { } line) commands.Enqueue(line); commands.Enqueue("quit"); });
        var finishAt = long.MaxValue; var paused = false;
        while (true)
        {
            while (commands.TryDequeue(out var command))
            {
                switch (command)
                {
                    case "quit": server.Dispose(); await server.Completion; return;
                    case "logout": host.CurrentSelf = 0; break;
                    case "login": host.CurrentSelf = 10; host.Listing = ListingWebTests.Sample(4); break;
                    case "friendly": host.Listing = ListingWebTests.Sample(18, true); break;
                    case "48": host.Listing = ListingWebTests.Sample(48); break;
                    case "busy": host.Blocked = "게임의 파티 모집 수정창을 먼저 닫아 주세요."; break;
                    case "ready": host.Blocked = null; break;
                    case "pause": paused = true; break;
                    case "resume": paused = false; break;
                    case "closed": host.Listing = host.Listing with { IsLive = false }; break;
                    case "unsafe-text": host.Listing = host.Listing with { Description = "<img src=x onerror=alert(1)> & <script>window.injected=true</script>" }; break;
                }
            }
            var now = Environment.TickCount64;
            if (!paused)
            {
                if (host.QueryBusy && finishAt == long.MaxValue) finishAt = now + 500;
                if (now >= finishAt) { host.Finish(); host.Listing = host.Listing with { IsLive = true, ReadAt = DateTime.Now }; finishAt = long.MaxValue; }
                bridge.Update(host, now);
            }
            await Task.Delay(100);
        }
    }
}
