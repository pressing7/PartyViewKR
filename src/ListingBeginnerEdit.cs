using System.Globalization;

namespace PartyViewKR;

internal static class ListingBeginners
{
    internal static bool? Decode(byte value) => value switch { 0 => false, 1 => true, _ => null };
    internal static string Label(bool? value) => value switch { true => "켬", false => "끔", _ => "확인 불가" };
    internal static bool? Read(string criteria)
    {
        var fields = criteria.Split('/');
        return fields.Length == 9 && byte.TryParse(fields[3], NumberStyles.None, CultureInfo.InvariantCulture, out var value)
            ? Decode(value) : null;
    }
}

internal sealed class ListingBeginnerDraft(RefreshTarget original)
{
    internal RefreshTarget Original { get; } = original;
    internal bool Enabled { get; set; } = ListingBeginners.Read(original.Criteria) == true;
    internal ListingBeginnerEdit Request => new(Original, Enabled);
}

internal sealed record ListingBeginnerEdit(RefreshTarget Original, bool Enabled)
{
    internal byte Value => Enabled ? (byte)1 : (byte)0;
    internal bool Changed => ListingBeginners.Read(Original.Criteria) is { } original && original != Enabled;
    internal static bool Supports(RefreshTarget target) => ListingJobEdit.Supports(target) && ListingBeginners.Read(target.Criteria).HasValue;
    internal bool Valid => Supports(Original) && Changed;
    internal bool MatchesOriginal(RefreshTarget current) => Valid && Supports(current) &&
        current.ListingId == Original.ListingId && RefreshEvidence.SameRecruitment(Original, current) &&
        ListingJobEdit.SameSlots(Original.Slots, current.Slots);

    internal RefreshTarget Expected(RefreshTarget before)
    {
        if (!Valid || !Supports(before)) throw new InvalidDataException("invalid_beginner_edit");
        var fields = before.Criteria.Split('/');
        fields[3] = Value.ToString(CultureInfo.InvariantCulture);
        return before with { Criteria = string.Join('/', fields) };
    }

    internal bool MatchesResult(RefreshTarget before, RefreshTarget after) => Valid && Supports(before) && Supports(after) &&
        RefreshEvidence.SameRecruitment(Expected(before), after) && ListingJobEdit.SameSlots(before.Slots, after.Slots);
}

internal readonly record struct BeginnerUiState(bool Checked, byte Stored)
{
    internal bool Matches(bool enabled) => Checked == enabled && ListingBeginners.Decode(Stored) == enabled;
}

internal enum BeginnerUiAction { Wait, Click, Complete, Failed }

// A checkbox is an absolute request (on/off), never an unbounded toggle loop.
// Once Click is returned, later updates only observe the UI and stored value.
internal sealed class ListingBeginnerUi(bool original, bool enabled)
{
    internal string Phase { get; private set; } = "ready";
    internal string Check { get; private set; } = "not_started";
    private long clickTick;

    internal BeginnerUiAction Next(BeginnerUiState state, long now)
    {
        if (Phase == "failed") return BeginnerUiAction.Failed;
        if (original == enabled || !ListingBeginners.Decode(state.Stored).HasValue)
            return Fail("beginner_request_or_stored_invalid");
        if (Phase == "ready")
        {
            if (!state.Matches(original)) return Fail("beginner_original_ui_or_stored_mismatch");
            Phase = "verifying"; Check = "clicking_beginner_once"; clickTick = now;
            return BeginnerUiAction.Click;
        }
        if (state.Checked != enabled) return Fail("beginner_checkbox_mismatch");
        if (Phase == "complete")
            return state.Matches(enabled) ? BeginnerUiAction.Complete : Fail("beginner_changed_after_verification");
        if (state.Matches(enabled) && now - clickTick >= 100)
        { Phase = "complete"; Check = "draft_beginner_verified"; return BeginnerUiAction.Complete; }
        if (now - clickTick > 3000) return Fail("beginner_stored_timeout");
        Check = "waiting_beginner_stored";
        return BeginnerUiAction.Wait;
    }

    private BeginnerUiAction Fail(string check)
    { Phase = "failed"; Check = check; return BeginnerUiAction.Failed; }
}
