using System.Text;

namespace PartyViewKR;

// Always report raw differences, including the verified friendly padding case.
// ListingEditorCriteria separately decides which differences may be accepted.
internal static class ListingRoleComparison
{
    internal static string Describe(RefreshTarget target, string draftCriteria,
        int draftGroups, int draftMainSlots, int requestedPosition)
    {
        var count = target.Slots.NativeSlotCount;
        if (count is < 1 or > 48 || !target.Slots.MappingValid ||
            !ListingEditorCriteria.TryReadFlags(target.Criteria, count, out var expected) ||
            !ListingEditorCriteria.TryReadFlags(draftCriteria, count, out var actual))
            return "editorRoleCheck=unavailable; editorRoleReason=invalid_mapping_or_criteria";

        var slots = target.Slots;
        var known = slots.JobRules.Complete;
        var mask = slots.JobRules.AllFlags;
        var differences = 0;
        for (var i = 0; i < count; i++) if (expected[i] != actual[i]) differences++;
        var requested = slots.Slots.FirstOrDefault(s => s.Position == requestedPosition);
        var text = new StringBuilder();
        text.Append($"editorRoleCheck={(differences == 0 ? "match" : "mismatch")}; " +
            $"editorRoleLayout={ListingSlotRoute.Classify(slots)}; editorRoleRegisteredSlots={slots.TotalSlots}; " +
            $"editorRoleNativeSlots={count}; editorRoleDraftGroups={draftGroups}; editorRoleDraftMainSlots={draftMainSlots}; " +
            $"editorRoleDifferenceCount={differences}\n");
        text.Append($"editorRoleRequestedPosition={requestedPosition}; editorRoleRequestedNativeIndex={requested?.NativeIndex.ToString() ?? "none"}; " +
            $"editorRoleKnownJobsComplete={known}; editorRoleKnownJobsMask={mask:X16}\n");
        text.Append($"editorRoleExpectedFlags={string.Join(",", expected.Select(f => f.ToString("X16")))}\n");
        text.Append($"editorRoleDraftFlags={string.Join(",", actual.Select(f => f.ToString("X16")))}");
        for (var i = 0; i < count; i++)
        {
            if (expected[i] == actual[i]) continue;
            var slot = slots.Slots.FirstOrDefault(s => s.NativeIndex == i);
            var added = known ? string.Join(",", slots.JobRules.Choices.Where(j => (expected[i] & j.Flag) == 0 && (actual[i] & j.Flag) != 0).Select(j => j.Id)) : "unknown";
            var removed = known ? string.Join(",", slots.JobRules.Choices.Where(j => (expected[i] & j.Flag) != 0 && (actual[i] & j.Flag) == 0).Select(j => j.Id)) : "unknown";
            text.Append($"\neditorRoleDifferenceIndex={i}; position={slot?.Position ?? 0}; group={(char)('A' + i / 8)}; member={i % 8 + 1}; " +
                $"kind={(slot == null ? ListingEditorCriteria.IsFriendlyPadding(slots, i) ? "unused_friendly_padding" : "unregistered" : slot.HasMember ? "occupied" : "vacant")}; " +
                $"expected={expected[i]:X16}; actual={actual[i]:X16}; xor={expected[i] ^ actual[i]:X16}; " +
                $"knownJobsAgree={(known ? ((expected[i] & mask) == (actual[i] & mask)).ToString() : "unknown")}; " +
                $"addedJobIds={added}; removedJobIds={removed}");
        }
        return text.ToString();
    }

}
