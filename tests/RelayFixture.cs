using System.Collections.Concurrent;
using System.Text.Json;
using PartyViewKR;

// Fake game + real outbound WebSocket for local workerd integration tests.
// Test credentials are read from an environment variable, not command lines.
static class RelayFixture
{
    internal static async Task Run(string[] args)
    {
        var address = args[Array.IndexOf(args, "--relay-fixture") + 1];
        var key = Environment.GetEnvironmentVariable("PVKR_TEST_KEY") ?? throw new Exception("Missing test key");
        var host = new WebEditFakeHost { CheckThread = false, Tick = Environment.TickCount64 };
        host.Native.Target = WebEditFakeHost.Target(18, true);
        var bridge = new ListingWebBridge("0.3.33.0");
        bridge.Update(host, Environment.TickCount64);
        using var relay = new ListingRelayClient(bridge, address, key, localTest: true);
        Console.WriteLine(relay.ConnectionUrl);
        var commands = new ConcurrentQueue<string>();
        _ = Task.Run(async () => { while (await Console.In.ReadLineAsync() is { } line) commands.Enqueue(line); commands.Enqueue("quit"); });
        while (true)
        {
            while (commands.TryDequeue(out var command)) {
                if (command == "quit") { relay.Dispose(); await relay.Completion; return; }
                if (command == "48") host.Native.Target = WebEditFakeHost.Target(48);
                if (command == "rule-disabled") host.Native.RuleEditAllowed = false;
                if (command == "logout") host.Native.SelfId = 0;
                if (command == "status") Console.WriteLine(JsonSerializer.Serialize(new { relay.Connected, relay.Status, host.StartCalls, host.Native.SubmitCalls }));
            }
            var now = Environment.TickCount64; host.Advance(now); bridge.Update(host, now);
            await Task.Delay(50);
        }
    }
}
