using System.Text.Json;
using PartyViewKR;

static partial class ListingDiscordConnectionTests
{
    private static void RunChatSendSdk(Action<string, Action> test, Action<bool, string> check)
    {
        foreach (var (name, expected) in new[] { ("파티", "/p 전달 <se.1>"), ("연합파티", "/a 전달 <se.1>"),
            ("귓속말", "/tell 모험가@펜리르 전달 <se.1>"), ("답장", "/r 전달 <se.1>") }) {
            test("Discord SDK chat send modal roundtrip ACK and one-shot dispatch " + name, () => {
                var transport = new OfflineTransport(); using var client = new ListingDiscordClient(new("test"), FakeToken, transport.Config());
                Wait(() => transport.Identified.Task.IsCompleted || !client.Running);
                transport.Ready().GetAwaiter().GetResult(); Wait(() => client.Connected || !client.Running); check(client.Connected, client.Status);
                var host = new GameChatSendingTests.Host(); client.ChatSending.Update(host, Environment.TickCount64);
                client.ChatSending.ObserveTell(10, GameChatSendingTests.Tell());
                var registered = transport.CommandBodies.Single(b => b.GetProperty("name").GetString() == "pvkr-send");
                check(registered.GetProperty("options").EnumerateArray().Select(o => o.GetProperty("name").GetString())
                    .SequenceEqual(["파티", "연합파티", "귓속말", "답장"]), "send channels not registered");
                transport.Interaction(2, new { id = "702", name = "pvkr-send", type = 1, options = new[] { new { name, type = 1 } } });
                Wait(() => transport.Responses.Count > 0);
                var modalResponse = transport.Responses.First().Body;
                check(modalResponse.GetProperty("type").GetInt32() == 9 && host.Calls.Count == 0, "opening send modal performed mutation");
                var modal = modalResponse.GetProperty("data");
                var labels = modal.GetProperty("components").EnumerateArray().ToArray();
                var fields = labels.Select(c => c.GetProperty("component")).ToArray();
                check(labels.All(c => c.GetProperty("type").GetInt32() == 18) && fields.Length == (name == "귓속말" ? 2 : 1) &&
                    fields.All(f => f.GetProperty("type").GetInt32() == 4 && f.GetProperty("style").GetInt32() == 1), "wrong mobile input form");
                if (name == "답장") check(labels[0].GetProperty("label").GetString()!.Contains("모험가@펜리르"), "reply target not shown");
                object[] values = name == "귓속말" ? [new { type = 4, custom_id = "target", value = "모험가@펜리르" },
                    new { type = 4, custom_id = "message", value = "전달 <se.1>" }] : [new { type = 4, custom_id = "message", value = "전달 <se.1>" }];
                var data = new { custom_id = modal.GetProperty("custom_id").GetString(), components = values.Select(v => new { type = 18, component = v }).ToArray() };
                transport.Interaction(5, data);
                Wait(() => transport.Responses.Count >= 2);
                var ack = transport.Responses.Skip(1).First().Body;
                check(ack.GetProperty("type").GetInt32() == 5 && host.Calls.Count == 0, "send was not deferred before game dispatch");
                check((ack.GetProperty("data").GetProperty("flags").GetInt32() & 64) != 0, "send receipt not private");
                Wait(() => { client.ChatSending.Update(host, Environment.TickCount64); return transport.Responses.Any(r => r.Method == "PATCH"); });
                var result = transport.Responses.Last().Body;
                check(host.Calls.Single().Command == expected && TextOf(result).Contains("요청"), "SDK send routing or result failed");
                transport.Interaction(5, data); Wait(() => transport.Responses.Count >= 4); client.ChatSending.Update(host, Environment.TickCount64 + 2000);
                check(host.Calls.Count == 1 && transport.ChatMessages.IsEmpty, "replayed modal or sent game text to Discord channel");
                client.Dispose(); client.Completion.GetAwaiter().GetResult();
            });
        }
    }
}
