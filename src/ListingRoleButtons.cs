namespace PartyViewKR;

internal enum RoleButtonActionKind { Anyone, IncludeJob, ExcludeJob, Confirm }

internal sealed record RoleButtonAction(RoleButtonActionKind Kind, uint NodeId, int Param, uint JobId = 0);

// LookingForGroupSelectRole NodeIDs, not ClassJob row IDs or SlotFlags bit positions.
// Normal jobs: supplied PFRemote 1.0.0.12 JobNodeByRowId.
// Classes, BLU, BST, Anyone and Confirm: KR Addon Inspector, confirmed 2026-10-15.
internal static class ListingRoleButtons
{
    internal const uint Anyone = 66, Confirm = 69;
    private static readonly IReadOnlyDictionary<uint, uint> NodeByJob = new Dictionary<uint, uint>
    {
        [19] = 18, [21] = 19, [32] = 20, [37] = 21, // PLD WAR DRK GNB
        [24] = 30, [33] = 31, [28] = 32, [40] = 33, // WHM AST SCH SGE
        [20] = 42, [22] = 43, [30] = 44, [34] = 45, [39] = 46, [41] = 47, [43] = 48,
        [23] = 49, [31] = 50, [38] = 51,           // BRD MCH DNC
        [25] = 52, [27] = 53, [35] = 54, [42] = 55, [36] = 56,
        [1] = 23, [3] = 24, [6] = 35,             // GLA MRD CNJ
        [2] = 58, [4] = 59, [29] = 60, [5] = 61, [7] = 62, [26] = 63,
    };

    internal static bool TryPlan(ListingJobRules rules, ulong selected,
        out RoleButtonSequence sequence, out string error)
    {
        sequence = null!;
        error = "허용 직업을 하나 이상 선택하고 모집 정보를 다시 확인해 주세요.";
        if (!rules.Complete || selected == 0 || (selected & ~rules.AllFlags) != 0) return false;
        var missing = rules.Choices.Where(job => !NodeByJob.ContainsKey(job.Id)).Select(job => job.Id).ToArray();
        if (missing.Length > 0)
        { error = "버튼 번호가 확인되지 않은 직업이 있습니다: " + string.Join(",", missing); return false; }
        var actions = new List<RoleButtonAction> { new(RoleButtonActionKind.Anyone, Anyone, 2) };
        actions.AddRange(rules.Choices.Where(job => (selected & job.Flag) == 0)
            .Select(job => new RoleButtonAction(RoleButtonActionKind.ExcludeJob, NodeByJob[job.Id], 3, job.Id)));
        actions.Add(new(RoleButtonActionKind.Confirm, Confirm, 0));
        sequence = new(actions.ToArray());
        error = "";
        return true;
    }

    // Friendly teams and spectators may expose different allowed jobs. Start
    // from the verified slot, avoiding an Anyone reset that could select a
    // different mask. Add new choices before removing old ones to stay nonempty.
    internal static bool TryPlanChanges(ListingJobRules rules, ulong current, ulong selected,
        out RoleButtonSequence sequence, out string error)
    {
        sequence = null!;
        error = "허용 직업과 기존 모집 조건을 확인하지 못했습니다.";
        if (!rules.Complete || selected == 0 || (selected & ~rules.AllFlags) != 0 ||
            selected == (current & rules.AllFlags) || rules.Choices.Any(job => !NodeByJob.ContainsKey(job.Id))) return false;
        var actions = new List<RoleButtonAction>();
        actions.AddRange(rules.Choices.Where(job => (selected & job.Flag) != 0 && (current & job.Flag) == 0)
            .Select(job => new RoleButtonAction(RoleButtonActionKind.IncludeJob, NodeByJob[job.Id], 3, job.Id)));
        actions.AddRange(rules.Choices.Where(job => (selected & job.Flag) == 0 && (current & job.Flag) != 0)
            .Select(job => new RoleButtonAction(RoleButtonActionKind.ExcludeJob, NodeByJob[job.Id], 3, job.Id)));
        actions.Add(new(RoleButtonActionKind.Confirm, Confirm, 0));
        sequence = new(actions.ToArray());
        error = "";
        return true;
    }
}

// Original dispatch order: Anyone once, excluded jobs once each, Confirm once.
// Exclude jobs in one batch as in PFRemote. Wait at least 100 ms between
// resetting, excluding and confirming. We do not interpret button labels,
// tooltips or IsChecked; the stored draft and the fresh listing verify the result.
// Friendly matches instead batch additions, removals and confirmation in order.
internal sealed class RoleButtonSequence(RoleButtonAction[] actions)
{
    internal IReadOnlyList<RoleButtonAction> Actions { get; } = Array.AsReadOnly(actions);
    private int next;
    private long nextTick;

    internal RoleButtonAction[] TakeBatch(long now)
    {
        if (next >= actions.Length || now < nextTick) return [];
        var start = next++;
        if (actions[start].Kind is RoleButtonActionKind.IncludeJob or RoleButtonActionKind.ExcludeJob)
            while (next < actions.Length && actions[next].Kind == actions[start].Kind) next++;
        nextTick = now + 100;
        return actions[start..next]; // Reserve the batch before dispatch; never retry sent events.
    }
}
