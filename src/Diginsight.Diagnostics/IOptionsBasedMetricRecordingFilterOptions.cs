namespace Diginsight.Diagnostics;

public interface IOptionsBasedMetricRecordingFilterOptions
{
    string? MetricName { get; set; }
    IDictionary<string, bool> ActivityNames { get; set; }
}
