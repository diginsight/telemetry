namespace Diginsight.Diagnostics;

public interface IDefaultMetricRecordingEnricherOptions
{
    IReadOnlyCollection<string> MetricTags { get; }
}
