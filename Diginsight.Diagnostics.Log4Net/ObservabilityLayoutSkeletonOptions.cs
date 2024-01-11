using Diginsight.Diagnostics.TextWriting;

namespace Diginsight.Diagnostics.Log4Net;

public sealed class ObservabilityLayoutSkeletonOptions : IObservabilityTextWritingOptions
{
    public bool UseUtcTimestamp { get; set; } = true;

    public string? Pattern { get; set; }

    public LineDescriptor GetLineDescriptor(int? width)
    {
        return Pattern is not null ? LineDescriptor.ParseFull(Pattern) : default;
    }
}
