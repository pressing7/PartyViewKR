namespace PartyViewKR;

internal readonly record struct LootUiState(int Index, int ListIndex, byte Stored, bool Open, bool Pending, bool Checked, int Count)
{
    internal bool Matches(int index) => Index == index && ListIndex == index && Stored == index;
    internal bool SettledClosed => !Open && !Pending && !Checked;
}

internal enum LootUiAction { Wait, Open, ClickItem, SelectItem, Complete, Failed }

// The same native dropdown sequence as purpose/completion: open, item click,
// then SelectItem with its highlight event. Advance before each dispatch.
internal sealed class ListingLootUi(int original, int selected)
{
    internal string Phase { get; private set; } = "ready";
    internal string Check { get; private set; } = "not_started";
    private long changedAt;

    internal LootUiAction Next(LootUiState state, long now)
    {
        if (Phase == "failed") return LootUiAction.Failed;
        if (original is < 0 or > 2 || selected is < 0 or > 2 || original == selected || state.Count != 3 ||
            state.Index is < 0 or > 2 || state.ListIndex is < 0 or > 2 || state.Stored > 2)
            return Fail("loot_state_or_request_unknown");
        switch (Phase)
        {
            case "ready":
                if (!state.Matches(original) || !state.SettledClosed) return Fail("loot_original_mismatch");
                return Advance("opening", "loot_open_requested", LootUiAction.Open, now);
            case "opening":
                if (!state.Matches(original)) return Fail("loot_changed_before_selection");
                if (state.Open && !state.Pending && state.Checked && now - changedAt >= 100)
                    return Advance("clicked", "loot_item_click_requested", LootUiAction.ClickItem, now);
                return Wait(now, "loot_open_timeout");
            case "clicked":
                if (!state.Matches(original) || state.Open) return Fail("loot_item_click_state_mismatch");
                return Advance("selecting", "loot_select_requested", LootUiAction.SelectItem, now);
            case "selecting":
                if (state.Open) return Fail("loot_reopened");
                if (state.Matches(selected) && state.SettledClosed && now - changedAt >= 100)
                    return Advance("complete", "draft_loot_verified", LootUiAction.Complete, now);
                if (state.Index != selected && state.Index != original || state.ListIndex != selected && state.ListIndex != original ||
                    state.Stored != selected && state.Stored != original) return Fail("loot_unexpected_selection");
                return Wait(now, "loot_selection_not_stored");
            case "complete":
                return state.Matches(selected) && state.SettledClosed ? LootUiAction.Complete : Fail("loot_final_mismatch");
            default: return Fail("loot_phase_unknown");
        }
    }

    private LootUiAction Advance(string phase, string check, LootUiAction action, long now)
    { Phase = phase; Check = check; changedAt = now; return action; }
    private LootUiAction Wait(long now, string error)
    { Check = "waiting_" + Phase; return now - changedAt > 3000 ? Fail(error) : LootUiAction.Wait; }
    private LootUiAction Fail(string reason) { Phase = "failed"; Check = reason; return LootUiAction.Failed; }
}
