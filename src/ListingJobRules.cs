namespace PartyViewKR;

internal sealed record RecruitmentJob(uint Id, string Name);

internal sealed record ListingAllowedJobs(bool Anyone, string[] Names, bool Complete)
{
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

    private readonly (ulong Flag, string Name)[] jobs;
    internal bool Complete { get; }
    internal ulong AllFlags { get; }
    internal int Count => jobs.Length;
    internal static readonly ListingJobRules Unavailable = new([], false);

    private ListingJobRules((ulong Flag, string Name)[] jobs, bool complete)
    {
        this.jobs = jobs;
        Complete = complete;
        AllFlags = jobs.Aggregate(0ul, (mask, job) => mask | job.Flag);
    }

    internal static ListingJobRules Read(IEnumerable<RecruitmentJob> battleJobs)
    {
        var jobs = new List<(ulong Flag, string Name)>();
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
            jobs.Add((1ul << bit, job.Name));
        }
        return new(jobs.OrderBy(j => j.Flag).ToArray(), complete && jobs.Count > 0);
    }

    internal ListingAllowedJobs Describe(ulong flags) => new(
        Complete && (flags & AllFlags) == AllFlags,
        jobs.Where(j => (flags & j.Flag) != 0).Select(j => j.Name).ToArray(), Complete);
}
