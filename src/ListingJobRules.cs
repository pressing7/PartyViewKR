namespace PartyViewKR;

internal sealed record RecruitmentJob(uint Id, string Name);
internal sealed record RecruitmentJobOption(uint Id, string Name, ulong Flag);

internal sealed record ListingAllowedJobs(bool Anyone, string[] Names, bool Complete)
{
    internal uint[] Ids { get; init; } = [];
    internal static readonly ListingAllowedJobs Unavailable = new(false, [], false);
}

// The flag index is NOT the ClassJob row ID: crafting/gathering rows are skipped.
// Mapping: Dalamud/Game/Gui/PartyFinder/Types/{JobFlags,JobFlagsExtensions}.cs.
// Include only battle classes/jobs present in the runtime's ClassJob sheet when
// deciding whether every job is allowed; SDKs can contain jobs absent in KR.
internal sealed class ListingJobRules
{
    private static readonly uint[] JobIdsByFlag =
        [0, 1, 2, 3, 4, 5, 6, 7, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28,
         29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43];

    // Shared by the tooltip and editor; keep the user's order, including classes.
    private static readonly uint[] DisplayOrder =
        [19, 21, 32, 37, 24, 33, 28, 40, 20, 22, 30, 34, 39, 41, 43, 23,
         31, 38, 25, 27, 35, 42, 36, 1, 3, 6, 2, 4, 29, 5, 7, 26];
    private readonly RecruitmentJobOption[] jobs;
    internal IReadOnlyList<RecruitmentJobOption> Choices { get; }
    internal bool Complete { get; }
    internal ulong AllFlags { get; }
    internal int Count => jobs.Length;
    internal static readonly ListingJobRules Unavailable = new([], false);

    private ListingJobRules(RecruitmentJobOption[] jobs, bool complete)
    {
        this.jobs = jobs;
        Choices = Array.AsReadOnly(jobs);
        Complete = complete;
        AllFlags = jobs.Aggregate(0ul, (mask, job) => mask | job.Flag);
    }

    internal static ListingJobRules Read(IEnumerable<RecruitmentJob> battleJobs)
    {
        var jobs = new List<RecruitmentJobOption>();
        var seen = new HashSet<uint>();
        var complete = true;
        foreach (var job in battleJobs)
        {
            var bit = Array.IndexOf(JobIdsByFlag, job.Id);
            if (bit <= 0 || string.IsNullOrWhiteSpace(job.Name) || !seen.Add(job.Id))
            {
                complete = false;
                continue;
            }
            jobs.Add(new(job.Id, job.Name, 1ul << bit));
        }
        return new(jobs.OrderBy(j => Array.IndexOf(DisplayOrder, j.Id)).ToArray(), complete && jobs.Count > 0);
    }

    internal ListingAllowedJobs Describe(ulong flags)
    {
        var allowed = jobs.Where(j => (flags & j.Flag) != 0).ToArray();
        return new(Complete && (flags & AllFlags) == AllFlags,
            allowed.Select(j => j.Name).ToArray(), Complete) { Ids = allowed.Select(j => j.Id).ToArray() };
    }
}
