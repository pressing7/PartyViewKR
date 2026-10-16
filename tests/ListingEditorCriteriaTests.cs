using System.Runtime.InteropServices;
using PartyViewKR;

static class ListingEditorCriteriaTests
{
    private static readonly int[] Padding = [5, 6, 7, 13, 14, 15];
    private const ulong DefaultTeamFlags = 0xFDFFFFFEul;

    internal static void Run(Action<string, Action> test, Action<bool, string> check)
    {
        uint[] ids = [19, 21, 32, 37, 24, 33, 28, 40, 20, 22, 30, 34, 39, 41, 43, 23,
            31, 38, 25, 27, 35, 42, 36, 1, 3, 6, 2, 4, 29, 5, 7, 26];
        var rules = ListingJobRules.Read(ids.Select(id => new RecruitmentJob(id, $"job {id}")));
        string Criteria(ulong[] flags) => "128/321/1/0/1/0/0/1/" + Convert.ToHexString(MemoryMarshal.AsBytes(flags.AsSpan()));
        RefreshTarget Target(ulong[] flags, byte join = 0x13)
        {
            var jobs = new byte[flags.Length]; var members = new ulong[flags.Length];
            jobs[0] = 25; members[0] = 10;
            var groups = flags.Length == 48 ? 6 : flags.Length == 24 ? 3 : 1;
            var count = flags.Count(f => f != 0);
            return new(10, 123, 321, "기존 소개말", Criteria(flags), 1800, count) {
                Slots = ListingSlots.Read(true, count, 1, groups, groups > 1, jobs, members, flags, id => id.ToString(), rules, join),
                JobEditingAllowed = true, ExtraCriteria = $"0/{join}/{groups > 1}/{groups}",
            };
        }
        // The user's exact 0.3.27 role arrays: 5 + 4 + 8, with B5 removed.
        ulong[] CapturedListing() => [
            0x4000, 0xFDFFFFFE, 0xFDFFFFFE, 0xFDFFFFFE, 0xFDFFFFFE, 0, 0, 0,
            0xFDFFFFFE, 0xFDFFFFFE, 0xFDFFFFFE, 0xFDFFFFFE, 0, 0, 0, 0,
            0xFFFFFFFFFFFFFFFE, 0xFFFFFFFFFFFFFFFE, 0xFFFFFFFFFFFFFFFE, 0xFFFFFFFFFFFFFFFE,
            0xFFFFFFFFFFFFFFFE, 0xFFFFFFFFFFFFFFFE, 0xFFFFFFFFFFFFFFFE, 0xFFFFFFFFFFFFFFFE,
        ];
        ulong[] EditorFlags(ulong[] listing)
        {
            var result = listing.ToArray();
            foreach (var i in Padding) result[i] = DefaultTeamFlags;
            return result;
        }
        string EditorCriteria(RefreshTarget target)
        {
            check(ListingEditorCriteria.TryReadFlags(target.Criteria, 24, out var flags), "invalid test target");
            var fields = target.Criteria.Split('/');
            fields[^1] = Convert.ToHexString(MemoryMarshal.AsBytes(EditorFlags(flags).AsSpan()));
            return string.Join('/', fields);
        }
        test("captured friendly 17-slot listing accepts only the six unused editor defaults", () => {
            var listing = CapturedListing(); var editor = EditorFlags(listing); var target = Target(listing);
            check(target.TotalSlots == 17 && target.Slots.MappingValid && target.Slots.IsFriendlyMatch, "capture metadata mismatch");
            check(ListingEditorCriteria.Matches(target, Criteria(editor), 3, 5, out var accepted) && accepted.SequenceEqual(Padding),
                "captured editor still blocked");
            check(target.Criteria == Criteria(listing) && listing[12] == 0 && editor[12] == 0 &&
                target.Slots.UnregisteredNativeIndices.Contains(12), "real unregistered B5 was changed");
            check(listing.Where((_, i) => Padding.Contains(i)).All(f => f == 0) && editor[5] == DefaultTeamFlags,
                "comparison rewrote either source");
            check(ListingEditorCriteria.Matches(target, target.Criteria, 3, 5, out accepted) && accepted.Length == 0,
                "exact matching arrays need an exception");
        });
        test("every real friendly position and extra unregistered slot remains an exact comparison", () => {
            var listing = CapturedListing(); var target = Target(listing);
            foreach (var index in Enumerable.Range(0, 24).Except(Padding))
            {
                var editor = EditorFlags(listing); editor[index] ^= 0x100;
                check(!ListingEditorCriteria.Matches(target, Criteria(editor), 3, 5, out var accepted) && accepted.Length == 0,
                    $"changed real slot {index} accepted");
            }
            var restoredB5 = EditorFlags(listing); restoredB5[12] = DefaultTeamFlags;
            check(!ListingEditorCriteria.Matches(target, Criteria(restoredB5), 3, 5, out _), "extra unregistered B5 ignored");
        });
        test("unused friendly positions reject unobserved masks instead of ignoring arbitrary values", () => {
            var listing = CapturedListing(); var target = Target(listing);
            foreach (var index in Padding)
            foreach (var value in new[] { 1ul, 0x100ul, 0xFFFFFFFFFFFFFFFEul, DefaultTeamFlags | (1ul << 32), DefaultTeamFlags | (1ul << 63) })
            {
                var editor = EditorFlags(listing); editor[index] = value;
                check(!ListingEditorCriteria.Matches(target, Criteria(editor), 3, 5, out _), "unknown padding representation accepted");
            }
        });
        test("ordinary 24 and 48 groups do not inherit the friendly unused-slot exception", () => {
            foreach (var count in new[] { 24, 48 })
            {
                var listing = Enumerable.Repeat(0xFFFFFFFFFFFFFFFEul, count).ToArray(); listing[0] = 0x4000;
                foreach (var i in Padding) listing[i] = 0;
                var target = Target(listing, 5); var groups = count / 8;
                check(ListingEditorCriteria.Matches(target, target.Criteria, groups, 8, out _), "existing alliance equality broken");
                check(!ListingEditorCriteria.Matches(target, Criteria(EditorFlags(listing)), groups, 8, out _), "alliance tail ignored");
            }
            var normal = Target([0x4000, 0x100, 0x100, 0x100], 1);
            check(ListingEditorCriteria.Matches(normal, normal.Criteria, 1, 4, out _) &&
                !ListingEditorCriteria.Matches(normal, Criteria([0x4000, 0x100, 0x100, 0x200]), 1, 4, out _), "normal exact comparison changed");
        });
        test("friendly padding never excuses changed conditions or an unverified shape", () => {
            var target = Target(CapturedListing()); var editor = EditorCriteria(target);
            for (var field = 0; field < 8; field++)
            {
                var changed = editor.Split('/'); changed[field] = (int.Parse(changed[field]) + 1).ToString();
                check(!ListingEditorCriteria.Matches(target, string.Join('/', changed), 3, 5, out _), "unrelated condition ignored");
            }
            foreach (var (groups, main) in new[] { (1, 5), (6, 5), (3, 7) })
                check(!ListingEditorCriteria.Matches(target, editor, groups, main, out _), "unknown editor shape accepted");
            foreach (var bad in new[] { target with { TotalSlots = 18 }, target with { Slots = target.Slots with { SlotsFilled = 2 } },
                target with { Slots = target.Slots with { JoinConditionFlags = 3 } } })
                check(!ListingEditorCriteria.Matches(bad, editor, 3, 5, out _), "unknown listing shape accepted");
            foreach (var bad in new[] { editor[..^16], editor + "00", editor[..^1] + "Z" })
                check(!ListingEditorCriteria.Matches(target, bad, 3, 5, out _), "bad role array accepted");
        });
        test("job edits retain zero listing padding while validating native defaults before submission", () => {
            foreach (var restoreB5 in new[] { false, true })
            {
                var listing = CapturedListing(); if (restoreB5) listing[12] = DefaultTeamFlags;
                var target = Target(listing);
                foreach (var position in new[] { 3, 6, target.TotalSlots })
                {
                    var edit = new ListingJobEdit(target, position, 0x100, rules);
                    check(edit.Valid && ListingEditorCriteria.Matches(target, EditorCriteria(target), 3, 5, out _), "editor entry blocked");
                    var expected = edit.Expected(target); var native = EditorCriteria(expected);
                    check(ListingEditorCriteria.Matches(expected, native, 3, 5, out var accepted) && accepted.SequenceEqual(Padding), "post-job editor blocked");
                    check(!ListingEditorCriteria.Matches(target, native, 3, 5, out _), "selected job change passed as padding");
                    check(ListingEditorCriteria.TryReadFlags(expected.Criteria, 24, out var serverFlags) && Padding.All(i => serverFlags[i] == 0),
                        "native defaults leaked into expected listing");
                    var fresh = Target(serverFlags) with { RemainingSeconds = 3598 };
                    check(edit.MatchesResult(target, fresh), "fresh 17/18-slot listing rejected after job edit");
                }
            }
        });
        test("all shared edits validate the friendly editor without changing their server expectations", () => {
            var target = Target(CapturedListing());
            var expected = new[] {
                new ListingDescriptionEdit(target, "새 소개말").Expected(target),
                new ListingObjectiveEdit(target, 1).Expected(target),
                new ListingBeginnerEdit(target, true).Expected(target),
                new ListingUniqueJobsEdit(target, true).Expected(target),
                new ListingPrivateEdit(target, true, "0123").Expected(target),
                new ListingCompletionEdit(target, true, 0).Expected(target),
                new ListingItemLevelEdit(target, true, "555").Expected(target),
                new ListingRuleEdit(target, ListingRuleKind.Unrestricted, 1).Expected(target),
                new ListingRuleEdit(target, ListingRuleKind.MinimumItemLevel, 1).Expected(target),
                new ListingRuleEdit(target, ListingRuleKind.SilenceEcho, 1).Expected(target),
                new ListingRuleEdit(target, ListingRuleKind.Loot, 1).Expected(target),
            };
            foreach (var after in expected)
                check(ListingEditorCriteria.Matches(after, EditorCriteria(after), 3, 5, out var accepted) && accepted.SequenceEqual(Padding) &&
                    ListingJobEdit.SameSlots(target.Slots, after.Slots), "shared edit rejected or changed slot expectations");
        });
    }
}
