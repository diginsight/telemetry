using Microsoft.Extensions.Logging.Console;

namespace Diginsight.Diagnostics;

public sealed class ObservabilityConsoleFormatterOptions : ConsoleFormatterOptions, IObservabilityConsoleFormatterOptions
{
    public string? TimestampCulture { get; set; }

    public int CategoryLength { get; set; } = 40;

    public int MaxMessageLength { get; set; }

    public int MaxLineLength { get; set; }

    public int MaxIndentedDepth { get; set; } = 10;

    public ObservabilityConsoleFormatterOptions()
    {
        UseUtcTimestamp = true;
    }
}
