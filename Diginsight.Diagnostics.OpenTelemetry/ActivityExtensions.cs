using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class ActivityExtensions
{
    public static void SetCustomDurationMetric(this Activity activity, Histogram<long> metric, params Tag[] tags)
    {
        activity.SetCustomDurationMetric((object)metric, tags);
    }

    public static void SetCustomDurationMetric(this Activity activity, Histogram<double> metric, params Tag[] tags)
    {
        activity.SetCustomDurationMetric((object)metric, tags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SetCustomDurationMetric(this Activity activity, object metric, params Tag[] tags)
    {
        if (activity is null)
        {
            throw new ArgumentNullException(nameof(activity));
        }

        activity.SetCustomProperty(ActivityCustomPropertyNames.CustomDurationMetric, metric);
        activity.SetCustomProperty(ActivityCustomPropertyNames.CustomDurationMetricTags, tags);
    }

    public static void AddTagsToCustomDurationMetric(this Activity activity, params Tag[] tags)
    {
        if (activity is null)
        {
            throw new ArgumentNullException(nameof(activity));
        }

        if (activity.GetCustomProperty(ActivityCustomPropertyNames.CustomDurationMetric) is not (Histogram<double> or Histogram<long>))
        {
            throw new ArgumentException("Activity has no associated custom duration metric");
        }

        Tag[] allTags = tags
            .Concat(activity.GetCustomDurationMetricTags())
#if NET6_0_OR_GREATER
            .DistinctBy(static x => x.Key)
#else
            .GroupBy(static x => x.Key, static (_, xs) => xs.First())
#endif
            .ToArray();
        activity.SetCustomProperty(ActivityCustomPropertyNames.CustomDurationMetricTags, allTags);
    }

    internal static Tag[] GetCustomDurationMetricTags(this Activity activity)
    {
        return activity.GetCustomProperty(ActivityCustomPropertyNames.CustomDurationMetricTags) switch
        {
            Tag[] tags => tags,
            null => Array.Empty<Tag>(),
            _ => throw new InvalidOperationException("Invalid custom duration metric tags in activity"),
        };
    }
}
