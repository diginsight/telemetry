using System.Collections.Frozen;

namespace Diginsight.Diagnostics;

/// <summary>
/// Represents configuration options for options-based metric recording enrichment.
/// </summary>
public sealed class OptionsBasedMetricRecordingEnricherOptions
    : IOptionsBasedMetricRecordingEnricherOptions
{
    private readonly bool frozen;

    /// <summary>
    /// Gets the activity tag names to copy to metric recordings.
    /// </summary>
    public ICollection<string> MetricTags { get; }

    IReadOnlyCollection<string> IOptionsBasedMetricRecordingEnricherOptions.MetricTags => (IReadOnlyCollection<string>)MetricTags;

    /// <summary>
    /// Initializes a new instance of the <see cref="OptionsBasedMetricRecordingEnricherOptions" /> class with default configuration.
    /// </summary>
    public OptionsBasedMetricRecordingEnricherOptions()
        : this(false, new List<string>()) { }

    private OptionsBasedMetricRecordingEnricherOptions(
        bool frozen, ICollection<string> metricTags
    )
    {
        this.frozen = frozen;
        MetricTags = metricTags;
    }

    /// <summary>
    /// Creates an immutable copy of this options instance.
    /// </summary>
    /// <returns>The frozen options instance.</returns>
    public OptionsBasedMetricRecordingEnricherOptions Freeze()
    {
        if (frozen)
            return this;

        return new OptionsBasedMetricRecordingEnricherOptions(true, MetricTags.ToFrozenSet());
    }
}
