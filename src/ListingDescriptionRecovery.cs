namespace PartyViewKR;

internal enum DescriptionRecoveryAction { Complete, RestoreInput, Wait, Failed }

// Reconcile only the input left by this item with the actual stored comment.
// Never resend the failed comment callback or retry a native input operation.
internal sealed class ListingDescriptionRecovery
{
    internal string Check { get; private set; } = "not_started";
    internal int InputCalls { get; private set; }
    private string? restoreValue;
    private long restoreTick;
    private bool failed;

    internal DescriptionRecoveryAction Next(string visible, string stored, string requestedInput, long now, string? observedInput = null)
    {
        if (failed) return DescriptionRecoveryAction.Failed;
        if (restoreValue != null && !ListingIdentity.SameText(stored, restoreValue))
            return Fail("recovery_stored_changed");
        if (ListingIdentity.SameText(visible, stored))
        { Check = "recovery_input_matches_stored"; return DescriptionRecoveryAction.Complete; }
        if (restoreValue != null)
        {
            if (now - restoreTick > 3000) return Fail("recovery_input_timeout");
            Check = "recovery_input_waiting";
            return DescriptionRecoveryAction.Wait;
        }
        if (!ListingIdentity.SameText(visible, requestedInput) &&
            (observedInput == null || !ListingIdentity.SameText(visible, observedInput)))
            return Fail("recovery_input_changed_elsewhere");
        // Consume before calling SetText, even if it throws after changing the UI.
        restoreValue = stored; restoreTick = now; InputCalls++;
        Check = "recovery_setting_input_once";
        return DescriptionRecoveryAction.RestoreInput;
    }

    private DescriptionRecoveryAction Fail(string check)
    { failed = true; Check = check; return DescriptionRecoveryAction.Failed; }
}
