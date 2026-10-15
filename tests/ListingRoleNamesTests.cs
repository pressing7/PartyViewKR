using PartyViewKR;

static class ListingRoleNamesTests
{
    internal static void Run(Action<string, Action> test, Action<bool, string> check)
    {
        var rules = ListingJobRules.Read(new[] { new RecruitmentJob(19, "나이트"), new RecruitmentJob(21, "전사"), new RecruitmentJob(24, "백마도사") });
        RoleButtonNames[] IconsOnly() => [new(18, true, "", ["나이트"]), new(19, false, "", ["전사"]), new(30, true, "", ["백마도사"])];
        test("icon-only buttons resolve their hover names without button text", () => {
            var map = ListingRoleNames.Read(rules, IconsOnly());
            check(map.Complete && map.Nodes.Count == 3 && map.Nodes[19] == 18 && map.Nodes[24] == 30, "hover name mapping");
            check(map.CheckedFlags == 0x2100, "selection must come from the matched buttons");
        });
        test("job names are matched independently of button numbering and display order", () => {
            var map = ListingRoleNames.Read(rules, [new(30, false, "", ["전사"]), new(18, true, "", ["백마도사"]), new(19, false, "", ["나이트"])]);
            check(map.Complete && map.Nodes[21] == 30 && map.Nodes[24] == 18 && map.CheckedFlags == 0x2000, "assumed fixed node order");
        });
        test("identical button and child hover labels do not create duplicate jobs", () => {
            var buttons = IconsOnly(); buttons[0] = buttons[0] with { ButtonText = "나이트", Tooltips = ["나이트", " 나이트 "] };
            check(ListingRoleNames.Read(rules, buttons).Complete, "same name counted twice");
        });
        test("conflicting names or duplicate buttons cannot authorize a click", () => {
            var buttons = IconsOnly(); buttons[0] = buttons[0] with { ButtonText = "전사" };
            var conflicting = ListingRoleNames.Read(rules, buttons);
            check(!conflicting.Complete && conflicting.AmbiguousNodes.SequenceEqual(new uint[] { 18 }), "conflicting evidence accepted");
            var duplicate = ListingRoleNames.Read(rules, IconsOnly().Append(new(63, false, "", ["나이트"])));
            check(!duplicate.Complete && duplicate.DuplicateJobs.SequenceEqual(new uint[] { 19 }), "duplicate job accepted");
        });
        test("missing and partial hover names stay unverified", () => {
            var buttons = IconsOnly(); buttons[1] = buttons[1] with { Tooltips = ["전사 선택"] };
            var missing = ListingRoleNames.Read(rules, buttons);
            check(!missing.Complete && missing.MissingJobs.SequenceEqual(new uint[] { 21 }), "substring used as identity");
            var empty = ListingRoleNames.Read(rules, IconsOnly().Select(b => b with { Tooltips = [] }));
            check(!empty.Complete && empty.Nodes.Count == 0 && empty.MissingJobs.Length == 3, "empty labels accepted");
        });
    }
}
