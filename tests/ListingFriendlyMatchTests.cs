using System.Buffers.Binary;
using System.Runtime.InteropServices;
using PartyViewKR;

static class ListingFriendlyMatchTests
{
    // KR manual clicks and detail capture: 18 visible rows in a 24-slot envelope.
    private static readonly int[] NativeIndices = [0, 1, 2, 3, 4, 8, 9, 10, 11, 12, 16, 17, 18, 19, 20, 21, 22, 23];
    private static readonly uint[] Nodes = [51, 52, 53, 54, 55, 60, 61, 62, 63, 64, 69, 70, 71, 72, 73, 74, 75, 76];
    private static readonly int[] Padding = [5, 6, 7, 13, 14, 15];

    internal static void Run(Action<string, Action> test, Action<bool, string> check)
    {
        uint[] ids = [19, 21, 32, 37, 24, 33, 28, 40, 20, 22, 30, 34, 39, 41, 43, 23,
            31, 38, 25, 27, 35, 42, 36, 1, 3, 6, 2, 4, 29, 5, 7, 26];
        var rules = ListingJobRules.Read(ids.Select(id => new RecruitmentJob(id, $"직업 {id}")));
        (byte[] jobs, ulong[] members, ulong[] flags) Arrays()
        {
            var jobs = new byte[48]; var members = new ulong[48]; var flags = new ulong[48];
            foreach (var index in NativeIndices) flags[index] = index < 16 ? 0xFDFFFFFEul : 0xFFFFFFFFFFFFFFFEul;
            jobs[0] = 25; members[0] = 10; flags[0] = 0x4000;
            return (jobs, members, flags);
        }
        ListingSlots Read(byte[] jobs, ulong[] members, ulong[] flags, int filled = 1) =>
            ListingSlots.Read(true, 18, filled, 3, true, jobs, members, flags, id => id.ToString(), rules);
        RefreshTarget Target()
        {
            var a = Arrays();
            return new(10, 123, 321, "친선경기", "128/321/1/0/1/0/0/1/" +
                Convert.ToHexString(MemoryMarshal.AsBytes(a.flags.AsSpan(0, 24))), 1800, 18) {
                Slots = Read(a.jobs, a.members, a.flags), JobEditingAllowed = true, ExtraCriteria = "0/19/True/3",
            };
        }
        RefreshTarget ReplaceRaw(RefreshTarget target, int index, ulong raw)
        {
            var fields = target.Criteria.Split('/'); var bytes = Convert.FromHexString(fields[^1]);
            BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(index * 8), raw);
            fields[^1] = Convert.ToHexString(bytes);
            var slots = target.Slots.Slots.Select(s => s.NativeIndex == index ? s with { SlotFlags = raw } : s).ToArray();
            return target with { Criteria = string.Join('/', fields), Slots = target.Slots with { Slots = slots } };
        }
        test("friendly detail shows 5 plus 5 plus 8 and retains the last spectator's registered job", () => {
            var a = Arrays(); a.jobs[23] = 19; a.members[23] = 90; a.flags[23] = 0x100;
            var slots = Read(a.jobs, a.members, a.flags, 2);
            check(slots.Readable && slots.CountsAgree && slots.MappingValid && slots.NativeSlotCount == 24, "invalid capture");
            check(slots.TotalSlots == 18 && slots.Slots.Length == 18, "wrong visible count");
            check(slots.Slots.Select(s => s.NativeIndex).SequenceEqual(NativeIndices), "native gaps collapsed incorrectly");
            check(slots.Slots.Select(s => s.Position).SequenceEqual(Enumerable.Range(1, 18)), "visible numbering has gaps");
            check(slots.Slots[^1].HasMember && slots.Slots[^1].JobId == 19 && slots.Slots[^1].MemberContentId == 90, "spectator 8 was truncated");
            a.jobs[23] = 24; a.members[23] = 99;
            check(slots.Slots[^1].JobId == 19 && slots.Slots[^1].MemberContentId == 90, "snapshot retained native array");
            check(slots.PositionLabel(slots.Slots[4]) == "별빛팀 5" && slots.PositionLabel(slots.Slots[5]) == "그림자팀 1" &&
                slots.PositionLabel(slots.Slots[10]) == "관전 1" && slots.PositionLabel(slots.Slots[17]) == "관전 8", "wrong team labels");
            check(!slots.Slots[1].AllowedJobs.Anyone && slots.Slots[16].AllowedJobs.Anyone, "fighter restriction became anyone");
        });
        test("friendly arrays require all 24 native slots, including the final six spectators", () => {
            var a = Arrays();
            check(!Read(a.jobs[..23], a.members, a.flags).Readable, "short jobs accepted");
            check(!Read(a.jobs, a.members[..23], a.flags).Readable, "short members accepted");
            check(!Read(a.jobs, a.members, a.flags[..23]).Readable, "short flags accepted");
            foreach (var index in Padding)
            for (var field = 0; field < 3; field++)
            {
                a = Arrays();
                if (field == 0) a.jobs[index] = 25;
                if (field == 1) a.members[index] = 91;
                if (field == 2) a.flags[index] = 0x100;
                check(!Read(a.jobs, a.members, a.flags).Readable, "unexpected native slot silently hidden");
            }
        });
        test("every friendly visible row routes to the traced group button and param 8", () => {
            var slots = Target().Slots;
            for (var i = 0; i < 18; i++)
            {
                check(ListingSlotRoute.TryCreate(slots, i + 1, out var route), "friendly route missing");
                check(route.Layout == RecruitmentSlotLayout.FriendlyMatch && route.NativeIndex == NativeIndices[i] &&
                    route.OpenNodeId == Nodes[i] && route.OpenParam == 8 && !route.SelectMember, "wrong friendly button");
                check(route.MatchesEditorShape(3, 5) && !route.MatchesEditorShape(3, 8) &&
                    !route.MatchesEditorShape(6, 5) && !route.MatchesEditorShape(1, 5), "editor shape mismatch admitted");
            }
            check(!ListingSlotRoute.TryCreate(slots, 0, out _) && !ListingSlotRoute.TryCreate(slots, 19, out _), "out of range");
        });
        test("friendly edits reject a packed mapping and retain the settings feature boundary", () => {
            var target = Target();
            var packed = target.Slots with { Slots = target.Slots.Slots.Select((s, i) => s with { NativeIndex = i }).ToArray() };
            check(ListingSlotRoute.Classify(packed) == RecruitmentSlotLayout.Unknown, "packed friendly mapping accepted");
            foreach (var changed in new[] { target.Slots with { IsAlliance = false }, target.Slots with { NumberOfParties = 1 },
                target.Slots with { SlotsFilled = 2 } })
                check(ListingSlotRoute.Classify(changed) == RecruitmentSlotLayout.Unknown, "unknown friendly shape accepted");
            check(ListingJobEdit.Supports(target) && !ListingJobEdit.SupportsSettings(target), "unexpected settings scope");
            check(!new ListingJobEdit(target, 1, 0x100, rules).Valid &&
                !new ListingJobEdit(target with { JobEditingAllowed = false }, 2, 0x100, rules).Valid, "occupied or blocked target accepted");
        });
        test("friendly member guards compare native indices across both teams and all spectators", () => {
            var a = Arrays(); a.jobs[12] = 24; a.members[12] = 20; a.jobs[23] = 19; a.members[23] = 30;
            var slots = Read(a.jobs, a.members, a.flags, 3);
            check(slots.MatchesMembers(a.members), "correct nonpacked members rejected");
            check(!slots.MatchesMembers(slots.Slots.Select(s => s.MemberContentId).ToArray()), "packed member array accepted");
            foreach (var index in new[] { 4, 8, 12, 16, 23 }.Concat(Padding))
            {
                var changed = a.members.ToArray(); changed[index]++;
                check(!slots.MatchesMembers(changed), "new occupant or wrong team accepted");
            }
        });
        test("friendly expected edits change exactly the intended native slot including spectator 8", () => {
            foreach (var position in new[] { 5, 6, 10, 11, 18 })
            {
                var before = Target(); var edit = new ListingJobEdit(before, position, 0x100, rules);
                var after = edit.Expected(before, 0x8000000000000100);
                check(edit.Valid && edit.MatchesResult(before, after), "valid friendly edit rejected");
                var raw = MemoryMarshal.Cast<byte, ulong>(Convert.FromHexString(after.Criteria.Split('/')[^1]));
                var original = MemoryMarshal.Cast<byte, ulong>(Convert.FromHexString(before.Criteria.Split('/')[^1]));
                check(raw.Length == 24 && raw[NativeIndices[position - 1]] == 0x8000000000000100, "wrong native write offset");
                for (var i = 0; i < 24; i++)
                    if (i != NativeIndices[position - 1]) check(raw[i] == original[i], "another team or padding changed");
                for (var i = 0; i < 18; i++)
                    if (i != position - 1) check(before.Slots.Slots[i] == after.Slots.Slots[i], "unselected row changed");
            }
        });
        test("friendly result verification rejects packed-index edits, padding changes and spectator arrivals", () => {
            var before = Target(); var edit = new ListingJobEdit(before, 18, 0x100, rules); var after = edit.Expected(before);
            check(!edit.MatchesResult(before, ReplaceRaw(before, 17, 0x100)), "visible index used as native index");
            check(!edit.MatchesResult(before, ReplaceRaw(after, 5, 0x100)), "padding changed");
            check(!edit.MatchesResult(before, ReplaceRaw(after, 22, 0x100)), "other spectator changed");
            var slots = after.Slots.Slots.ToArray(); slots[^1] = slots[^1] with { MemberContentId = 99, JobId = 19 };
            var joined = after with { Slots = after.Slots with { Slots = slots, SlotsFilled = 2 } };
            check(!edit.MatchesResult(before, joined) && !edit.MatchesOriginal(joined), "new spectator ignored");
            check(!edit.MatchesResult(before, after with { ExtraCriteria = "changed" }), "other setting changed");
        });
        test("friendly refresh simulation submits and verifies each group boundary exactly once", () => {
            foreach (var position in new[] { 5, 6, 10, 11, 18 })
            {
                var target = Target(); var edit = new ListingJobEdit(target, position, 0x100, rules);
                var host = new FakeHost { Target = target }; var flow = new ListingRefresh(host);
                flow.Request(1000, edit); flow.Update(1000); host.FinishQuery(); flow.Update(1500); flow.Update(2000);
                flow.Update(2500); flow.Update(3000);
                check(host.SubmitCalls == 1 && edit.MatchesResult(target, host.SubmittedTarget!), "wrong submitted slot");
                flow.Update(4000); host.Target = host.SubmittedTarget! with { RemainingSeconds = 3598 }; host.FinishQuery(); flow.Update(4500);
                check(flow.Stage == RefreshStage.Completed && host.JobEditCalls == 1 && host.SubmitCalls == 1, "unverified or repeated submission");
            }
        });
        test("friendly job changes do not reset anyone or toggle already excluded jobs", () => {
            check(ListingRoleButtons.TryPlanChanges(rules, 0xFDFFFFFE, 0x100, out var plan, out _), "missing delta plan");
            check(!plan.Actions.Any(a => a.Kind == RoleButtonActionKind.Anyone || a.JobId is 36 or 43), "excluded BLU/BST toggled");
            check(plan.Actions.Count(a => a.Kind == RoleButtonActionKind.ExcludeJob) == 29, "wrong exclusions");
            ulong actual = 0xFDFFFFFE;
            foreach (var action in plan.TakeBatch(1000)) actual ^= rules.Choices.Single(job => job.Id == action.JobId).Flag;
            check(actual == 0x100 && plan.TakeBatch(1000).Length == 0, "wrong result or duplicate batch");
            check(plan.TakeBatch(1100).Single().Kind == RoleButtonActionKind.Confirm && plan.TakeBatch(9999).Length == 0, "confirm repeated");
        });
        test("friendly changes add replacement jobs before removing the last current job", () => {
            check(ListingRoleButtons.TryPlanChanges(rules, 0x100, 0x2000, out var plan, out _), "replacement rejected");
            ulong actual = 0x100;
            var calls = new List<RoleButtonAction>();
            for (var now = 1000; now <= 1600; now += 100)
            foreach (var action in plan.TakeBatch(now))
            {
                calls.Add(action);
                if (action.Kind == RoleButtonActionKind.Confirm) check(actual == 0x2000, "confirmed wrong jobs");
                else actual ^= rules.Choices.Single(job => job.Id == action.JobId).Flag;
                check(actual != 0, "empty intermediate selection");
            }
            check(calls.Select(a => a.Kind).SequenceEqual(new[] { RoleButtonActionKind.IncludeJob, RoleButtonActionKind.ExcludeJob, RoleButtonActionKind.Confirm }), "wrong ordering");
            foreach (var selected in new[] { 0ul, 0x100ul, 1ul << 60 })
                check(!ListingRoleButtons.TryPlanChanges(rules, 0x100, selected, out _, out _), "invalid delta plan accepted");
        });
    }
}
