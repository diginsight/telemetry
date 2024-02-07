namespace Diginsight.Diagnostics.Log4Net;

public interface IDiginsightLayoutSkeletonOptions
{
    bool UseUtcTimestamp { get; }
    string? Pattern { get; }
}
