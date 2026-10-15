namespace PartyViewKR;

internal readonly record struct ObjectiveUiState(int Index, int ListIndex, byte Stored, bool Open, bool Pending, bool Checked, int Count)
{
    internal bool SettledClosed => !Open && !Pending && !Checked;
    internal bool Matches(int index) => Index == index && ListIndex == index && Stored == ListingObjectives.Value(index);
}

internal enum ObjectiveUiAction { Wait, Open, ClickItem, SelectItem, Complete, Failed }

// The KR manual trace shows: checkbox checked + ButtonClick(2) opens the list;
// ListItemClick(1) closes it WITHOUT changing the indices; then list selection
// emits ListItemHighlight(1), which reaches the editor as ListItemHighlight(20).
// Advancing before dispatch makes every mutating action one-shot.
internal sealed class ListingObjectiveUi(int originalIndex, int selectedIndex)
{
    internal string Phase { get; private set; } = "ready";
    internal string Check { get; private set; } = "not_started";
    private long phaseTick;

    internal ObjectiveUiAction Next(ObjectiveUiState state, long now)
    {
        if (Phase == "failed") return ObjectiveUiAction.Failed;
        if (originalIndex == selectedIndex || !ListingObjectives.Value(originalIndex).HasValue ||
            !ListingObjectives.Value(selectedIndex).HasValue || state.Count != 4)
            return Fail("objective_shape_or_request_invalid");
        if (state.Index != state.ListIndex ||
            state.Index != originalIndex && state.Index != selectedIndex ||
            state.Stored != ListingObjectives.Value(originalIndex) && state.Stored != ListingObjectives.Value(selectedIndex))
            return Fail("objective_selection_changed");
        switch (Phase)
        {
            case "ready":
                if (!state.Matches(originalIndex) || !state.SettledClosed) return Fail("objective_not_original_and_closed");
                return Advance("opening", "opening_dropdown_once", ObjectiveUiAction.Open, now);
            case "opening":
                if (!state.Matches(originalIndex)) return Fail("objective_changed_before_click");
                if (state.Open && !state.Pending && state.Checked && now - phaseTick >= 100)
                    return Advance("clicked", "clicking_item_once", ObjectiveUiAction.ClickItem, now);
                return WaitOrFail(now, "dropdown_open_timeout");
            case "clicked":
                if (state.Open || !state.Matches(originalIndex)) return Fail("item_click_result_not_observed_route");
                return Advance("selecting", "selecting_item_once", ObjectiveUiAction.SelectItem, now);
            case "selecting":
                if (state.Open) return Fail("dropdown_reopened");
                if (state.Matches(selectedIndex) && state.SettledClosed && now - phaseTick >= 100)
                    return Advance("complete", "draft_objective_verified", ObjectiveUiAction.Complete, now);
                return WaitOrFail(now, "objective_ui_or_stored_timeout");
            case "complete":
                return state.Matches(selectedIndex) && state.SettledClosed ? ObjectiveUiAction.Complete : Fail("objective_changed_after_verification");
            default:
                return Fail("objective_phase_unknown");
        }
    }

    private ObjectiveUiAction Advance(string phase, string check, ObjectiveUiAction action, long now)
    { Phase = phase; Check = check; phaseTick = now; return action; }
    private ObjectiveUiAction WaitOrFail(long now, string error)
    { Check = "waiting_" + Phase; return now - phaseTick > 3000 ? Fail(error) : ObjectiveUiAction.Wait; }
    private ObjectiveUiAction Fail(string error)
    { Phase = "failed"; Check = error; return ObjectiveUiAction.Failed; }
}
