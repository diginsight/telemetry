namespace Diginsight.Diagnostics;

public interface IOptionsBasedMetricRecordingEnricherOptions
{
    IReadOnlyCollection<string> MetricTags { get; }
}
