using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics;

public static class ActivityExtensions
{
#if !(NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
    private static readonly char[] StarSeparators = { '*' };
#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RecordDurationMetric(this Activity activity, Histogram<long> durationMetric, params Tag[] tags)
    {
        activity.RecordDurationMetric((object)durationMetric, tags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RecordDurationMetric(this Activity activity, Histogram<double> durationMetric, params Tag[] tags)
    {
        activity.RecordDurationMetric((object)durationMetric, tags);
    }

    private static void RecordDurationMetric(this Activity activity, object durationMetric, params Tag[] tags)
    {
        if (activity is null)
        {
            throw new ArgumentNullException(nameof(activity));
        }

        activity.SetCustomProperty(ActivityCustomPropertyNames.DurationMetric, durationMetric);
        activity.SetCustomProperty(ActivityCustomPropertyNames.DurationMetricTags, tags);
    }

    public static void AddDurationMetricTags(this Activity activity, params Tag[] tags)
    {
        if (activity is null)
        {
            throw new ArgumentNullException(nameof(activity));
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

    internal static bool MatchesActivityNamePattern(string name, string namePattern)
    {
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        return namePattern.Split('*', 3) switch
#else
        string[] tokens = namePattern.Split(StarSeparators, 3);
        string startToken;
        string endToken;

        return tokens.Length switch
#endif
        {
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            [_] => string.Equals(name, namePattern, StringComparison.OrdinalIgnoreCase),
            [var startToken, var endToken] => (startToken, endToken) switch
#else
            1 => string.Equals(name, namePattern, StringComparison.OrdinalIgnoreCase),
            2 => (startToken = tokens[0], endToken = tokens[1]) switch
#endif
            {
                ("", "") => true,
                ("", _) => name.EndsWith(endToken, StringComparison.OrdinalIgnoreCase),
                (_, "") => name.StartsWith(startToken, StringComparison.OrdinalIgnoreCase),
                (_, _) => name.StartsWith(startToken, StringComparison.OrdinalIgnoreCase) &&
                    name.EndsWith(endToken, StringComparison.OrdinalIgnoreCase),
            },
            _ => throw new ArgumentException("Invalid activity name pattern"),
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool MatchesActivityNamePattern(this Activity activity, IEnumerable<string> namePatterns)
    {
        string name = activity.OperationName;
        return namePatterns.Any(x => MatchesActivityNamePattern(name, x));
    }
}
