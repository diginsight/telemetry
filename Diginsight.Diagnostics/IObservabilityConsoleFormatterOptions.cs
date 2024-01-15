namespace Diginsight.Diagnostics;

public interface IObservabilityConsoleFormatterOptions
{
    string? TimestampFormat { get; }
    bool UseUtcTimestamp { get; }
    string? Pattern { get; }
    IReadOnlyDictionary<string, string?> Patterns { get; }
}
