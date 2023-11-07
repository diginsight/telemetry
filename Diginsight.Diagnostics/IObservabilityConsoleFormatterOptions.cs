namespace Diginsight.Diagnostics;

public interface IObservabilityConsoleFormatterOptions
{
    bool UseUtcTimestamp { get; }

    string? TimestampFormat { get; }

    string? TimestampCulture { get; }

    int MaxCategoryLength { get; }
}
