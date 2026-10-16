using System.Runtime.InteropServices;
using PartyViewKR;

static class ListingRoleComparisonTests
{
    internal static void Run(Action<string, Action> test, Action<bool, string> check)
    {
        uint[] ids = [19, 21, 32, 37, 24, 33, 28, 40, 20, 22, 30, 34, 39, 41, 43, 23,
            31, 38, 25, 27, 35, 42, 36, 1, 3, 6, 2, 4, 29, 5, 7, 26];
        var rules = ListingJobRules.Read(ids.Select(id => new RecruitmentJob(id, $"job {id}")));
        static string Criteria(ulong[] flags) => "128/321/1/0/1/0/0/1/" + Convert.ToHexString(MemoryMarshal.AsBytes(flags.AsSpan()));
        (RefreshTarget target, ulong[] flags) Friendly()
        {
            var flags = new ulong[24]; var jobs = new byte[24]; var members = new ulong[24];
            foreach (var i in Enumerable.Range(0, 5).Concat(Enumerable.Range(8, 5))) flags[i] = 0xFDFFFFFEul;
            foreach (var i in Enumerable.Range(16, 8)) flags[i] = 0xFFFFFFFFFFFFFFFEul;
            flags[0] = 0x4000; jobs[0] = 25; members[0] = 987654321012345;
            var slots = ListingSlots.Read(true, 18, 1, 3, true, jobs, members, flags, id => id.ToString(), rules, 0x13);
            return (new(987654321012345, 123456789012345, 321, "PRIVATE COMMENT", Criteria(flags), 1800, 18) {
                Slots = slots, JobEditingAllowed = true, ExtraCriteria = "0/19/True/3",
            }, flags);
        }
        test("role mismatch diagnostics distinguish raw-only bits, changed jobs and unregistered tails", () => {
            var (target, flags) = Friendly(); var draft = flags.ToArray();
            draft[1] |= 1ul << 63;
            draft[4] &= ~0x100ul;
            draft[7] = 0x100;
            draft[23] = 0x100;
            var diagnostic = ListingRoleComparison.Describe(target, Criteria(draft), 3, 5, 5);
            check(diagnostic.Contains("editorRoleDifferenceCount=4") && diagnostic.Contains("editorRoleRequestedNativeIndex=4"), "wrong raw comparison");
            var lines = diagnostic.Split('\n');
            check(lines.Single(l => l.StartsWith("editorRoleDifferenceIndex=1;")).Contains("knownJobsAgree=True"), "unknown high bit reported as a job change");
            check(lines.Single(l => l.StartsWith("editorRoleDifferenceIndex=4;")).Contains("removedJobIds=19"), "wrong job flag decoding");
            check(lines.Single(l => l.StartsWith("editorRoleDifferenceIndex=7;")).Contains("kind=unregistered"), "tail misidentified as a visible row");
            check(lines.Single(l => l.StartsWith("editorRoleDifferenceIndex=23;")).Contains("position=18; group=C; member=8"), "final spectator missing");
            check(diagnostic.Contains("actual=80000000FDFFFFFE"), "raw value lost its bit order");
        });
        test("role diagnostics preserve sources and retain their capture without identifiers or comment text", () => {
            var (target, flags) = Friendly(); var before = flags.ToArray();
            var diagnostic = ListingRoleComparison.Describe(target, Criteria(flags), 3, 5, 18);
            check(flags.SequenceEqual(before) && target.Criteria == Criteria(before) && target.Slots.Slots[0].MemberContentId == 987654321012345,
                "diagnostic changed source data");
            check(diagnostic.Contains("editorRoleCheck=match") && !diagnostic.Contains("987654321012345") &&
                !diagnostic.Contains("123456789012345") && !diagnostic.Contains("PRIVATE COMMENT"), "private fields escaped into diagnostic");
            flags[23] = 0;
            check(diagnostic.Contains("editorRoleCheck=match") && !diagnostic.Contains("editorRoleDifferenceIndex="), "capture followed later native changes");
        });
        test("role diagnostics reject truncated or malformed criteria and do not guess unknown jobs", () => {
            var (target, flags) = Friendly();
            foreach (var bad in new[] { "", Criteria(flags[..18]), Criteria(flags) + "00", Criteria(flags)[..^1] + "Z" })
                check(ListingRoleComparison.Describe(target, bad, 3, 5, 5).Contains("editorRoleCheck=unavailable"), "invalid criteria accepted");
            var unknown = target with { Slots = target.Slots with { JobRules = ListingJobRules.Unavailable } };
            flags[4] = 0x100;
            var diagnostic = ListingRoleComparison.Describe(unknown, Criteria(flags), 3, 5, 5);
            check(diagnostic.Contains("knownJobsAgree=unknown; addedJobIds=unknown; removedJobIds=unknown"), "incomplete job mapping guessed");
        });
    }
}
