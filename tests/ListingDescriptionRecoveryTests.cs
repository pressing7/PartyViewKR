using PartyViewKR;

static class ListingDescriptionRecoveryTests
{
    internal static void Run(Action<string, Action> test, Action<bool, string> check)
    {
        test("failed description restores the actual stored text, including an empty comment", () => {
            foreach (var stored in new[] { "기존 소개말", "", "일부 저장된 내용" })
            {
                var recovery = new ListingDescriptionRecovery();
                check(recovery.Next("요청한 내용", stored, "요청한 내용", 1000) == DescriptionRecoveryAction.RestoreInput, "no reconciliation");
                check(recovery.Next(stored, stored, "요청한 내용", 1500) == DescriptionRecoveryAction.Complete && recovery.InputCalls == 1, "stored input not accepted");
            }
        });
        test("already matching input needs no recovery, even when the item reported a failure", () => {
            var recovery = new ListingDescriptionRecovery();
            check(recovery.Next("새 소개말", "새 소개말", "새 소개말", 1000) == DescriptionRecoveryAction.Complete &&
                recovery.InputCalls == 0, "successful partial write was undone");
        });
        test("recovery recognizes delayed input updates and the game's observed formatted text", () => {
            var recovery = new ListingDescriptionRecovery();
            check(recovery.Next("새 소개말", "기존", "새 소개말", 1000, "기존") == DescriptionRecoveryAction.RestoreInput,
                "delayed SetText result was mistaken for a manual change");
            recovery = new();
            check(recovery.Next("정리된 글", "기존", "정리 전 요청", 1000, "정리된 글") == DescriptionRecoveryAction.RestoreInput,
                "observed game input was not recognized");
        });
        test("unresponsive recovery input times out without retrying the native call", () => {
            var recovery = new ListingDescriptionRecovery();
            check(recovery.Next("새", "기존", "새", 1000) == DescriptionRecoveryAction.RestoreInput, "no initial input");
            check(recovery.Next("새", "기존", "새", 1500) == DescriptionRecoveryAction.Wait, "no observation wait");
            check(recovery.Next("새", "기존", "새", 4500) == DescriptionRecoveryAction.Failed &&
                recovery.Next("새", "기존", "새", 5000) == DescriptionRecoveryAction.Failed && recovery.InputCalls == 1, "repeated input");
        });
        test("recovery does not overwrite later manual input or chase a changed stored comment", () => {
            var recovery = new ListingDescriptionRecovery();
            check(recovery.Next("사용자가 바꿈", "기존", "새", 1000) == DescriptionRecoveryAction.Failed && recovery.InputCalls == 0, "manual input overwritten");
            recovery = new(); recovery.Next("새", "기존", "새", 1000);
            check(recovery.Next("기존", "별도 변경", "새", 1500) == DescriptionRecoveryAction.Failed && recovery.InputCalls == 1, "changed stored comment retried");
        });
        test("failed description no longer blocks the next setting or the single final refresh", () => {
            foreach (var throws in new[] { false, true })
            {
                var t = ListingBatchTests.Target();
                var h = new PartialDescriptionHost { Target = t, ThrowAfterRestoring = throws };
                var f = new ListingRefresh(h);
                f.RequestBatch(1000, [ListingBatchTests.Description(t), ListingBatchTests.Rule(t)]);
                f.Update(1000); h.FinishQuery(); f.Update(1500); f.Update(2000);
                long tick = 2500;
                for (var i = 0; i < 80 && f.Busy && h.SubmitCalls == 0; i++, tick += 500) f.Update(tick);
                check(h.DescriptionEditCalls == 1 && h.Recovery.InputCalls == 1 && h.RuleEditCalls == 1 && h.SubmitCalls == 1,
                    "failed description blocked later rule or repeated an operation");
                check(h.SubmittedTarget!.Description == t.Description && h.SubmittedDescriptionArgument == t.Description &&
                    ListingRules.Read(h.SubmittedTarget.Criteria, ListingRuleKind.Unrestricted) == 1, "incorrect cumulative draft submitted");
                f.Update(tick + 2000); h.Target = h.SubmittedTarget with { RemainingSeconds = 3598 }; h.FinishQuery(); f.Update(tick + 2500);
                check(f.BatchResults.Select(r => r.Applied).SequenceEqual(new bool?[] { false, true }), "incorrect partial result");
            }
        });
    }

    // Model the native input/stored-text divergence; use the real recovery
    // decision code and batch coordinator. Native UI dispatch needs game tests.
    private sealed class PartialDescriptionHost : FakeHost, IListingBatchHost
    {
        public readonly ListingDescriptionRecovery Recovery = new();
        public bool ThrowAfterRestoring;
        private RefreshTarget? stored;
        private string visible = "", ownedInput = "";
        private bool descriptionActive;
        public bool PrepareBatchStep(RefreshTarget identity, out RefreshTarget current, out string error)
        {
            if (stored == null) { stored = identity; visible = identity.Description; }
            descriptionActive = false; current = stored; error = ""; return true;
        }
        public new bool BeginDescriptionEdit(RefreshTarget target, ListingDescriptionEdit edit, out string error)
        { DescriptionEditCalls++; descriptionActive = true; visible = ownedInput = edit.Value; error = ""; return true; }
        public new SlotEditProgress UpdateDescriptionEdit(out RefreshTarget expected, out string error)
        { expected = stored!; error = "description callback did not update stored text"; return SlotEditProgress.Failed; }
        public SlotEditProgress FinishBatchStep(RefreshTarget identity, out RefreshTarget current, out string error)
        {
            current = stored!; error = "";
            if (descriptionActive)
            {
                switch (Recovery.Next(visible, current.Description, ownedInput, Environment.TickCount64))
                {
                    case DescriptionRecoveryAction.RestoreInput:
                        try
                        {
                            visible = current.Description;
                            if (ThrowAfterRestoring) throw new InvalidOperationException("input changed before dispatch threw");
                        }
                        catch (InvalidOperationException) { }
                        return SlotEditProgress.Waiting;
                    case DescriptionRecoveryAction.Wait: return SlotEditProgress.Waiting;
                    case DescriptionRecoveryAction.Failed: return SlotEditProgress.Failed;
                }
            }
            else if (RuleExpected != null) current = stored = RuleExpected;
            return SlotEditProgress.Complete;
        }
        public new bool BeginRuleEdit(RefreshTarget target, ListingRuleEdit edit, out string error)
        {
            if (!ListingIdentity.SameText(visible, target.Description)) { error = "editor_comment_mismatch"; return false; }
            return base.BeginRuleEdit(target, edit, out error);
        }
        public bool SubmitBatch(RefreshTarget identity, out string error) => Submit(stored!, stored!.Description, out error);
    }
}
