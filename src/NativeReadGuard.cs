using System.Runtime.InteropServices;
using Dalamud.Game.Text.SeStringHandling;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace PartyViewKR;

// Validate native pointers before following UI string fields. A matching
// assembly identity alone does not prove every game-version offset is valid.
internal static unsafe class NativeReadGuard
{
    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryInfo
    {
        public nuint BaseAddress, AllocationBase;
        public uint AllocationProtect;
        public nuint RegionSize;
        public uint State, Protect, Type;
    }

    [DllImport("kernel32.dll", ExactSpelling = true)]
    private static extern nuint VirtualQuery(nuint address, out MemoryInfo info, nuint size);

    internal static bool Readable(void* pointer, nuint length)
    {
        var cursor = (nuint)pointer;
        if (cursor < 0x10000 || length == 0 || length > 65536 || cursor > nuint.MaxValue - length) return false;
        var end = cursor + length;
        for (var i = 0; i < 16 && cursor < end; i++)
        {
            if (VirtualQuery(cursor, out var region, (nuint)sizeof(MemoryInfo)) == 0 || region.State != 0x1000 ||
                (region.Protect & 0x100) != 0 || (region.Protect & 0xff) is not (0x02 or 0x04 or 0x08 or 0x20 or 0x40 or 0x80))
                return false;
            if (region.RegionSize == 0 || region.BaseAddress > nuint.MaxValue - region.RegionSize) return false;
            var next = region.BaseAddress + region.RegionSize;
            if (next <= cursor) return false;
            cursor = next;
        }
        return cursor >= end;
    }

    internal static bool TextNode(AtkTextNode* node, out string text)
    {
        text = "";
        return Readable(node, (nuint)sizeof(AtkTextNode)) && node->Type == NodeType.Text && Text(&node->NodeText, out text);
    }

    internal static bool Text(Utf8String* value, out string text)
    {
        text = "";
        if (!Readable(value, (nuint)sizeof(Utf8String)) || value->BufUsed is < 0 or > 16384 || value->BufSize < value->BufUsed)
            return false;
        if (value->BufUsed == 0) return true;
        var bytes = value->StringPtr.Value;
        if (!Readable(bytes, (nuint)value->BufUsed)) return false;
        return FixedText(new ReadOnlySpan<byte>(bytes, (int)value->BufUsed), out text);
    }

    internal static bool FixedText(ReadOnlySpan<byte> bytes, out string text)
    {
        text = "";
        var end = bytes.IndexOf((byte)0);
        if (end >= 0) bytes = bytes[..end];
        try
        {
            text = SeString.Parse(bytes).TextValue.Trim();
            return !text.Any(c => c == '\ufffd' || char.IsControl(c) && c is not ('\n' or '\r' or '\t'));
        }
        catch { return false; }
    }
}
