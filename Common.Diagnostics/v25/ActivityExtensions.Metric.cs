using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Common
{
    public static partial class ActivityExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RecordDurationMetric(this Activity? activity, Histogram<long> durationMetric, params Tag[] tags)
        {
            activity.RecordDurationMetric((object)durationMetric, tags);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RecordDurationMetric(this Activity? activity, Histogram<double> durationMetric, params Tag[] tags)
        {
            activity.RecordDurationMetric((object)durationMetric, tags);
        }

        private static void RecordDurationMetric(this Activity? activity, object durationMetric, params Tag[] tags)
        {
            if (activity is null)
            {
                return;
            }

            activity.SetCustomProperty(ActivityCustomPropertyNames.DurationMetric, durationMetric);
            activity.SetCustomProperty(ActivityCustomPropertyNames.DurationMetricTags, tags);
        }

        public static void AddDurationMetricTags(this Activity? activity, params Tag[] tags)
        {
            if (activity is null)
            {
                return;
            }

            if (activity.GetCustomProperty(ActivityCustomPropertyNames.DurationMetric) is not (Histogram<double> or Histogram<long>))
            {
                throw new ArgumentException("Activity has no associated duration metric");
            }

            Tag[] allTags = tags
                .Concat(activity.GetDurationMetricTags())
#if NET6_0_OR_GREATER
                .DistinctBy(static x => x.Key)
#else
            .Distinct(TagKeyComparer.Instance)
#endif
                .ToArray();
            activity.SetCustomProperty(ActivityCustomPropertyNames.DurationMetricTags, allTags);
        }

#if !NET6_0_OR_GREATER
    private sealed class TagKeyComparer : IEqualityComparer<Tag>
    {
        public static readonly IEqualityComparer<Tag> Instance = new TagKeyComparer();

        private TagKeyComparer() { }

        public bool Equals(Tag x1, Tag x2)
        {
            return string.Equals(x1.Key, x2.Key);
        }

        public int GetHashCode(Tag x)
        {
            return x.Key.GetHashCode();
        }
    }
#endif

        internal static Tag[] GetDurationMetricTags(this Activity activity)
        {
            return activity.GetCustomProperty(ActivityCustomPropertyNames.DurationMetricTags) switch
            {
                Tag[] tags => tags,
                null => Array.Empty<Tag>(),
                _ => throw new InvalidOperationException("Invalid duration metric tags in activity"),
            };
        }
    }
}
