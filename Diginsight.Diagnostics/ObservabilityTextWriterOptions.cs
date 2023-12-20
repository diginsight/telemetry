using Microsoft.Extensions.Logging.Console;
using System.Globalization;

namespace Diginsight.Diagnostics;

public sealed class ObservabilityTextWriterOptions : ConsoleFormatterOptions, IObservabilityTextWriterOptions
{
    private CultureInfo? timestampCultureInfo;

    public string? TimestampCulture { get; set; }

    CultureInfo IObservabilityTextWriterOptions.TimestampCulture => TimestampCultureInfo;

    internal CultureInfo TimestampCultureInfo
    {
        private get => timestampCultureInfo ?? throw new InvalidOperationException("TimestampCultureInfo not set yet");
        set => timestampCultureInfo = value;
    }

    public int CategoryLength { get; set; } = 40;

    public int MaxMessageLength { get; set; }

    public int MaxLineLength { get; set; }

    public int MaxIndentedDepth { get; set; } = 10;

    public ObservabilityTextWriterOptions()
    {
        UseUtcTimestamp = true;
    }
}
