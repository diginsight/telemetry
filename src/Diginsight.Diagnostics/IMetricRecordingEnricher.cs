using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Diginsight.Diagnostics;

/// <summary>
/// Represents an interface for enriching metric recordings with tags.
/// </summary>
public interface IMetricRecordingEnricher
{
    /// <summary>
    /// Extracts tags for a metric recording from the specified activity and instrument.
    /// </summary>
    /// <param name="activity">The activity being recorded.</param>
    /// <param name="instrument">The metric instrument.</param>
    /// <returns>The extracted tags.</returns>
    Tags ExtractTags(Activity activity, Instrument instrument);
}
