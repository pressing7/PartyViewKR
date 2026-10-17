namespace PartyViewKR;

// Asset-free scaffolding: the eventual per-bot application emoji registry can
// supply IDs here. No uploads, network calls, or invented job images are needed
// to show the roster. Missing images always have an explicit state fallback.
internal sealed class ListingDiscordIcons(IReadOnlyDictionary<string, ulong>? applicationEmojiIds = null)
{
    internal string ForSlot(WebSlot slot)
    {
        var (key, fallback) = slot.HasMember ? (slot.JobId > 0 ? "job_" + slot.JobId : "member", "🟦") :
            !slot.RulesComplete ? ("unknown", "⬛") :
            slot.Anyone ? ("anyone", "⬜") : ("restricted", "🟨");
        return applicationEmojiIds != null && applicationEmojiIds.TryGetValue(key, out var id) && id != 0
            ? $"<:pvkr_{key}:{id}>" : fallback;
    }

    // Only the fallback symbols in use need explaining once real icons exist.
    internal string Legend(IEnumerable<WebSlot> slots)
    {
        var used = slots.Select(ForSlot).ToHashSet();
        (string Icon, string Label)[] states = [("🟦", "참가"), ("⬜", "누구나 가능"), ("🟨", "직업 제한"), ("⬛", "확인 불가")];
        return string.Join(" · ", states.Where(s => used.Contains(s.Icon)).Select(s => s.Icon + " " + s.Label));
    }
}
