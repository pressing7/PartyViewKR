using System.Globalization;

namespace PartyViewKR;

internal static class ListingItemLevels
{
    internal const int Minimum = 1, Maximum = 999;
    // Recruitment details carry the effective requirement: zero means no
    // restriction. The editor separately keeps its checkbox and cached number.
    internal static int? Decode(int value) => value is >= 0 and <= Maximum ? value : null;
    internal static int? Read(string extraCriteria)
    {
        var fields = extraCriteria.Split('/');
        return fields.Length == 4 && int.TryParse(fields[0], NumberStyles.None, CultureInfo.InvariantCulture, out var value)
            ? Decode(value) : null;
    }
    internal static bool TryLevel(string text, out int value)
    {
        value = 0;
        return text.Length is >= 1 and <= 3 && text.All(c => c is >= '0' and <= '9') &&
            int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value) && value is >= Minimum and <= Maximum;
    }
    internal static string Label(int? value) => value switch {
        0 => "설정 안 함", >= Minimum and <= Maximum => $"{value} 이상", _ => "확인 불가"
    };
}

internal sealed class ListingItemLevelDraft(RefreshTarget original)
{
    internal RefreshTarget Original { get; } = original;
    internal bool Enabled { get; set; } = ListingItemLevels.Read(original.ExtraCriteria) is > 0;
    internal string LevelText { get; set; } = Math.Max(1, ListingItemLevels.Read(original.ExtraCriteria) ?? 1).ToString(CultureInfo.InvariantCulture);
    internal ListingItemLevelEdit Request => new(Original, Enabled, Enabled ? LevelText : "");
}

internal sealed record ListingItemLevelEdit(RefreshTarget Original, bool Enabled, string LevelText)
{
    internal int? Value => !Enabled ? 0 : ListingItemLevels.TryLevel(LevelText, out var value) ? value : null;
    internal bool Changed => Value.HasValue && ListingItemLevels.Read(Original.ExtraCriteria) is { } original && original != Value;
    internal static bool Supports(RefreshTarget target) => ListingJobEdit.SupportsSettings(target) && ListingItemLevels.Read(target.ExtraCriteria).HasValue;
    internal bool Valid => Supports(Original) && Changed;
    internal bool MatchesOriginal(RefreshTarget current) => Valid && Supports(current) && current.ListingId == Original.ListingId &&
        RefreshEvidence.SameRecruitment(Original, current) && ListingJobEdit.SameSlots(Original.Slots, current.Slots);
    internal RefreshTarget Expected(RefreshTarget before)
    {
        if (!Valid || !Supports(before)) throw new InvalidDataException("invalid_item_level_edit");
        var fields = before.ExtraCriteria.Split('/'); fields[0] = Value!.Value.ToString(CultureInfo.InvariantCulture);
        return before with { ExtraCriteria = string.Join('/', fields) };
    }
    internal bool MatchesResult(RefreshTarget before, RefreshTarget after) => Valid && Supports(before) && Supports(after) &&
        RefreshEvidence.SameRecruitment(Expected(before), after) && ListingJobEdit.SameSlots(before.Slots, after.Slots);
}

internal readonly record struct ItemLevelUiState(bool Checked, byte EnabledRaw, ushort StoredValue,
    int NumericValue, bool InputReady, int Minimum, int Maximum)
{
    internal bool? StoredEnabled => EnabledRaw switch { 0 => false, 1 => true, _ => null };
    internal bool CanEnter(int value) => InputReady && value is >= ListingItemLevels.Minimum and <= ListingItemLevels.Maximum &&
        Minimum <= value && value <= Maximum;
    internal bool Matches(int effective) => effective == 0 ? !Checked && StoredEnabled == false :
        Checked && StoredEnabled == true && CanEnter(effective) && NumericValue == effective && StoredValue == effective;
}

internal enum ItemLevelUiAction { Wait, Toggle, Input, Complete, Failed }

// Advance before every dispatch, so failures/timeouts never repeat a native action.
internal sealed class ListingItemLevelUi(int original, int requested)
{
    internal string Phase { get; private set; } = "ready";
    internal string Check { get; private set; } = "not_started";
    private long changedAt;
    private ushort beforeInput;

    internal ItemLevelUiAction Next(ItemLevelUiState state, long now)
    {
        if (Phase == "failed") return ItemLevelUiAction.Failed;
        if (!ListingItemLevels.Decode(original).HasValue || !ListingItemLevels.Decode(requested).HasValue || original == requested ||
            !state.StoredEnabled.HasValue || !ListingItemLevels.Decode(state.StoredValue).HasValue)
            return Fail("item_level_state_or_request_unknown");
        var enabled = requested != 0;
        if (Phase == "ready")
        {
            if (!state.Matches(original)) return Fail("item_level_original_mismatch");
            if (state.Checked != enabled) return Advance("toggling", "item_level_toggle_requested", ItemLevelUiAction.Toggle, now);
            Phase = "waiting_input"; changedAt = now;
        }
        if (Phase == "toggling")
        {
            if (state.Checked != enabled) return Fail("item_level_checkbox_mismatch");
            if (state.StoredEnabled != enabled) return Wait(now, "item_level_toggle_not_stored");
            if (now - changedAt < 100) return ItemLevelUiAction.Wait;
            if (state.Matches(requested)) return Advance("complete", "draft_item_level_verified", ItemLevelUiAction.Complete, now);
            Phase = "waiting_input"; changedAt = now;
        }
        if (Phase == "waiting_input")
        {
            if (!state.Checked || state.StoredEnabled != true) return Fail("item_level_enable_reversed");
            if (!state.InputReady) return Wait(now, "item_level_input_unavailable");
            if (!state.CanEnter(requested)) return Fail("item_level_input_range_mismatch");
            beforeInput = state.StoredValue;
            return Advance("verifying", "item_level_input_requested", ItemLevelUiAction.Input, now);
        }
        if (Phase is "verifying" or "complete")
        {
            if (state.Checked != enabled || state.StoredEnabled != enabled) return Fail("item_level_state_reversed");
            if (!state.Matches(requested))
            {
                if (Phase == "complete" || !state.CanEnter(requested) || state.NumericValue != requested || state.StoredValue != beforeInput)
                    return Fail("item_level_ui_or_stored_mismatch");
                return Wait(now, "item_level_value_not_stored");
            }
            if (Phase != "complete" && now - changedAt < 100) return ItemLevelUiAction.Wait;
            Phase = "complete"; Check = "draft_item_level_verified"; return ItemLevelUiAction.Complete;
        }
        return Fail("item_level_phase_unknown");
    }

    private ItemLevelUiAction Advance(string phase, string check, ItemLevelUiAction action, long now)
    { Phase = phase; Check = check; changedAt = now; return action; }
    private ItemLevelUiAction Wait(long now, string error)
    { Check = "waiting_" + Phase; return now - changedAt > 3000 ? Fail(error) : ItemLevelUiAction.Wait; }
    private ItemLevelUiAction Fail(string reason) { Phase = "failed"; Check = reason; return ItemLevelUiAction.Failed; }
}
