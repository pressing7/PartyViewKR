using PartyViewKR;

static class ListingSlotsTests
{
    internal static void Run(Action<string, Action> test, Action<bool, string> check)
    {
        static string JobName(uint id) => $"직업 #{id}";
        test("recruitment jobs preserve positions and survive later source changes", () => {
            byte[] jobs = [21, 0, 24, 0];
            ulong[] members = [100, 0, 200, 0];
            ulong[] flags = [1, 2, 4, 8];
            var snapshot = ListingSlots.Read(true, 4, 2, 1, false, jobs, members, flags, JobName);
            jobs[0] = 19; members[0] = 999; flags[1] = 16;
            check(snapshot.Readable && snapshot.CountsAgree, "valid detail rejected");
            check(snapshot.Slots.Length == 4 && snapshot.Slots[2].Position == 3, "empty slots collapsed");
            check(snapshot.Slots[0].JobId == 21 && snapshot.Slots[0].Job == "직업 #21", "registered job replaced");
            check(snapshot.Slots[0].MemberContentId == 100 && snapshot.Slots[1].SlotFlags == 2, "native buffer retained");
            check(!snapshot.Slots[1].HasMember && snapshot.Slots[2].HasMember, "slot occupancy lost");
        });
        test("recruitment capacity follows the listing through 48 slots", () => {
            foreach (var size in new[] { 1, 4, 8, 10, 24, 48 })
            {
                var jobs = new byte[48]; var members = new ulong[48]; var flags = new ulong[48];
                Array.Fill(flags, 0xFFFFFFFFFFFFFFFEul, 0, size);
                jobs[size - 1] = 21; members[size - 1] = 100;
                // Single-party data past TotalSlots must never become visible.
                if (size < 48) { jobs[size] = 19; members[size] = 200; }
                var parties = size == 48 ? 6 : size == 24 ? 3 : 1;
                var snapshot = ListingSlots.Read(true, size, 1, parties, parties > 1, jobs, members, flags, JobName);
                check(snapshot.Readable && snapshot.Slots.Length == size && snapshot.CountsAgree, "wrong slot count");
                check(snapshot.Slots[^1].Position == size && snapshot.Slots[^1].JobId == 21, "last slot lost");
                check(snapshot.NumberOfParties == parties && snapshot.IsAlliance == (parties > 1), "group metadata lost");
            }
        });
        test("invalid or incomplete recruitment arrays are not displayed", () => {
            byte[] jobs = [21, 0, 0, 0]; ulong[] members = [100, 0, 0, 0]; ulong[] flags = [1, 2, 4, 8];
            foreach (var (total, filled) in new[] { (0, 0), (49, 1), (4, -1), (4, 5) })
                check(!ListingSlots.Read(true, total, filled, 1, false, jobs, members, flags, JobName).Readable, "invalid count accepted");
            check(!ListingSlots.Read(true, 4, 1, 1, false, jobs.AsSpan(0, 3), members, flags, JobName).Readable, "short jobs");
            check(!ListingSlots.Read(true, 4, 1, 1, false, jobs, members.AsSpan(0, 3), flags, JobName).Readable, "short members");
            check(!ListingSlots.Read(true, 4, 1, 1, false, jobs, members, flags.AsSpan(0, 3), JobName).Readable, "short flags");
        });
        test("unmatched detail never supplies jobs to the current listing", () => {
            var resolved = 0;
            var snapshot = ListingSlots.Read(false, 1, 1, 1, false, new byte[] { 21 },
                new ulong[] { 100 }, new ulong[] { 1 }, id => { resolved++; return JobName(id); });
            check(!snapshot.Readable && snapshot.Slots.Length == 0 && resolved == 0, "unmatched detail used");
        });
        test("missing and unknown recruitment jobs retain their source values", () => {
            var snapshot = ListingSlots.Read(true, 3, 2, 1, false, new byte[] { 0, 250, 0 },
                new ulong[] { 100, 200, 0 }, new ulong[] { 0, 0, 2 }, JobName);
            check(snapshot.CountsAgree && snapshot.Slots[0].HasMember, "member with missing job became empty");
            check(snapshot.Slots[0].JobId == 0 && snapshot.Slots[0].Job == "", "missing job guessed");
            check(snapshot.Slots[1].JobId == 250 && snapshot.Slots[1].Job == "직업 #250", "unknown job changed");
            check(!snapshot.Slots[2].HasMember, "empty slot became occupied");
            check(!snapshot.Diagnostic.Contains("100") && !snapshot.Diagnostic.Contains("200"), "member IDs leaked into diagnostics");
        });
        test("inconsistent member count remains visible as incomplete data", () => {
            var snapshot = ListingSlots.Read(true, 4, 2, 1, false, new byte[] { 21, 0, 0, 0 },
                new ulong[] { 100, 0, 0, 0 }, new ulong[4], JobName);
            check(snapshot.Readable && !snapshot.CountsAgree && snapshot.ObservedMembers == 1, "inconsistency hidden");
        });
    }
}
