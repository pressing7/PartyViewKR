namespace PartyViewKR;

// Chat does not depend on the AFK recruitment watcher. Preserve the known
// character/home world across temporary PlayerState gaps, and clear on logout.
internal sealed class GameChatSession
{
    internal ulong SelfId { get; private set; }
    internal string HomeWorld { get; private set; } = "";

    internal void Observe(bool loggedIn, bool loaded, ulong self, Func<string> readHomeWorld)
    {
        if (!loggedIn) { SelfId = 0; HomeWorld = ""; return; }
        if (!loaded) return;
        if (self != SelfId) { SelfId = self; HomeWorld = ""; }
        if (SelfId != 0 && HomeWorld.Length == 0) HomeWorld = readHomeWorld();
    }
}
