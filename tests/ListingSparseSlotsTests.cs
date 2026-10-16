using System.Buffers.Binary;
using System.Runtime.InteropServices;
using PartyViewKR;

static class ListingSparseSlotsTests
{
    internal static void Run(Action<string, Action> test, Action<bool, string> check)
    {
        uint[] ids = [19, 21, 32, 37, 24, 33, 28, 40, 20, 22, 30, 34, 39, 41, 43, 23,
            31, 38, 25, 27, 35, 42, 36, 1, 3, 6, 2, 4, 29, 5, 7, 26];
        var rules = ListingJobRules.Read(ids.Select(id => new RecruitmentJob(id, $"직업 {id}")));

        // The game moves unregistered slots to the end of EACH group at creation.
        // Inputs below represent the fetched result, not the user's pre-creation clicks.
        RefreshTarget Target(int[] registeredPerGroup, bool friendly = false)
        {
            var jobs = new byte[48]; var members = new ulong[48]; var flags = new ulong[48];
            for (var group = 0; group < registeredPerGroup.Length; group++)
            for (var member = 0; member < registeredPerGroup[group]; member++)
                flags[group * 8 + member] = friendly && group < 2 ? 0xFDFFFFFEul : 0xFFFFFFFFFFFFFFFEul;
            var first = Array.FindIndex(flags, f => f != 0);
            jobs[first] = 25; members[first] = 10; flags[first] = 0x4000;
            var count = registeredPerGroup.Sum(); var groups = registeredPerGroup.Length;
            var join = (byte)(friendly ? 0x13 : groups > 1 ? 5 : 1);
            var slots = ListingSlots.Read(true, count, 1, groups, groups > 1, jobs, members, flags,
                id => id.ToString(), rules, join);
            return new(10, 123, 321, "기존 소개말", $"{(friendly ? 128 : 4)}/321/1/0/1/0/0/1/" +
                Convert.ToHexString(MemoryMarshal.AsBytes(flags.AsSpan(0, slots.NativeSlotCount))), 1800, count) {
                Slots = slots, JobEditingAllowed = true, ExtraCriteria = $"0/{join}/{groups > 1}/{groups}",
            };
        }
        ulong[] RawFlags(RefreshTarget target) => Convert.FromHexString(target.Criteria.Split('/')[^1])
            .Chunk(8).Select(b => BinaryPrimitives.ReadUInt64LittleEndian(b)).ToArray();
        void Complete(FakeHost host, ListingRefresh flow)
        {
            flow.Update(1000); host.FinishQuery(); flow.Update(1500); flow.Update(2000);
            flow.Update(2500); flow.Update(3000);
            check(host.EditCalls == 1 && host.SubmitCalls == 1 && host.SubmittedTarget != null,
                $"edit did not submit once: {flow.Diagnostic}");
            flow.Update(4000); host.Target = host.SubmittedTarget! with { RemainingSeconds = 3598 };
            host.FinishQuery(); flow.Update(4500);
            check(flow.Stage == RefreshStage.Completed && host.SubmitCalls == 1, "fresh result rejected or duplicate submit");
        }

        test("24 and 48 player layouts with a group tail removed retain their final group", () => {
            foreach (var counts in new[] { new[] { 8, 7, 8 }, new[] { 8, 8, 8, 8, 7, 8 } })
            {
                var slots = Target(counts).Slots; var nativeCount = counts.Length * 8;
                check(slots.CountsAgree && slots.MappingValid && slots.Slots.Length == nativeCount - 1, "removed tail miscounted");
                check(slots.UnregisteredNativeIndices.SequenceEqual(new[] { nativeCount - 9 }), "wrong unregistered tail");
                check(slots.Slots[^1].NativeIndex == nativeCount - 1, "final position truncated");
                check(slots.PositionLabel(slots.Slots[^1]) == $"{(char)('A' + counts.Length - 1)}파티 8", "final group mislabeled");
                check(ListingSlotRoute.Classify(slots) == (counts.Length == 3 ? RecruitmentSlotLayout.Alliance24 : RecruitmentSlotLayout.Alliance48),
                    "layout depends on registered count equaling 24 or 48");
            }
        });
        test("B party removal uses the fetched B1 through B7 and keeps C1 at native index 16", () => {
            var slots = Target([8, 7, 8]).Slots;
            check(slots.Slots.Skip(8).Take(7).Select(s => s.NativeIndex).SequenceEqual(Enumerable.Range(8, 7)), "pre-creation clicked B1 was hidden");
            check(slots.PositionLabel(slots.Slots[8]) == "B파티 1" && slots.PositionLabel(slots.Slots[15]) == "C파티 1", "groups packed globally");
            check(ListingSlotRoute.TryCreate(slots, 16, out var route) && route.NativeIndex == 16 && route.OpenNodeId == 69, "C1 routed to B8");
        });
        test("multiple group tails and an empty first group preserve native group numbers", () => {
            foreach (var counts in new[] { new[] { 5, 7, 6 }, new[] { 0, 7, 8 }, new[] { 6, 8, 7, 0, 8, 5 } })
            {
                var slots = Target(counts).Slots;
                var expected = counts.SelectMany((count, group) => Enumerable.Range(group * 8, count)).ToArray();
                check(slots.MappingValid && slots.CountsAgree && slots.Slots.Select(s => s.NativeIndex).SequenceEqual(expected), "group tail lost");
                for (var i = 0; i < expected.Length; i++)
                    check(ListingSlotRoute.TryCreate(slots, i + 1, out var route) && route.NativeIndex == expected[i] &&
                        route.MatchesEditorShape(counts.Length, counts[0]) && route.MatchesEditorShape(counts.Length, 8), "group route mismatch");
            }
        });
        test("friendly 5 plus 5 plus 8 reads every spectator regardless of the alliance boolean", () => {
            foreach (var alliance in new[] { true, false })
            {
                var slots = Target([5, 5, 8], true).Slots with { IsAlliance = alliance };
                check(slots.IsFriendlyMatch && slots.MappingValid && slots.Slots.Length == 18 &&
                    slots.Slots.Skip(10).Select(s => s.NativeIndex).SequenceEqual(Enumerable.Range(16, 8)), "spectators truncated");
                check(slots.PositionLabel(slots.Slots[^1]) == "관전 8" && ListingSlotRoute.SupportsSettings(slots), "friendly edit still blocked");
            }
        });
        test("an ordinary alliance with 18 registered positions does not become a friendly match", () => {
            var slots = Target([5, 5, 8]).Slots;
            check(slots.MappingValid && slots.TotalSlots == 18 && !slots.IsFriendlyMatch &&
                ListingSlotRoute.Classify(slots) == RecruitmentSlotLayout.Alliance24, "count alone determined friendly mode");
            check(slots.PositionLabel(slots.Slots[5]) == "B파티 1", "ordinary alliance renamed");
        });
        test("friendly additional unregistered team and spectator tails preserve their original indices", () => {
            var slots = Target([4, 5, 7], true).Slots;
            check(slots.IsFriendlyMatch && slots.MappingValid && slots.Slots.Length == 16 && slots.Slots[^1].NativeIndex == 22, "smaller friendly layout rejected");
            check(slots.PositionLabel(slots.Slots[4]) == "그림자팀 1" && slots.PositionLabel(slots.Slots[^1]) == "관전 7", "friendly tail mislabeled");
            check(ListingSlotRoute.TryCreate(slots, 16, out var route) && route.OpenNodeId == 75 && route.OpenParam == 8 &&
                route.MatchesEditorShape(3, 4) && route.MatchesEditorShape(3, 5) && route.MatchesEditorShape(3, 8) &&
                !route.MatchesEditorShape(3, 7) && !route.MatchesEditorShape(6, 5), "friendly route/shape mismatch");
        });
        test("normal party tails still support edits without showing unused native positions", () => {
            foreach (var count in new[] { 3, 4, 6, 7, 8 })
            {
                var target = Target([count]); var slots = target.Slots;
                check(slots.Slots.Length == count && slots.NativeSlotCount == count &&
                    ListingJobEdit.SupportsSettings(target), "normal party rejected");
                check(ListingSlotRoute.TryCreate(slots, count, out var route) && route.OpenNodeId == 50 + count &&
                    route.MatchesEditorShape(1, count), "normal button changed");
            }
        });
        test("member verification includes unregistered tails and the final registered member", () => {
            var target = Target([8, 7, 8]); var slots = target.Slots;
            var members = new ulong[48]; members[0] = 10;
            check(slots.MatchesMembers(members), "matching members rejected");
            foreach (var index in new[] { 14, 15, 16, 23 })
            {
                var changed = members.ToArray(); changed[index] = 90;
                check(!slots.MatchesMembers(changed), "new occupant unnoticed across a group boundary");
            }
            check(!slots.MatchesMembers(members[..23]), "truncated array accepted");
        });
        test("mismatched slot counts and overlapping or packed mappings remain uneditable", () => {
            var target = Target([8, 7, 8]); var slots = target.Slots;
            foreach (var bad in new[] { slots with { TotalSlots = 22 }, slots with { SlotsFilled = 2 },
                slots with { UnregisteredNativeIndices = [14] },
                slots with { Slots = slots.Slots.Select((s, i) => s with { NativeIndex = i }).ToArray() } })
                check(!ListingJobEdit.SupportsSettings(target with { Slots = bad }), "unverified mapping accepted");
            check(!new ListingJobEdit(target, 2, 0, rules).Valid, "refresh can create an unregistered slot");
        });
        test("job edits after group tails change only the intended native position and submit once", () => {
            foreach (var target in new[] { Target([8, 7, 8]), Target([8, 8, 8, 8, 7, 8]), Target([4, 5, 7], true), Target([6]) })
            {
                var position = target.TotalSlots; var nativeIndex = target.Slots.Slots[^1].NativeIndex;
                var edit = new ListingJobEdit(target, position, 0x100, rules);
                check(edit.Valid, "registered empty slot rejected");
                var expected = edit.Expected(target); var before = RawFlags(target); var after = RawFlags(expected);
                check(after.Length == target.Slots.NativeSlotCount, "criteria truncated at registered count");
                for (var i = 0; i < before.Length; i++) check(after[i] == (i == nativeIndex ? 0x100ul : before[i]), "wrong native slot written");
                var host = new FakeHost { Target = target }; var flow = new ListingRefresh(host);
                flow.Request(1000, edit); Complete(host, flow);
                check(edit.MatchesResult(target, host.Target) && host.JobEditCalls == 1, "job result changed another position");
            }
        });
        test("shared setting edits work with group tails while preserving all native slot flags", () => {
            foreach (var target in new[] { Target([8, 7, 8]), Target([8, 8, 8, 8, 7, 8]), Target([5, 5, 8], true), Target([4, 5, 7], true) })
            {
                Action<ListingRefresh>[] requests = [
                    f => f.Request(1000, descriptionEdit: new(target, "새 소개말")),
                    f => f.Request(1000, objectiveEdit: new(target, 1)),
                    f => f.Request(1000, beginnerEdit: new(target, true)),
                    f => f.Request(1000, uniqueJobsEdit: new(target, true)),
                    f => f.Request(1000, privateEdit: new(target, true, "0123")),
                    f => f.Request(1000, completionEdit: new(target, true, 0)),
                    f => f.Request(1000, itemLevelEdit: new(target, true, "555")),
                    f => f.Request(1000, ruleEdit: new(target, ListingRuleKind.Unrestricted, 1)),
                    f => f.Request(1000, ruleEdit: new(target, ListingRuleKind.MinimumItemLevel, 1)),
                    f => f.Request(1000, ruleEdit: new(target, ListingRuleKind.SilenceEcho, 1)),
                    f => f.Request(1000, ruleEdit: new(target, ListingRuleKind.Loot, 1)),
                ];
                // FakeHost verifies managed gating and result comparison only.
                // Native hosts still require the actual game control to be enabled.
                foreach (var request in requests)
                {
                    var host = new FakeHost { Target = target }; var flow = new ListingRefresh(host);
                    request(flow); Complete(host, flow);
                    check(ListingJobEdit.SameSlots(target.Slots, host.Target.Slots) && RawFlags(target).SequenceEqual(RawFlags(host.Target)),
                        "setting edit changed registered slots or unregistered tails");
                }
            }
        });
        test("a newly registered tail invalidates an older edit snapshot", () => {
            var original = Target([8, 7, 8]); var restored = Target([8, 8, 8]);
            var edit = new ListingJobEdit(original, 16, 0x100, rules);
            check(!edit.MatchesOriginal(restored) && !ListingJobEdit.SameSlots(original.Slots, restored.Slots), "stale visible row can target B8 instead of C1");
        });
    }
}
