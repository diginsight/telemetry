namespace Diginsight.Diagnostics;

public interface IDiginsightDebugLoggerOptions
{
    string? Pattern { get; }
    TimeZoneInfo? TimeZone { get; }
}
