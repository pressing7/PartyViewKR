namespace PartyViewKR;

internal sealed record RoleButtonNames(uint NodeId, bool Checked, string ButtonText, string[] Tooltips);

// Names can belong to hover tooltips rather than to ButtonTextNode. Resolve all
// evidence before any click; never infer a job from screen order or a partial name.
internal sealed record ListingRoleNames(Dictionary<uint, uint> Nodes, ulong CheckedFlags,
    uint[] MissingJobs, uint[] AmbiguousNodes, uint[] DuplicateJobs)
{
    internal bool Complete => Nodes.Count > 0 && MissingJobs.Length + AmbiguousNodes.Length + DuplicateJobs.Length == 0;

    internal static ListingRoleNames Read(ListingJobRules rules, IEnumerable<RoleButtonNames> buttons)
    {
        var nodes = new Dictionary<uint, uint>();
        var ambiguous = new List<uint>();
        var duplicates = new List<uint>();
        ulong flags = 0;
        foreach (var button in buttons)
        {
            var names = button.Tooltips.Append(button.ButtonText).Where(n => !string.IsNullOrWhiteSpace(n)).ToArray();
            var matches = rules.Choices.Where(j => names.Any(n => ListingIdentity.SameText(j.Name, n))).ToArray();
            if (matches.Length > 1) { ambiguous.Add(button.NodeId); continue; }
            if (matches.Length == 0) continue;
            var job = matches[0];
            if (!nodes.TryAdd(job.Id, button.NodeId)) { duplicates.Add(job.Id); continue; }
            if (button.Checked) flags |= job.Flag;
        }
        return new(nodes, flags, rules.Choices.Select(j => j.Id).Except(nodes.Keys).ToArray(),
            ambiguous.ToArray(), duplicates.ToArray());
    }
}
