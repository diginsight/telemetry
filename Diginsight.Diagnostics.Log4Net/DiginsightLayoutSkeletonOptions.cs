namespace Diginsight.Diagnostics.Log4Net;

public sealed class DiginsightLayoutSkeletonOptions : IDiginsightLayoutSkeletonOptions
{
    private string? pattern;

    public bool UseUtcTimestamp { get; set; } = true;

    public string? Pattern
    {
        get => pattern;
        set => pattern = value.HardTrim();
    }
}
