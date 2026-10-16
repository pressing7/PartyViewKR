using System.Globalization;

namespace PartyViewKR;

internal static class ListingCompletions
{
    // KR manual trace: off -> command20/1/2; index0 -> 20/2/2;
    // index1 -> 20/3/2. Those callback ordinals are NOT the stored bit flags.
    internal const byte None = 1, Complete = 2, Incomplete = 4;
    internal static readonly string[] Names = ["공략완료", "미완료"];
    internal static bool Supported(byte? value) => value is None or Complete or Incomplete;
    internal static byte? Value(bool enabled, int index) => !enabled ? None : index switch { 0 => Complete, 1 => Incomplete, _ => null };
    internal static int Index(byte? value) => value switch { Complete => 0, Incomplete => 1, _ => -1 };
    internal static string Label(byte? value) => value switch {
        None => "설정 안 함", Complete => Names[0], Incomplete => Names[1],
        8 => "추가 완료조건 (현재 편집 미지원)", _ => "확인 불가"
    };
    internal static byte? Read(string criteria)
    {
        var fields = criteria.Split('/');
        return fields.Length == 9 && byte.TryParse(fields[4], NumberStyles.None, CultureInfo.InvariantCulture, out var value) &&
            (Supported(value) || value == 8) ? value : null;
    }
}

internal sealed class ListingCompletionDraft(RefreshTarget original)
{
    internal RefreshTarget Original { get; } = original;
    internal bool Enabled { get; set; } = ListingCompletions.Read(original.Criteria) != ListingCompletions.None;
    internal int SelectedIndex { get; set; } = Math.Max(0, ListingCompletions.Index(ListingCompletions.Read(original.Criteria)));
    internal ListingCompletionEdit Request => new(Original, Enabled, SelectedIndex);
}

internal sealed record ListingCompletionEdit(RefreshTarget Original, bool Enabled, int SelectedIndex)
{
    internal byte? Value => ListingCompletions.Value(Enabled, SelectedIndex);
    internal bool Changed => Value.HasValue && ListingCompletions.Read(Original.Criteria) is { } original && original != Value;
    internal static bool Supports(RefreshTarget target) => ListingJobEdit.SupportsSettings(target) && ListingCompletions.Supported(ListingCompletions.Read(target.Criteria));
    internal bool Valid => Supports(Original) && Changed;
    internal bool MatchesOriginal(RefreshTarget current) => Valid && Supports(current) &&
        current.ListingId == Original.ListingId && RefreshEvidence.SameRecruitment(Original, current) && ListingJobEdit.SameSlots(Original.Slots, current.Slots);
    internal RefreshTarget Expected(RefreshTarget before)
    {
        if (!Valid || !Supports(before)) throw new InvalidDataException("invalid_completion_edit");
        var fields = before.Criteria.Split('/'); fields[4] = Value!.Value.ToString(CultureInfo.InvariantCulture);
        return before with { Criteria = string.Join('/', fields) };
    }
    internal bool MatchesResult(RefreshTarget before, RefreshTarget after) => Valid && Supports(before) && Supports(after) &&
        RefreshEvidence.SameRecruitment(Expected(before), after) && ListingJobEdit.SameSlots(before.Slots, after.Slots);
}

internal readonly record struct CompletionUiState(bool Enabled, int Index, int ListIndex, byte Stored,
    bool Open, bool Pending, bool DropdownChecked, int Count, bool CanSelect)
{
    internal bool Closed => !Open && !Pending && !DropdownChecked;
    internal bool Matches(byte value) => Stored == value && (value == ListingCompletions.None ? !Enabled :
        Enabled && Index == ListingCompletions.Index(value) && ListIndex == Index);
    internal bool KnownSelection => Enabled && Index is 0 or 1 && Index == ListIndex &&
        Stored == ListingCompletions.Value(true, Index) && Count is >= 2 and <= 3;
}

