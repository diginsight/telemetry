using System.Collections.Immutable;

namespace Diginsight.Diagnostics;

public sealed class OptionsBasedMetricRecordingEnricherOptions
    : IOptionsBasedMetricRecordingEnricherOptions
{
    public string? MetricName { get; set; } = string.Empty;
    public ICollection<string> MetricTags { get; set; } = new List<string>();
}


//public sealed class OptionsBasedMetricRecordingEnricherOptions
//    : IOptionsBasedMetricRecordingEnricherOptions
//{
//    private readonly bool frozen;

//    public string? MetricName { get; set; } = string.Empty;

//    public ICollection<string> MetricTags { get; }

//    IReadOnlyCollection<string> IOptionsBasedMetricRecordingEnricherOptions.MetricTags => (IReadOnlyCollection<string>)MetricTags;

//    public OptionsBasedMetricRecordingEnricherOptions()
//        : this(false, new List<string>()) { }

//    private OptionsBasedMetricRecordingEnricherOptions(
//        bool frozen, ICollection<string> metricTags
//    )
//    {
//        this.frozen = frozen;
//        MetricTags = metricTags;
//    }

//    public OptionsBasedMetricRecordingEnricherOptions Freeze()
//    {
//        if (frozen)
//            return this;

//        return new OptionsBasedMetricRecordingEnricherOptions(true, MetricTags.ToImmutableList());
//    }
//}
