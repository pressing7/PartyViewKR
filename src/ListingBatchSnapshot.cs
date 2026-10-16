using System.Runtime.InteropServices;

namespace PartyViewKR;

internal static class ListingBatchSnapshot
{
    internal static bool TryRead(RefreshTarget identity, string criteria, string description, string extra,
        out RefreshTarget current, out string error)
    {
        current = identity;
        error = "수정창의 모집 자리 정보를 확인하지 못했습니다.";
        if (!ListingEditorCriteria.TryReadFlags(criteria, identity.Slots.NativeSlotCount, out var flags)) return false;
        foreach (var index in identity.Slots.UnregisteredNativeIndices)
        {
            if (flags[index] == 0) continue;
            if (!ListingEditorCriteria.IsFriendlyPadding(identity.Slots, index) || flags[index] != 0xFDFFFFFEul) return false;
            flags[index] = 0; // Managed listing representation only. No native writes.
        }
        var fields = criteria.Split('/');
        fields[^1] = Convert.ToHexString(MemoryMarshal.AsBytes(flags.AsSpan()));
        var slots = identity.Slots.Slots.Select(s => s with {
            SlotFlags = flags[s.NativeIndex], AllowedJobs = identity.Slots.JobRules.Describe(flags[s.NativeIndex])
        }).ToArray();
        current = identity with { Criteria = string.Join('/', fields), Description = description, ExtraCriteria = extra,
            Slots = identity.Slots with { Slots = slots } };
        error = ""; return true;
    }
}
