using System.Collections.Immutable;

namespace Diginsight.Diagnostics;

public sealed class DefaultMetricRecordingEnricherOptions
    : IDefaultMetricRecordingEnricherOptions
{
    private readonly bool frozen;

    public ICollection<string> MetricTags { get; }

    IReadOnlyCollection<string> IDefaultMetricRecordingEnricherOptions.MetricTags => (IReadOnlyCollection<string>)MetricTags;

    public DefaultMetricRecordingEnricherOptions()
        : this(false, new List<string>()) { }

    private DefaultMetricRecordingEnricherOptions(
        bool frozen, ICollection<string> metricTags
    )
    {
        this.frozen = frozen;
        MetricTags = metricTags;
    }

    public DefaultMetricRecordingEnricherOptions Freeze()
    {
        if (frozen)
            return this;

        return new DefaultMetricRecordingEnricherOptions(true, MetricTags.ToImmutableList());
    }
}
