using System.Buffers.Binary;

namespace PartyViewKR;

// Compare the registered listing to the editor without rewriting either one.
// KR 0.3.27 capture: the six unused friendly-team positions are zero in the
// listing but carry 0xFDFFFFFE in the editor. B5 was separately unregistered
// and remained zero in BOTH arrays; it must not receive the same exception.
internal static class ListingEditorCriteria
{
    private const ulong FriendlyPaddingFlags = 0x00000000FDFFFFFEul;

    internal static bool IsFriendlyPadding(ListingSlots slots, int index) =>
        slots.IsFriendlyMatch && (index is 5 or 6 or 7 or 13 or 14 or 15) &&
        slots.UnregisteredNativeIndices.Contains(index);

    internal static bool Matches(RefreshTarget target, string actualCriteria, int draftGroups,
        int draftMainSlots, out int[] acceptedPaddingIndices)
        => Matches(target, actualCriteria, draftGroups, draftMainSlots, out acceptedPaddingIndices, out _);

    internal static bool Matches(RefreshTarget target, string actualCriteria, int draftGroups,
        int draftMainSlots, out int[] acceptedPaddingIndices, out int[] acceptedOccupiedIndices)
    {
        acceptedPaddingIndices = [];
        acceptedOccupiedIndices = [];
        if (target.Criteria == actualCriteria) return true;
        if (target.TotalSlots != target.Slots.TotalSlots ||
            !ListingSlotRoute.TryCreate(target.Slots, 1, out var route) ||
            !route.MatchesEditorShape(draftGroups, draftMainSlots)) return false;
        // Every non-role condition still compares exactly.
        if (!ListingRoleNormalization.TryReadPair(target.Criteria, actualCriteria, target.Slots.NativeSlotCount,
            out var expected, out var actual)) return false;
        var accepted = new List<int>();
        var occupied = new List<int>();
        for (var i = 0; i < expected.Length; i++)
        {
            if (expected[i] == actual[i]) continue;
            if (ListingRoleNormalization.IsOccupied(target.Slots, i, expected[i], actual[i])) {
                occupied.Add(i); continue;
            }
            if (!IsFriendlyPadding(target.Slots, i) || expected[i] != 0 || actual[i] != FriendlyPaddingFlags)
                return false;
            accepted.Add(i);
        }
        acceptedPaddingIndices = accepted.ToArray();
        acceptedOccupiedIndices = occupied.ToArray();
        return true;
    }

    internal static bool TryReadFlags(string criteria, int count, out ulong[] flags)
    {
        flags = [];
        var fields = criteria.Split('/');
        if (count is < 1 or > 48 || fields.Length != 9 || fields[^1].Length != count * sizeof(ulong) * 2) return false;
        byte[] bytes;
        try { bytes = Convert.FromHexString(fields[^1]); }
        catch (FormatException) { return false; }
        flags = new ulong[count];
        for (var i = 0; i < count; i++) flags[i] = BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(i * sizeof(ulong)));
        return true;
    }
}
