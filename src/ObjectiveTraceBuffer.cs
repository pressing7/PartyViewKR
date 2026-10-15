namespace PartyViewKR;

// Bounded, in-memory observations. Starting explicitly replaces the previous
// capture; stopping preserves it for copying.
internal sealed class ObjectiveTraceBuffer
{
    internal const int MaxEntries = 1024;
    internal const long DurationMs = 120_000;
    private readonly object gate = new();
    private readonly List<string> entries = [];
    private bool active;
    private long started;
    private string reason = "not_started";
    internal bool Active { get { lock (gate) return active; } }
    internal int Count { get { lock (gate) return entries.Count; } }
    internal string Reason { get { lock (gate) return reason; } }

    internal void Start(long now)
    {
        lock (gate) { entries.Clear(); started = now; active = true; reason = "recording"; }
    }

    internal void Add(long now, string line)
    {
        lock (gate)
        {
            if (!active) return;
            if (now - started >= DurationMs) { StopCore("timeout"); return; }
            if (entries.Count >= MaxEntries) { StopCore("entry_limit"); return; }
            entries.Add($"+{Math.Max(0, now - started)}ms {line}");
        }
    }

    internal void Tick(long now)
    {
        lock (gate) if (active && now - started >= DurationMs) StopCore("timeout");
    }

    internal void Stop(string stopReason)
    {
        lock (gate) if (active) StopCore(stopReason);
    }

    private void StopCore(string stopReason) { active = false; reason = stopReason; }

    internal string Snapshot()
    {
        lock (gate) return $"objectiveTraceActive={active}; objectiveTraceReason={reason}; objectiveTraceEntries={entries.Count}\n" +
            string.Join('\n', entries);
    }
}
