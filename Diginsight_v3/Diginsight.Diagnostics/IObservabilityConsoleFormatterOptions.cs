namespace Diginsight.Diagnostics;

public interface IObservabilityConsoleFormatterOptions
{
    string? TimestampFormat { get; }

    bool UseUtcTimestamp { get; }

    string? TimestampCulture { get; }

    int MaxCategoryLength { get; }
}
