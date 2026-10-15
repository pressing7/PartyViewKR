using PartyViewKR;

static class ListingRoleButtonsTests
{
    internal static void Run(Action<string, Action> test, Action<bool, string> check)
    {
        // Independent button -> ClassJob fixture from the supplied DLL and the
        // user's KR Inspector observations; intentionally not read from the plan.
        var gameButtons = new Dictionary<uint, uint> {
            [18] = 19, [19] = 21, [20] = 32, [21] = 37, [23] = 1, [24] = 3,
            [30] = 24, [31] = 33, [32] = 28, [33] = 40, [35] = 6,
            [42] = 20, [43] = 22, [44] = 30, [45] = 34, [46] = 39, [47] = 41, [48] = 43,
            [49] = 23, [50] = 31, [51] = 38, [52] = 25, [53] = 27, [54] = 35, [55] = 42,
            [56] = 36, [58] = 2, [59] = 4, [60] = 29, [61] = 5, [62] = 7, [63] = 26,
        };
        var rules = ListingJobRules.Read(gameButtons.Values.Select(id => new RecruitmentJob(id, $"직업 {id}")));
        test("node events produce every single-job choice, mixed classes and anyone", () => {
            var selections = rules.Choices.Select(job => job.Flag).Append(rules.AllFlags)
                .Append(rules.Choices.Where(job => new uint[] { 1, 26, 36, 43 }.Contains(job.Id)).Aggregate(0ul, (mask, job) => mask | job.Flag));
            foreach (var selected in selections)
            {
                check(ListingRoleButtons.TryPlan(rules, selected, out var plan, out var error), error);
                ulong actual = 0x400; // Arbitrary existing restriction; reset must replace it.
                var calls = new List<RoleButtonAction>();
                for (var now = 1000; now <= 5000; now += 100)
                {
                    var action = plan.Take(now);
                    if (action == null) continue;
                    calls.Add(action);
                    if (action.NodeId == 66)
                    { check(action.Param == 2 && calls.Count == 1, "invalid Anyone callback/order"); actual = rules.AllFlags; }
                    else if (action.NodeId == 69)
                        check(action.Param == 0 && actual == selected, "confirmed the wrong selection");
                    else
                    {
                        check(action.Param == 3 && gameButtons.ContainsKey(action.NodeId), "invalid job callback");
                        actual ^= rules.Choices.Single(job => job.Id == gameButtons[action.NodeId]).Flag;
                        check(actual != 0, "empty intermediate selection");
                    }
                }
                check(actual == selected && calls[^1].NodeId == 69, "result/confirmation mismatch");
                check(calls.Select(action => action.NodeId).Distinct().Count() == calls.Count, "button clicked twice");
                check(plan.Take(100000) == null, "finished sequence repeated");
            }
        });
        test("anyone selection sends only reset and confirmation", () => {
            check(ListingRoleButtons.TryPlan(rules, rules.AllFlags, out var plan, out _), "valid selection rejected");
            check(plan.Actions.Count == 2 && plan.Take(1000)?.NodeId == 66 && plan.Take(1100)?.NodeId == 69, "unnecessary job toggles");
        });
        test("frequent and late frames never resend or burst button events", () => {
            ListingRoleButtons.TryPlan(rules, 0x100, out var plan, out _);
            check(plan.Take(1000)?.NodeId == 66, "missing reset");
            for (var now = 1000; now < 1100; now++) check(plan.Take(now) == null, "delay bypassed");
            var first = plan.Take(5000);
            check(first?.Kind == RoleButtonActionKind.ExcludeJob, "missing exclusion");
            check(plan.Take(5000) == null && plan.Take(5099) == null, "late frame dispatched a burst");
            check(plan.Take(5100)?.NodeId != first!.NodeId, "last event repeated");
        });
        test("empty, unknown or unreadable selections produce no button plan", () => {
            var future = ListingJobRules.Read(new[] { new RecruitmentJob(19, "나이트"), new RecruitmentJob(44, "새 직업") });
            foreach (var (r, mask) in new[] { (rules, 0ul), (rules, 1ul << 60), (ListingJobRules.Unavailable, 0x100ul), (future, 0x100ul) })
                check(!ListingRoleButtons.TryPlan(r, mask, out var plan, out var error) && plan == null && error.Length > 0, "invalid plan accepted");
        });
    }
}
