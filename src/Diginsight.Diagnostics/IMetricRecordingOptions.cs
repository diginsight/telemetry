namespace Diginsight.Diagnostics;

/// <summary>
/// Represents metric recording options.
/// </summary>
public interface IMetricRecordingOptions
{
    /// <summary>
    /// Gets a value indicating whether metrics are recorded.
    /// </summary>
    bool Record { get; }
    /// <summary>
    /// Gets the meter name.
    /// </summary>
    string MeterName { get; }
    /// <summary>
    /// Gets the metric name.
    /// </summary>
    string MetricName { get; }
    /// <summary>
    /// Gets the metric description.
    /// </summary>
    string? MetricDescription { get; }
}
