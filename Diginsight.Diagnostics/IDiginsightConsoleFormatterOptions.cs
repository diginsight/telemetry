namespace Diginsight.Diagnostics;

public interface IDiginsightConsoleFormatterOptions
{
    string? TimestampFormat { get; }
    bool UseUtcTimestamp { get; }
    string? Pattern { get; }
    IReadOnlyDictionary<string, string?> Patterns { get; }
}
