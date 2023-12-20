using System.Globalization;

namespace Diginsight.Diagnostics;

public interface IObservabilityTextWriterOptions
{
    bool UseUtcTimestamp { get; }

    string? TimestampFormat { get; }

    CultureInfo TimestampCulture { get; }

    int CategoryLength { get; }

    int MaxMessageLength { get; }

    int MaxLineLength { get; }

    int MaxIndentedDepth { get; }
}
