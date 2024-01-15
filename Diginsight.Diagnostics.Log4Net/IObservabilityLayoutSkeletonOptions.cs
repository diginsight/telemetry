namespace Diginsight.Diagnostics.Log4Net;

public interface IObservabilityLayoutSkeletonOptions
{
    bool UseUtcTimestamp { get; }
    string? Pattern { get; }
}
