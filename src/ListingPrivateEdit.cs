using System.Globalization;

namespace PartyViewKR;

internal static class ListingPrivate
{
    internal const byte Flag = 0x02;
    // 10000 is public. KR manual ValueUpdate(27), Param 29 accepts numeric 0.
    // Require an explicit four-digit draft; never infer an entered PIN from 0 alone.
    internal const ushort PublicPassword = 10000;
    internal static bool FromFlags(byte flags) => (flags & Flag) != 0;
    internal static bool? Read(string extraCriteria) => ListingUniqueJobs.TryFlags(extraCriteria, out var flags) ? FromFlags(flags) : null;
    internal static bool? StoredEnabled(ushort password) => password <= 9999 ? true : password == PublicPassword ? false : null;
    internal static bool TryPassword(string text, out int value)
    {
        value = 0;
        if (text.Length != 4 || text.Any(c => c is < '0' or > '9')) return false;
        value = int.Parse(text, CultureInfo.InvariantCulture);
        return value is >= 0 and <= 9999;
    }
    internal static string WithEnabled(string extraCriteria, bool enabled)
    {
        if (!ListingUniqueJobs.TryFlags(extraCriteria, out var flags)) throw new InvalidDataException("invalid_join_conditions");
        var fields = extraCriteria.Split('/');
        fields[1] = (enabled ? (byte)(flags | Flag) : (byte)(flags & ~Flag)).ToString(CultureInfo.InvariantCulture);
        return string.Join('/', fields);
    }
}

internal sealed class ListingPrivateDraft(RefreshTarget original)
{
    internal RefreshTarget Original { get; } = original;
    internal bool Enabled { get; set; } = ListingPrivate.Read(original.ExtraCriteria) == true;
    // Keep leading zeroes. A listing detail does not expose its password.
    internal string Password { get; set; } = "";
    internal ListingPrivateEdit Request => new(Original, Enabled, Enabled ? Password : "");
}

internal sealed record ListingPrivateEdit(RefreshTarget Original, bool Enabled, string Password)
{
    internal static bool Supports(RefreshTarget target) => ListingJobEdit.Supports(target) && ListingPrivate.Read(target.ExtraCriteria).HasValue;
    internal bool Valid => Supports(Original) && (Enabled ? ListingPrivate.TryPassword(Password, out _) : ListingPrivate.Read(Original.ExtraCriteria) == true);
    internal int PasswordValue => Enabled && ListingPrivate.TryPassword(Password, out var value) ? value : 0;
    internal bool MatchesOriginal(RefreshTarget current) => Valid && Supports(current) && current.ListingId == Original.ListingId &&
        RefreshEvidence.SameRecruitment(Original, current) && ListingJobEdit.SameSlots(Original.Slots, current.Slots);
    internal RefreshTarget Expected(RefreshTarget before)
    {
        if (!Valid || !Supports(before)) throw new InvalidDataException("invalid_private_edit");
        return before with { ExtraCriteria = ListingPrivate.WithEnabled(before.ExtraCriteria, Enabled) };
    }
    // Detail readback confirms the private flag and other conditions, not the PIN.
    internal bool MatchesResult(RefreshTarget before, RefreshTarget after) => Valid && Supports(before) && Supports(after) &&
        RefreshEvidence.SameRecruitment(Expected(before), after) && ListingJobEdit.SameSlots(before.Slots, after.Slots);
    public override string ToString() => $"ListingPrivateEdit {{ Enabled = {Enabled}, Password = redacted }}";
}

internal readonly record struct PrivateUiState(bool Checked, ushort StoredPassword, int NumericValue, bool InputReady)
{
    internal bool? StoredEnabled => ListingPrivate.StoredEnabled(StoredPassword);
    internal bool Matches(bool enabled, int password) => Checked == enabled && StoredEnabled == enabled &&
        (!enabled || password is >= 0 and <= 9999 && InputReady && NumericValue == password && StoredPassword == password);
    public override string ToString() => $"PrivateUiState {{ Checked = {Checked}, InputReady = {InputReady}, Password = redacted }}";
}

internal enum PrivateUiAction { Wait, Toggle, Input, Complete, Failed }

// Consumes each mutation once, before native dispatch. No fallback or retry.
internal sealed class ListingPrivateUi(bool original, bool enabled, int password)
{
    internal string Phase { get; private set; } = "ready";
    internal string Check { get; private set; } = "not_started";
    private long changedAt;

    internal PrivateUiAction Next(PrivateUiState state, long now)
    {
        if (Phase == "failed") return PrivateUiAction.Failed;
        if (!state.StoredEnabled.HasValue || enabled && password is < 0 or > 9999) return Fail("private_state_unknown");
        if (Phase == "ready")
        {
            if (state.Checked != original || state.StoredEnabled != original) return Fail("private_original_mismatch");
            if (original != enabled)
            { Phase = "toggling"; changedAt = now; Check = "private_toggle_requested"; return PrivateUiAction.Toggle; }
            if (!enabled) return Fail("private_unchanged");
            Phase = "waiting_input"; changedAt = now;
        }
        if (Phase == "toggling")
        {
            if (state.Checked != enabled) return Fail("private_checkbox_mismatch");
            if (state.StoredEnabled != enabled)
                return now - changedAt > 3000 ? Fail("private_toggle_not_stored") : PrivateUiAction.Wait;
            if (now - changedAt < 100) return PrivateUiAction.Wait;
            if (!enabled) { Phase = "complete"; Check = "draft_private_verified"; return PrivateUiAction.Complete; }
            Phase = "waiting_input"; changedAt = now;
        }
        if (Phase == "waiting_input")
        {
            if (!state.Checked || state.StoredEnabled != true) return Fail("private_enable_reversed");
            if (!state.InputReady)
                return now - changedAt > 3000 ? Fail("private_input_unavailable") : PrivateUiAction.Wait;
            Phase = "verifying"; changedAt = now; Check = "private_password_requested";
            return PrivateUiAction.Input;
        }
        if (Phase is "verifying" or "complete")
        {
            if (state.Checked != enabled || state.StoredEnabled != enabled) return Fail("private_state_reversed");
            if (!state.Matches(enabled, password))
            {
                if (Phase == "complete" || !state.InputReady || state.NumericValue != password) return Fail("private_password_ui_mismatch");
                return now - changedAt > 3000 ? Fail("private_password_not_stored") : PrivateUiAction.Wait;
            }
            if (Phase != "complete" && now - changedAt < 100) return PrivateUiAction.Wait;
            Phase = "complete"; Check = "draft_private_verified"; return PrivateUiAction.Complete;
        }
        return Fail("private_phase_unknown");
    }

    private PrivateUiAction Fail(string reason) { Phase = "failed"; Check = reason; return PrivateUiAction.Failed; }
}
