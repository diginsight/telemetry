namespace Diginsight.Diagnostics;

public interface IDiginsightActivitiesMetricOptions
{
    bool Record { get; }
    string MeterName { get; }
    string MetricName { get; }
    string? MetricDescription { get; }
}
