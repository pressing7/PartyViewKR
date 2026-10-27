using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace PartyViewKR;

// Bind the runtime's public chat event: KR/API-15 distributions have both the
// legacy five-argument delegate and the newer IHandleableChatMessage delegate.
// No native offsets, packet hooks, or dependency on a particular Lumina version.
internal sealed class GameChatCapture : IDisposable
{
    private readonly object source;
    private readonly EventInfo chatEvent;
    private readonly Delegate callback;
    private readonly Func<GameChatKind, bool> wanted;
    private readonly Action<GameChatMessage> receive;
    private readonly Func<uint, string> worldName;
    private readonly Func<string> homeWorld;
    private readonly Action<string> error;
    private bool disposed;

    internal GameChatCapture(object source, Type contract, Func<GameChatKind, bool> wanted,
        Action<GameChatMessage> receive, Func<uint, string> worldName, Action<string> error, Func<string>? homeWorld = null)
    {
        this.source = source; this.wanted = wanted; this.receive = receive; this.worldName = worldName; this.error = error;
        this.homeWorld = homeWorld ?? (() => "");
        chatEvent = contract.GetEvent("ChatMessage") ?? throw new MissingMemberException("ChatMessage");
        var handler = chatEvent.EventHandlerType ?? throw new MissingMemberException("ChatMessage handler");
        var invoke = handler.GetMethod("Invoke")!;
        var parameters = invoke.GetParameters().Select(p => Expression.Parameter(p.ParameterType, p.Name)).ToArray();
        MethodCallExpression call;
        if (invoke.ReturnType != typeof(void)) throw new NotSupportedException("Chat event return type");
        if (parameters.Length == 5 && invoke.GetParameters()[2].ParameterType.IsByRef &&
            invoke.GetParameters()[3].ParameterType.IsByRef && invoke.GetParameters()[4].ParameterType == typeof(bool).MakeByRefType())
            call = Expression.Call(Expression.Constant(this), nameof(Legacy), null,
                Expression.Convert(parameters[0], typeof(ushort)), Expression.Convert(parameters[1], typeof(int)),
                Expression.Convert(parameters[2], typeof(object)), Expression.Convert(parameters[3], typeof(object)));
        else if (parameters.Length == 1 && !invoke.GetParameters()[0].ParameterType.IsByRef)
            call = Expression.Call(Expression.Constant(this), nameof(Current), null, Expression.Convert(parameters[0], typeof(object)));
        else throw new NotSupportedException("Chat event signature");
        callback = Expression.Lambda(handler, call, parameters).Compile();
        chatEvent.AddEventHandler(source, callback);
    }

    private static object? Property(object value, string name)
    {
        var type = value.GetType();
        var property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public) ??
            type.GetInterfaces().SelectMany(i => i.GetProperties()).FirstOrDefault(p => p.Name == name);
        return property?.GetValue(value);
    }
    private void Current(object message)
    {
        try {
            if (disposed || Property(message, "LogKind") is not { } type || GameChat.FromType(Convert.ToUInt16(type)) is not { } kind || !wanted(kind)) return;
            Emit(kind, Convert.ToInt32(Property(message, "Timestamp") ?? 0), Property(message, "Sender"), Property(message, "Message"));
        }
        catch (Exception e) { error(e.GetType().Name); }
    }
    private void Legacy(ushort type, int timestamp, object sender, object message)
    {
        try {
            if (!disposed && GameChat.FromType(type) is { } kind && wanted(kind)) Emit(kind, timestamp, sender, message);
        }
        catch (Exception e) { error(e.GetType().Name); }
    }
    private void Emit(GameChatKind kind, int timestamp, object? sender, object? message)
    {
        if (sender == null || message == null || Property(message, "TextValue") is not string text) return;
        var name = Property(sender, "TextValue") as string ?? ""; var world = "";
        var explicitWorld = false;
        var crossWorld = name.Contains('@') || name.Contains('＠');
        if (Property(sender, "Payloads") is IEnumerable payloads) {
            foreach (var payload in payloads) {
                if (payload?.GetType().Name == "IconPayload" && Property(payload, "Icon")?.ToString() == "CrossWorld") crossWorld = true;
                if (payload == null || payload.GetType().Name != "PlayerPayload") continue;
                name = Property(payload, "PlayerName") as string ?? name;
                var row = Property(payload, "World");
                if (row != null && Property(row, "RowId") is { } id && Convert.ToUInt32(id) is > 0 and var worldId) {
                    explicitWorld = true; world = worldName(worldId);
                }
            }
        }
        // Same-world/local senders can omit the world link. An explicit remote
        // world always wins, even if its name cannot currently be resolved.
        if (!explicitWorld && !crossWorld) world = homeWorld();
        // BoxedOutlinedNumber0..9 are game-font party number glyphs, not names.
        name = new string(name.Where(c => c is < '\uE0E0' or > '\uE0E9').ToArray()).Trim();
        text = IncludeSoundEffects(message, text);
        receive(new(kind, name, world, text, timestamp > 0 ? DateTimeOffset.FromUnixTimeSeconds(timestamp) : DateTimeOffset.UtcNow));
        error("");
    }

    private static string IncludeSoundEffects(object message, string text)
    {
        // The public TextValue normally evaluates fixed(200,5,n) to <se.n+1>.
        // Some API-15 builds leave the macro as a non-text payload. Read its
        // public encoding as well; never evaluate/play it or modify the message.
        if (Property(message, "Payloads") is not IEnumerable payloads) return text;
        foreach (var payload in payloads) {
            if (payload == null || payload.GetType().Name is not ("RawPayload" or "AutoTranslatePayload")) continue;
            var bytes = Property(payload, "Data") as byte[] ??
                payload.GetType().GetMethod("Encode", [typeof(bool)])?.Invoke(payload, [false]) as byte[];
            // Canonical Fixed macro with three small integer expressions.
            if (bytes is not { Length: 7 } || bytes[0] != 2 || bytes[1] != 0x2E || bytes[2] != 4 ||
                bytes[3] != 201 || bytes[4] != 6 || bytes[5] is < 1 or > 16 || bytes[6] != 3) continue;
            var tag = $"<se.{bytes[5]}>";
            if (!text.Contains(tag, StringComparison.OrdinalIgnoreCase)) text += (text.Length == 0 ? "" : " ") + tag;
        }
        return text;
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true; chatEvent.RemoveEventHandler(source, callback);
    }
}
