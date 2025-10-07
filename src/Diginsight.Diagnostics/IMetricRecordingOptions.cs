namespace Diginsight.Diagnostics;

public interface IMetricRecordingOptions
{
    bool Record { get; }
    string MeterName { get; }
    string MetricName { get; }
    string? MetricDescription { get; }
}
