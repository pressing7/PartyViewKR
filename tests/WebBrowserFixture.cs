using System.Collections.Concurrent;
using PartyViewKR;

// Local browser smoke-test fixture. Not compiled into the plugin. Commands on
// stdin only change this fake host; the production listener exposes no test API.
static class WebBrowserFixture
{
    internal static async Task Run()
    {
        var host = new WebEditFakeHost { CheckThread = false, Tick = Environment.TickCount64 };
        host.Native.Target = WebEditFakeHost.Target(4);
        var bridge = new ListingWebBridge("0.3.32.0");
        bridge.Update(host, Environment.TickCount64);
        using var server = new ListingWebServer(bridge);
        Console.WriteLine(server.ConnectionUrl);
        var commands = new ConcurrentQueue<string>();
        _ = Task.Run(async () => { while (await Console.In.ReadLineAsync() is { } line) commands.Enqueue(line); commands.Enqueue("quit"); });
        var paused = false;
        while (true)
        {
            while (commands.TryDequeue(out var command))
            {
                switch (command)
                {
                    case "quit": server.Dispose(); await server.Completion; return;
                    case "logout": host.Native.SelfId = 0; break;
                    case "login": host.Native.SelfId = 10; host.Native.Target = WebEditFakeHost.Target(4); host.Live = true; break;
                    case "friendly": host.Native.Target = WebEditFakeHost.Target(18, true); break;
                    case "48": host.Native.Target = WebEditFakeHost.Target(48); break;
                    case "busy": host.Blocked = "게임의 파티 모집 수정창을 먼저 닫아 주세요."; break;
                    case "ready": host.Blocked = null; break;
                    case "pause": paused = true; break;
                    case "resume": paused = false; break;
                    case "closed": host.Live = false; break;
                    case "unsafe-text": host.Native.Target = host.Native.Target with { Description = "<img src=x onerror=alert(1)> & <script>window.injected=true</script>" }; break;
                    case "rule-disabled": host.Native.RuleEditAllowed = false; break;
                }
            }
            var now = Environment.TickCount64;
            if (!paused)
            {
                host.Advance(now);
                bridge.Update(host, now);
            }
            await Task.Delay(100);
        }
    }
}
