namespace Diginsight.Diagnostics;

public interface IDiginsightConsoleFormatterOptions
{
    string? TimestampFormat { get; }
    TimeZoneInfo? TimeZone { get; }

    string? Pattern { get; }
    IReadOnlyDictionary<int, string?> Patterns { get; }

    int TotalWidth { get; }

    bool UseColor { get; }
}
