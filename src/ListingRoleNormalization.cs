namespace PartyViewKR;

// Opening an existing recruitment can narrow an occupied slot to its entry job.
// Use the listing's registered job and member identity, never the live party job.
internal static class ListingRoleNormalization
{
    internal static bool IsOccupied(ListingSlots slots, int nativeIndex, ulong before, ulong after)
    {
        if (!slots.JobRules.Complete) return false;
        var slot = slots.Slots.FirstOrDefault(s => s.NativeIndex == nativeIndex);
        if (slot == null || slot.MemberContentId == 0 || slot.JobId == 0 || slot.SlotFlags != before) return false;
        var flag = slots.JobRules.Choices.FirstOrDefault(j => j.Id == slot.JobId)?.Flag ?? 0;
        return flag != 0 && after == flag && (before & flag) != 0;
    }

    internal static bool SameCriteria(RefreshTarget before, RefreshTarget after)
    {
        if (before.Criteria == after.Criteria) return true;
        if (before.TotalSlots != before.Slots.TotalSlots || after.TotalSlots != after.Slots.TotalSlots ||
            !ListingJobEdit.SameSlots(before.Slots, after.Slots) ||
            !TryReadPair(before.Criteria, after.Criteria, before.Slots.NativeSlotCount, out var expected, out var actual)) return false;
        for (var i = 0; i < expected.Length; i++) {
            if (expected[i] == actual[i]) continue;
            if (!IsOccupied(before.Slots, i, expected[i], actual[i]) ||
                after.Slots.Slots.FirstOrDefault(s => s.NativeIndex == i)?.SlotFlags != actual[i]) return false;
        }
        return true;
    }

    internal static bool TryReadPair(string before, string after, int count, out ulong[] expected, out ulong[] actual)
    {
        expected = []; actual = [];
        var a = before.Split('/'); var b = after.Split('/');
        return a.Length == 9 && b.Length == 9 && a.Take(8).SequenceEqual(b.Take(8)) &&
            ListingEditorCriteria.TryReadFlags(before, count, out expected) &&
            ListingEditorCriteria.TryReadFlags(after, count, out actual);
    }
}
