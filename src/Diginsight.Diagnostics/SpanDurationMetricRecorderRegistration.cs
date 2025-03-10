﻿using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Diginsight.Diagnostics;

public class SpanDurationMetricRecorderRegistration : IActivityListenerRegistration
{
    private readonly IDiginsightActivitiesOptions? activitiesOptions;

    public IActivityListenerLogic Logic { get; }

    protected SpanDurationMetricRecorderRegistration(SpanDurationMetricRecorder recorder)
    {
        Logic = recorder;
    }

    public SpanDurationMetricRecorderRegistration(
        SpanDurationMetricRecorder recorder,
        IOptions<DiginsightActivitiesOptions> activitiesOptions
    )
        : this(recorder)
    {
        this.activitiesOptions = activitiesOptions.Value.Freeze();
    }

    public virtual bool ShouldListenTo(ActivitySource activitySource)
    {
        if (activitiesOptions is null)
        {
            throw new NotSupportedException($"{nameof(SpanDurationMetricRecorderRegistration)} instance was created without {nameof(activitiesOptions)}");
        }

        string activitySourceName = activitySource.Name;
        return activitiesOptions.ActivitySources
            .Where(x => ActivityUtils.NameMatchesPattern(activitySourceName, x.Key))
            .All(static x => x.Value);
    }
}
