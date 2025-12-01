namespace Diginsight.Diagnostics;

public sealed class DiginsightDebugLoggerOptions : IDiginsightDebugLoggerOptions
{
    private TimeZoneInfo? timeZone = TimeZoneInfo.Utc;

    public string? Pattern
    {
        get;
        set => field = value.HardTrim();
    }

    public string? TimeZone
    {
        get => timeZone?.Id;
        set => timeZone = value is null ? null : TimeZoneInfo.FindSystemTimeZoneById(value);
    }

    TimeZoneInfo? IDiginsightDebugLoggerOptions.TimeZone => timeZone;
}
