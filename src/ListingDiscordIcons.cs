using System.Collections.Concurrent;

namespace PartyViewKR;

// A per-connection registry: one user's bot IDs never reach another bot. The
// background uploader and interaction renderer can access it concurrently.
internal sealed class ListingDiscordIcons(IReadOnlyDictionary<string, ulong>? applicationEmojiIds = null)
{
    private readonly ConcurrentDictionary<string, ulong> ids = new(applicationEmojiIds ?? new Dictionary<string, ulong>());
    internal int Count => ids.Count;
    internal void Register(string key, ulong id) { if (id != 0) ids[key] = id; }
    internal string Private => Resolve("private", "🔒");
    internal string Beginner => Resolve("beginner", "🌱");

    internal string ForSlot(WebSlot slot)
    {
        var fallback = slot.HasMember ? "🟦" : !slot.RulesComplete ? "⬛" : slot.Anyone ? "⬜" : "🟨";
        var key = ConditionKey(slot);
        return key == null ? fallback : Resolve((slot.HasMember ? "checked_" : "") + key, fallback);
    }

    internal static string? ConditionKey(WebSlot slot)
    {
        if (!slot.RulesComplete) return null;
        if (slot.Anyone) return "anyone";
        var jobs = slot.AllowedJobIds.Distinct().ToArray();
        if (jobs.Length == 0 || jobs.Any(id => Role(id) == 0)) return null;
        if (jobs.Length == 1) return "job_" + jobs[0];
        // One or more jobs from each role is enough; do not require the whole
        // role. Only the full runtime catalog qualifies as "anyone".
        return jobs.Aggregate(0, (mask, id) => mask | Role(id)) switch {
            1 => "role_t", 2 => "role_h", 4 => "role_d",
            3 => "role_th", 5 => "role_td", 6 => "role_hd", 7 => "role_thd",
            _ => null
        };
    }
    private static int Role(uint id) => id switch {
        19 or 21 or 32 or 37 or 1 or 3 => 1,
        24 or 33 or 28 or 40 or 6 => 2,
        20 or 22 or 30 or 34 or 39 or 41 or 43 or 2 or 4 or 29 or
        23 or 31 or 38 or 5 or 25 or 27 or 35 or 42 or 36 or 7 or 26 => 4,
        _ => 0
    };
    private string Resolve(string key, string fallback) =>
        ids.TryGetValue(key, out var id) && id != 0
            ? $"<:{ListingDiscordEmojiAssets.NameFor(key)}:{id}>" : fallback;

    // Only the fallback symbols in use need explaining once real icons exist.
    internal string Legend(IEnumerable<WebSlot> slots)
    {
        var used = slots.Select(ForSlot).ToHashSet();
        (string Icon, string Label)[] states = [("🟦", "참가"), ("⬜", "누구나 가능"), ("🟨", "직업 제한"), ("⬛", "확인 불가")];
        return string.Join(" · ", states.Where(s => used.Contains(s.Icon)).Select(s => s.Icon + " " + s.Label));
    }
}
