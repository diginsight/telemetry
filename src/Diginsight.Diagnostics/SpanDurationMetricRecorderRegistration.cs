using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Diginsight.Diagnostics;

/// <summary>
/// Represents an activity listener registration for span duration metric recording.
/// </summary>
public class SpanDurationMetricRecorderRegistration : IActivityListenerRegistration
{
    private readonly IDiginsightActivitiesOptions? activitiesOptions;

    /// <inheritdoc />
    public IActivityListenerLogic Logic { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpanDurationMetricRecorderRegistration" /> class.
    /// </summary>
    /// <param name="recorder">The span duration metric recorder.</param>
    protected SpanDurationMetricRecorderRegistration(SpanDurationMetricRecorder recorder)
    {
        Logic = recorder;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpanDurationMetricRecorderRegistration" /> class.
    /// </summary>
    /// <param name="recorder">The span duration metric recorder.</param>
    /// <param name="activitiesOptions">The activity options.</param>
    public SpanDurationMetricRecorderRegistration(
        SpanDurationMetricRecorder recorder,
        IOptions<DiginsightActivitiesOptions> activitiesOptions
    )
        : this(recorder)
    {
        this.activitiesOptions = activitiesOptions.Value.Freeze();
    }

    /// <inheritdoc />
    public virtual bool ShouldListenTo(ActivitySource activitySource)
    {
        if (activitiesOptions is null)
        {
            throw new NotSupportedException($"{nameof(SpanDurationMetricRecorderRegistration)} instance was created without {nameof(activitiesOptions)}");
        }

        string activitySourceName = activitySource.Name;
        IEnumerable<bool> matches = activitiesOptions.ActivitySources
            .Where(x => ActivityUtils.NameMatchesPattern(activitySourceName, x.Key))
            .Select(static x => x.Value);

        bool result = false;
        foreach (bool match in matches)
        {
            if (!match)
                return false;
            result = true;
        }
        return result;
    }
}