internal enum CompletionUiAction { Wait, Toggle, Open, ClickItem, SelectItem, Complete, Failed }

internal sealed class ListingCompletionUi(byte original, byte requested)
{
    internal string Phase { get; private set; } = "ready";
    internal string Check { get; private set; } = "not_started";
    private long tick;
    private byte beforeSelection;

    internal CompletionUiAction Next(CompletionUiState state, long now)
    {
        if (Phase == "failed") return CompletionUiAction.Failed;
        if (!ListingCompletions.Supported(original) || !ListingCompletions.Supported(requested) || original == requested ||
            !ListingCompletions.Supported(state.Stored)) return Fail("completion_state_or_request_unknown");
        var enabled = requested != ListingCompletions.None;
        if (Phase == "ready")
        {
            if (!state.Matches(original) || !state.Closed) return Fail("completion_original_mismatch");
            if (state.Enabled != enabled) return Advance("toggling", "completion_toggle_requested", CompletionUiAction.Toggle, now);
            return SelectOrComplete(state, now);
        }
        if (Phase == "toggling")
        {
            if (state.Enabled != enabled || !state.Closed) return Fail("completion_toggle_ui_mismatch");
            if (now - tick < 100) return CompletionUiAction.Wait;
            if (!enabled)
                return state.Matches(requested) ? Advance("complete", "draft_completion_verified", CompletionUiAction.Complete, now) : Wait(now, "completion_toggle_not_stored");
            if (!state.KnownSelection || !state.CanSelect) return Wait(now, "completion_enable_not_ready");
            return SelectOrComplete(state, now);
        }
        if (state.Enabled != enabled) return Fail("completion_checkbox_reversed");
        switch (Phase)
        {
            case "opening":
                if (!state.Matches(beforeSelection) || !state.CanSelect) return Fail("completion_changed_before_click");
                if (state.Open && !state.Pending && state.DropdownChecked && now - tick >= 100)
                    return Advance("clicked", "completion_item_click_requested", CompletionUiAction.ClickItem, now);
                return Wait(now, "completion_dropdown_open_timeout");
            case "clicked":
                if (state.Open || !state.Matches(beforeSelection) || !state.CanSelect) return Fail("completion_click_state_unexpected");
                return Advance("selecting", "completion_selection_requested", CompletionUiAction.SelectItem, now);
            case "selecting":
                if (state.Open || !state.CanSelect || state.Index != state.ListIndex ||
                    state.Stored != beforeSelection && state.Stored != requested) return Fail("completion_selection_changed");
                if (state.Matches(requested) && state.Closed && now - tick >= 100)
                    return Advance("complete", "draft_completion_verified", CompletionUiAction.Complete, now);
                return Wait(now, "completion_ui_or_stored_timeout");
            case "complete":
                return state.Matches(requested) && state.Closed && (!enabled || state.CanSelect) ? CompletionUiAction.Complete : Fail("completion_changed_after_verification");
            default:
                return Fail("completion_phase_unknown");
        }
    }

    private CompletionUiAction SelectOrComplete(CompletionUiState state, long now)
    {
        if (!state.Closed || !state.KnownSelection || !state.CanSelect) return Fail("completion_item_unavailable");
        if (state.Matches(requested)) return Advance("complete", "draft_completion_verified", CompletionUiAction.Complete, now);
        beforeSelection = state.Stored;
        return Advance("opening", "completion_open_requested", CompletionUiAction.Open, now);
    }
    private CompletionUiAction Advance(string phase, string check, CompletionUiAction action, long now)
    { Phase = phase; Check = check; tick = now; return action; }
    private CompletionUiAction Wait(long now, string error)
    { Check = "waiting_" + Phase; return now - tick > 3000 ? Fail(error) : CompletionUiAction.Wait; }
    private CompletionUiAction Fail(string error)
    { Phase = "failed"; Check = error; return CompletionUiAction.Failed; }
}
