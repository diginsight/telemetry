namespace Diginsight.Diagnostics;

public interface IOptionsBasedMetricRecordingEnricherOptions
{
    string? MetricName { get; set; }
    ICollection<string> MetricTags { get; set; }
}
