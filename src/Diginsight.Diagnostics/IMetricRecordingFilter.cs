using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Diginsight.Diagnostics;

/// <summary>
/// Represents an interface for filtering metric recording.
/// </summary>
public interface IMetricRecordingFilter
{
    /// <summary>
    /// Determines whether a metric should be recorded for the specified activity and instrument.
    /// </summary>
    /// <param name="activity">The activity being recorded.</param>
    /// <param name="instrument">The metric instrument.</param>
    /// <returns><c>true</c> if the metric should be recorded; <c>false</c> if it should not be recorded; or <c>null</c> if the filter does not decide, eventually falling back to the default behavior.</returns>
    bool? ShouldRecord(Activity activity, Instrument instrument);
}
