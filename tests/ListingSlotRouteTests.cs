using System.Runtime.InteropServices;
using PartyViewKR;

static class ListingSlotRouteTests
{
    internal static void Run(Action<string, Action> test, Action<bool, string> check)
    {
        var rules = ListingJobRules.Read(new[] { new RecruitmentJob(19, "나이트"), new RecruitmentJob(24, "백마도사") });
        RefreshTarget Target(int count)
        {
            var groups = count > 8 ? count / 8 : 1;
            var flags = Enumerable.Repeat(rules.AllFlags, count).ToArray();
            var jobs = new byte[count]; jobs[0] = 19;
            var members = new ulong[count]; members[0] = 10;
            return new(10, 123, 321, "소개말", "4/321/0/0/0/0/0/1/" + Convert.ToHexString(MemoryMarshal.AsBytes(flags.AsSpan())), 1800, count) {
                Slots = ListingSlots.Read(true, count, 1, groups, count > 8, jobs, members, flags, id => id.ToString(), rules),
                JobEditingAllowed = true, ExtraCriteria = $"700/0/{count > 8}/{groups}",
            };
        }
        test("normal 4/8-person slot routes preserve original node IDs and param 8", () => {
            foreach (var count in new[] { 4, 8 })
            {
                var slots = Target(count).Slots;
                uint[] nodes = [51, 52, 53, 54, 55, 56, 57, 58];
                for (var i = 0; i < count; i++)
                {
                    check(ListingSlotRoute.TryCreate(slots, i + 1, out var route), "normal slot rejected");
                    check(route.OpenNodeId == nodes[i] && route.OpenParam == 8 && !route.SelectMember, "normal path changed");
                    check(route.MatchesEditorShape(1, count) && !route.MatchesEditorShape(3, count), "normal editor shape");
                }
            }
        });
        test("24-person group boundaries use A/B/C slot buttons and registered parameters", () => {
            var slots = Target(24).Slots;
            foreach (var (position, node) in new (int, uint)[] { (1, 51), (2, 52), (8, 58), (9, 60), (16, 67), (17, 69), (24, 76) })
            {
                check(ListingSlotRoute.TryCreate(slots, position, out var route), "24p slot rejected");
                check(route.Layout == RecruitmentSlotLayout.Alliance24 && route.OpenNodeId == node && !route.SelectMember, "wrong 24p slot button");
                check(route.OpenParam == null, "unverified 24p parameter guessed");
                check(route.MatchesEditorShape(3, 8) && !route.MatchesEditorShape(1, 8) && !route.MatchesEditorShape(3, 24), "24p shape");
            }
        });
        test("48-person groups and members use independently observed node/param pairs", () => {
            var slots = Target(48).Slots; // Only one occupant; layout must still be 48p.
            (uint node, int param)[] groups = [(44, 9), (45, 10), (46, 11), (47, 12), (48, 13), (49, 14)];
            (uint node, int param)[] members = [(3, 4), (4, 5), (5, 6), (6, 7), (7, 8), (8, 9), (9, 10), (10, 11)];
            var position = 1;
            foreach (var group in groups)
            foreach (var member in members)
            {
                check(ListingSlotRoute.TryCreate(slots, position++, out var route), "48p slot rejected");
                check(route.SelectMember && route.OpenNodeId == group.node && route.OpenParam == group.param, "wrong party");
                check(route.MemberNodeId == member.node && route.MemberParam == member.param, "wrong member");
                check(route.MatchesEditorShape(6, 8) && !route.MatchesEditorShape(3, 8) && !route.MatchesEditorShape(6, 48), "48p shape");
            }
        });
        test("inconsistent shapes, party counts, bounds and PvP eligibility block edits", () => {
            var t = Target(48);
            foreach (var slots in new[] {
                t.Slots with { Readable = false }, t.Slots with { IsAlliance = false },
                t.Slots with { NumberOfParties = 3 }, t.Slots with { NumberOfParties = 0 },
                t.Slots with { TotalSlots = 24 }, t.Slots with { SlotsFilled = 2 },
                Target(24).Slots with { NumberOfParties = 6 }, Target(8).Slots with { IsAlliance = true },
            }) check(!ListingSlotRoute.TryCreate(slots, 2, out _), "unknown layout accepted");
            foreach (var position in new[] { -1, 0, 49 }) check(!ListingSlotRoute.TryCreate(t.Slots, position, out _), "invalid index accepted");
            foreach (var count in new[] { 4, 8, 24, 48 })
                check(!new ListingJobEdit(Target(count) with { JobEditingAllowed = false }, 2, 0x100, rules).Valid, "PvP admitted");
        });
        test("alliance edit retains global position and only submits its verified slot once", () => {
            foreach (var (count, position) in new[] { (24, 9), (24, 24), (48, 9), (48, 18), (48, 48) })
            {
                var t = Target(count); var edit = new ListingJobEdit(t, position, 0x100, rules);
                check(edit.Valid && !new ListingJobEdit(t, 1, 0x100, rules).Valid, "empty/occupied slot eligibility");
                var h = new FakeHost { Target = t }; var flow = new ListingRefresh(h);
                flow.Request(1000, edit); flow.Update(1000); h.FinishQuery(); flow.Update(1500); flow.Update(2000);
                check(flow.Stage == RefreshStage.EditingSlot && h.JobEditCalls == 1, "alliance edit not started");
                flow.Update(2500); flow.Update(3000);
                check(h.SubmitCalls == 1 && h.SubmittedTarget!.Slots.Slots[position - 1].SlotFlags == 0x100, "wrong/duplicate submit");
                for (var i = 0; i < count; i++)
                    if (i != position - 1) check(t.Slots.Slots[i] == h.SubmittedTarget!.Slots.Slots[i], "other group/member changed");
                flow.Update(4000); h.Target = h.SubmittedTarget! with { RemainingSeconds = 3598 }; h.FinishQuery(); flow.Update(4500);
                check(flow.Stage == RefreshStage.Completed && h.SubmitCalls == 1, "alliance result not verified");
            }
        });
        test("changing the same member number in the wrong alliance group cannot be submitted", () => {
            foreach (var count in new[] { 24, 48 })
            {
                var t = Target(count); var intended = new ListingJobEdit(t, 10, 0x100, rules);
                var h = new FakeHost { Target = t }; var flow = new ListingRefresh(h);
                flow.Request(1000, intended); flow.Update(1000); h.FinishQuery(); flow.Update(1500); flow.Update(2000);
                // A2 instead of B2: native interaction is simulated, not claimed tested.
                h.EditExpected = new ListingJobEdit(t, 2, 0x100, rules).Expected(t);
                flow.Update(2500); flow.Update(3000);
                check(flow.Stage == RefreshStage.Stopped && h.SubmitCalls == 0, "wrong party submitted");
                var correct = intended.Expected(t);
                var changedOther = new ListingJobEdit(correct, 18, 0x100, rules).Expected(correct);
                check(!intended.MatchesResult(t, changedOther), "another group's simultaneous change accepted");
            }
        });
    }
}
