using Microsoft.Extensions.Logging.Console;

namespace Diginsight.Diagnostics;

public sealed class ObservabilityConsoleFormatterOptions : ConsoleFormatterOptions, IObservabilityConsoleFormatterOptions
{
    public string? TimestampCulture { get; set; }

    public int MaxCategoryLength { get; set; } = 50;

    public ObservabilityConsoleFormatterOptions()
    {
        UseUtcTimestamp = true;
    }
}
