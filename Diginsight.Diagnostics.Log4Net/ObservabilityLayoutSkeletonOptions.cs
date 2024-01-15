using Diginsight.Diagnostics.TextWriting;

namespace Diginsight.Diagnostics.Log4Net;

public sealed class ObservabilityLayoutSkeletonOptions : IObservabilityTextWritingOptions
{
    private string? pattern;
    private LineDescriptor? lineDescriptor;

    public bool UseUtcTimestamp { get; set; } = true;

    public string? Pattern
    {
        get => pattern;
        set
        {
            value = value.HardTrim();
            if (pattern == value)
            {
                return;
            }

            pattern = value;
            lineDescriptor = null;
        }
    }

    public LineDescriptor GetLineDescriptor(int? width)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Must be positive");
        }

        return lineDescriptor ??= LineDescriptor.ParseFull(Pattern);
    }
}
