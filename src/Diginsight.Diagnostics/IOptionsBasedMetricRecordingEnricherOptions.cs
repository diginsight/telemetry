namespace Diginsight.Diagnostics;

/// <summary>
/// Represents options for options-based metric recording enrichment.
/// </summary>
public interface IOptionsBasedMetricRecordingEnricherOptions
{
    /// <summary>
    /// Gets the activity tag names to copy to metric recordings.
    /// </summary>
    IReadOnlyCollection<string> MetricTags { get; }
}
