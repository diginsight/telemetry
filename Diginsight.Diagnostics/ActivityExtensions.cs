using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics;

public static class ActivityExtensions
{
#if !(NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
    private static readonly char[] StarSeparators = [ '*' ];
#endif

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

    public static void SetOutput(this Activity? activity, object? output)
    {
        if (activity is null)
        {
            return;
        }
        if (activity.GetCustomProperty(ActivityCustomPropertyNames.Logger) is null)
        {
            throw new ArgumentException("Invalid logger in activity");
        }

        activity.SetCustomProperty(ActivityCustomPropertyNames.Output, new StrongBox<object?>(output));
    }

    public static void SetNamedOutputs(this Activity? activity, object namedOutputs)
    {
        if (namedOutputs is null)
        {
            throw new ArgumentNullException(nameof(namedOutputs));
        }

        activity?.SetCustomProperty(ActivityCustomPropertyNames.NamedOutputs, namedOutputs);
    }

    public static ActivityDepth GetDepth(this Activity? activity)
    {
        if (activity is null)
        {
            return default;
        }

        if (activity.GetCustomProperty(ActivityCustomPropertyNames.Depth) is not ActivityDepth depth)
        {
            depth = GetDepth(activity.Parent).GetChild(activity.HasRemoteParent);
            activity.SetCustomProperty(ActivityCustomPropertyNames.Depth, depth);
        }

        return depth;
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

    public static Type? GetCallerType(this Activity activity)
    {
        return activity.GetCustomProperty(ActivityCustomPropertyNames.CallerType) switch
        {
            Type t => t,
            null => null,
            _ => throw new InvalidOperationException("Invalid caller type in activity"),
        };
    }

    public static bool NameMatchesPattern(string name, string namePattern)
    {
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        return namePattern.Split('*', 3) switch
#else
        return namePattern.Split(StarSeparators, 3) switch
#endif
        {
            [ _ ] => string.Equals(name, namePattern, StringComparison.OrdinalIgnoreCase),
            [ var startToken, var endToken ] => (startToken, endToken) switch
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
    public static bool NameMatchesPattern(this Activity activity, IEnumerable<string> namePatterns)
    {
        return namePatterns.Any(x => NameMatchesPattern(activity.OperationName, x));
    }

    public static string? GetLabel(this Activity activity)
    {
        if (activity is null)
        {
            throw new ArgumentNullException(nameof(activity));
        }

        return activity.GetCustomProperty(ActivityCustomPropertyNames.Label) switch
        {
            string s => s,
            null => null,
            _ => throw new InvalidOperationException("Invalid label in activity"),
        };
    }

    public static void SetLabel(this Activity activity, string? label)
    {
        if (activity is null)
        {
            throw new ArgumentNullException(nameof(activity));
        }

        activity.SetCustomProperty(ActivityCustomPropertyNames.Label, label);
    }

    public static Activity? FindLabeledParent(this Activity activity, string label)
    {
        return activity.GetAncestors(true).SkipWhile(a => a.GetLabel() != label).FirstOrDefault();
    }

    public static IEnumerable<Activity> GetAncestors(this Activity activity, bool includeSelf = false)
    {
        if (activity is null)
        {
            throw new ArgumentNullException(nameof(activity));
        }

        if (includeSelf)
        {
            yield return activity;
        }
        for (Activity? current = activity.Parent; current is not null; current = current.Parent)
        {
            yield return current;
        }
    }
}
