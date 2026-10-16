namespace PartyViewKR;

internal static class ListingCheckboxValues
{
    internal static bool? Decode(byte value) => value switch { 0 => false, 1 => true, _ => null };
    internal static string Label(bool? value) => value switch { true => "켬", false => "끔", _ => "확인 불가" };
}

internal readonly record struct CheckboxUiState(bool Checked, byte Stored)
{
    internal bool Matches(bool enabled) => Checked == enabled && ListingCheckboxValues.Decode(Stored) == enabled;
}

internal enum CheckboxUiAction { Wait, Click, Complete, Failed }

// A checkbox is an absolute request (on/off), never an unbounded toggle loop.
// Once Click is returned, later updates only observe the UI and stored value.
internal sealed class ListingCheckboxUi(bool original, bool enabled, string setting)
{
    internal string Phase { get; private set; } = "ready";
    internal string Check { get; private set; } = "not_started";
    private long clickTick;

    internal CheckboxUiAction Next(CheckboxUiState state, long now)
    {
        if (Phase == "failed") return CheckboxUiAction.Failed;
        if (original == enabled || !ListingCheckboxValues.Decode(state.Stored).HasValue)
            return Fail($"{setting}_request_or_stored_invalid");
        if (Phase == "ready")
        {
            if (!state.Matches(original)) return Fail($"{setting}_original_ui_or_stored_mismatch");
            Phase = "verifying"; Check = $"clicking_{setting}_once"; clickTick = now;
            return CheckboxUiAction.Click;
        }
        if (state.Checked != enabled) return Fail($"{setting}_checkbox_mismatch");
        if (Phase == "complete")
            return state.Matches(enabled) ? CheckboxUiAction.Complete : Fail($"{setting}_changed_after_verification");
        if (state.Matches(enabled) && now - clickTick >= 100)
        { Phase = "complete"; Check = $"draft_{setting}_verified"; return CheckboxUiAction.Complete; }
        if (now - clickTick > 3000) return Fail($"{setting}_stored_timeout");
        Check = $"waiting_{setting}_stored";
        return CheckboxUiAction.Wait;
    }

    private CheckboxUiAction Fail(string check)
    { Phase = "failed"; Check = check; return CheckboxUiAction.Failed; }
}
