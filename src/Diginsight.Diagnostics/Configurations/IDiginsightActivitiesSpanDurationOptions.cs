namespace Diginsight.Diagnostics;

public interface IDiginsightActivitiesSpanDurationOptions
{
    bool Record { get; }
    string MeterName { get; }
    string MetricName { get; }
    string? MetricDescription { get; }
}
