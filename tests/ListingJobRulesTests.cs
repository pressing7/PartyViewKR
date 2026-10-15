using PartyViewKR;

static class ListingJobRulesTests
{
    internal static void Run(Action<string, Action> test, Action<bool, string> check)
    {
        static RecruitmentJob[] Jobs(bool include43 = false) =>
            Enumerable.Range(1, 7).Concat(Enumerable.Range(19, include43 ? 25 : 24))
                .Select(id => new RecruitmentJob((uint)id, $"직업 #{id}")).ToArray();

        test("recruitment flags map to job rows rather than bit indices", () => {
            var rules = ListingJobRules.Read(Jobs(true));
            foreach (var (flag, id) in new (ulong, uint)[] {
                (0x2, 1), (0x80, 7), (0x100, 19), (0x400, 21), (0x2000, 24),
                (0x8000, 26), (0x2000000, 36), (0x40000000, 41),
                (0x80000000, 42), (0x100000000, 43) })
            {
                var allowed = rules.Describe(flag);
                check(allowed.Complete && !allowed.Anyone, "single job became unrestricted");
                check(allowed.Names.SequenceEqual(new[] { $"직업 #{id}" }), $"wrong row for {flag:X}");
            }
        });
        test("anyone requires every runtime battle class and job including limited jobs", () => {
            var rules = ListingJobRules.Read(Jobs());
            check(rules.AllFlags == 0xFFFFFFFEul && rules.Count == 31, "wrong complete mask");
            check(rules.Describe(0xFFFFFFFEul).Anyone, "all jobs rejected");
            check(rules.Describe(ulong.MaxValue).Anyone, "unused bits hid all allowed jobs");
            for (var bit = 1; bit <= 31; bit++)
                check(!rules.Describe(0xFFFFFFFEul & ~(1ul << bit)).Anyone, $"missing job {bit} still unrestricted");
        });
        test("all jobs follow the runtime catalog instead of the SDK job count", () => {
            var oldRules = ListingJobRules.Read(Jobs());
            var newRules = ListingJobRules.Read(Jobs(true));
            check(oldRules.Describe(0xFFFFFFFEul).Anyone, "SDK-only job required in older client");
            check(!newRules.Describe(0xFFFFFFFEul).Anyone, "new runtime job omitted from all check");
            check(newRules.Describe(0x1FFFFFFFEul).Anyone, "64-bit all mask rejected");
        });
        test("tooltip lists every selected job without role summaries", () => {
            var rules = ListingJobRules.Read([
                new(40, "현자"), new(24, "백마도사"), new(28, "학자"), new(33, "점성술사"), new(19, "나이트")]);
            var allowed = rules.Describe(0x2000ul | 0x20000ul | 0x400000ul | 0x20000000ul);
            check(!allowed.Anyone && allowed.Names.SequenceEqual(new[] { "백마도사", "학자", "점성술사", "현자" }),
                "jobs omitted, grouped or in unstable order");
            check(!rules.Describe(0).Anyone && rules.Describe(0).Names.Length == 0, "zero mask became unrestricted");
            check(!rules.Describe(1ul << 63).Anyone && rules.Describe(1ul << 63).Names.Length == 0,
                "unknown bit invented a job");
        });
        test("empty or unmapped runtime catalogs cannot claim anyone", () => {
            foreach (var rules in new[] { ListingJobRules.Unavailable, ListingJobRules.Read([]),
                ListingJobRules.Read(Jobs().Append(new(44, "새 직업"))),
                ListingJobRules.Read(Jobs().Append(new(19, "중복"))) })
                check(!rules.Complete && !rules.Describe(ulong.MaxValue).Anyone, "incomplete catalog accepted");
        });
        test("empty slot restrictions are copied without replacing occupied registered jobs", () => {
            var flags = new ulong[] { 0x2000, 0x2000, 0xFFFFFFFE };
            var slots = ListingSlots.Read(true, 3, 1, 1, false, new byte[] { 21, 0, 0 },
                new ulong[] { 100, 0, 0 }, flags, id => $"직업 #{id}", ListingJobRules.Read(Jobs()));
            flags[1] = 0x100;
            check(slots.Slots[0].JobId == 21 && slots.Slots[0].Job == "직업 #21" &&
                !slots.Slots[0].AllowedJobs.Complete, "occupied job was replaced by recruitment condition");
            check(slots.Slots[1].AllowedJobs.Names.SequenceEqual(new[] { "직업 #24" }), "snapshot changed with source flags");
            check(slots.Slots[2].AllowedJobs.Anyone, "empty unrestricted slot lost");
        });
    }
}
