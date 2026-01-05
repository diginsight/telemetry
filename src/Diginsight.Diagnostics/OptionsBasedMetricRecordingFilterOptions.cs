using System.Collections.Immutable;

namespace Diginsight.Diagnostics;

public sealed class OptionsBasedMetricRecordingFilterOptions
    : IOptionsBasedMetricRecordingFilterOptions
{
    public string? MetricName { get; set; } = string.Empty;
    public IDictionary<string, bool> ActivityNames { get; set; } = new Dictionary<string, bool>();
}

//public sealed class OptionsBasedMetricRecordingFilterOptions
//    : IOptionsBasedMetricRecordingFilterOptions
//{
//    private readonly bool frozen;

//    public IDictionary<string, bool> ActivityNames { get; }

//    IReadOnlyDictionary<string, bool> IOptionsBasedMetricRecordingFilterOptions.ActivityNames => (IReadOnlyDictionary<string, bool>)ActivityNames;

//    public OptionsBasedMetricRecordingFilterOptions()
//        : this(false, new Dictionary<string, bool>()) { }

//    private OptionsBasedMetricRecordingFilterOptions(
//        bool frozen, IDictionary<string, bool> activityNames
//    )
//    {
//        this.frozen = frozen;
//        ActivityNames = activityNames;
//    }

//    public OptionsBasedMetricRecordingFilterOptions Freeze()
//    {
//        if (frozen)
//            return this;

//        return new OptionsBasedMetricRecordingFilterOptions(true, ActivityNames.ToImmutableDictionary());
//    }
//}
