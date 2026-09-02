using System.Collections.Frozen;

namespace Diginsight.Diagnostics;

/// <summary>
/// Represents configuration options for options-based metric recording filtering.
/// </summary>
public sealed class OptionsBasedMetricRecordingFilterOptions
    : IOptionsBasedMetricRecordingFilterOptions
{
    private readonly bool frozen;

    /// <summary>
    /// Gets the activity name patterns mapped to metric recording decisions.
    /// </summary>
    public IDictionary<string, bool> ActivityNames { get; }

    IReadOnlyDictionary<string, bool> IOptionsBasedMetricRecordingFilterOptions.ActivityNames => (IReadOnlyDictionary<string, bool>)ActivityNames;

    /// <summary>
    /// Initializes a new instance of the <see cref="OptionsBasedMetricRecordingFilterOptions" /> class with default configuration.
    /// </summary>
    public OptionsBasedMetricRecordingFilterOptions()
        : this(false, new Dictionary<string, bool>()) { }

    private OptionsBasedMetricRecordingFilterOptions(
        bool frozen, IDictionary<string, bool> activityNames
    )
    {
        this.frozen = frozen;
        ActivityNames = activityNames;
    }

    /// <summary>
    /// Creates an immutable copy of this options instance.
    /// </summary>
    /// <returns>The frozen options instance.</returns>
    public OptionsBasedMetricRecordingFilterOptions Freeze()
    {
        if (frozen)
            return this;

        return new OptionsBasedMetricRecordingFilterOptions(true, ActivityNames.ToFrozenDictionary());
    }
}
