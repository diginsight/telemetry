namespace Diginsight.Diagnostics;

public interface IDiginsightConsoleFormatterOptions
{
    string? TimestampFormat { get; }
    bool UseUtcTimestamp { get; }

    string? Pattern { get; }
    IReadOnlyDictionary<int, string?> Patterns { get; }

    int TotalWidth { get; }

    bool UseColor { get; }
}
