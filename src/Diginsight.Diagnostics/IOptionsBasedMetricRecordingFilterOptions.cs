namespace Diginsight.Diagnostics;

/// <summary>
/// Represents options for options-based metric recording filtering.
/// </summary>
public interface IOptionsBasedMetricRecordingFilterOptions
{
    /// <summary>
    /// Gets the activity name patterns mapped to metric recording decisions.
    /// </summary>
    IReadOnlyDictionary<string, bool> ActivityNames { get; }
}
