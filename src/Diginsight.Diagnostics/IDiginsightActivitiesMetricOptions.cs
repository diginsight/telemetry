namespace Diginsight.Diagnostics;

public interface IDiginsightActivitiesMetricOptions
{
    bool RecordSpanDurations { get; }

    string MeterName { get; }
    string MetricName { get; }
    string? MetricUnit { get; }
    string? MetricDescription { get; }
}
