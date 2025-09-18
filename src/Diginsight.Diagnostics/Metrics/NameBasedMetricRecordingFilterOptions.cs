using System.Collections.Immutable;

namespace Diginsight.Diagnostics;

public sealed class NameBasedMetricRecordingFilterOptions
    : INameBasedMetricRecordingFilterOptions
{
    private readonly bool frozen;

    public IDictionary<string, bool> ActivityNames { get; }

    IReadOnlyDictionary<string, bool> INameBasedMetricRecordingFilterOptions.ActivityNames => (IReadOnlyDictionary<string, bool>)ActivityNames;

    public NameBasedMetricRecordingFilterOptions()
        : this(false, new Dictionary<string, bool>()) { }

    private NameBasedMetricRecordingFilterOptions(
        bool frozen, IDictionary<string, bool> activityNames
    )
    {
        this.frozen = frozen;
        ActivityNames = activityNames;
    }

    public NameBasedMetricRecordingFilterOptions Freeze()
    {
        if (frozen)
            return this;

        return new NameBasedMetricRecordingFilterOptions(true, ActivityNames.ToImmutableDictionary());
    }
}
