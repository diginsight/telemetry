namespace Diginsight.Diagnostics;

/// <summary>
/// Represents configuration options for the Diginsight debug logger.
/// </summary>
public sealed class DiginsightDebugLoggerOptions : IDiginsightDebugLoggerOptions
{
    private TimeZoneInfo? timeZone = TimeZoneInfo.Utc;

    /// <inheritdoc />
    public string? Pattern
    {
        get;
        set => field = value.HardTrim();
    }

    /// <summary>
    /// Gets the time zone identifier used to render timestamps.
    /// </summary>
    public string? TimeZone
    {
        get => timeZone?.Id;
        set => timeZone = value is null ? null : TimeZoneInfo.FindSystemTimeZoneById(value);
    }

    TimeZoneInfo? IDiginsightDebugLoggerOptions.TimeZone => timeZone;
}
