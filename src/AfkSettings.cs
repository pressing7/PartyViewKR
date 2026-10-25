namespace PartyViewKR;

internal sealed record AfkSettings(bool PeriodicEnabled = false, int PeriodMinutes = 1,
    bool RetryEnabled = true, int RetrySeconds = 5)
{
    internal AfkSettings Normalize() => this with {
        PeriodMinutes = Math.Clamp(PeriodMinutes, 1, 55), RetrySeconds = Math.Clamp(RetrySeconds, 1, 3600)
    };
    internal long PeriodMilliseconds => PeriodMinutes * 60000L;
    internal long RetryMilliseconds => RetrySeconds * 1000L;
}
