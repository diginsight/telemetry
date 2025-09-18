using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class ActivityExtensions
{
    private static class CustomPropertyNames
    {
        public const string CustomDurationMetric = nameof(CustomDurationMetric);
        public const string CustomDurationMetricTags = nameof(CustomDurationMetricTags);
        public const string Depth = nameof(Depth);
        public const string Label = nameof(Label);
        public const string LogBehavior = nameof(LogBehavior);
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

        if (activity.GetCustomProperty(CustomPropertyNames.Depth) is not ActivityDepth depth)
        {
            depth = ActivityDepth.FromTraceStateValue(TraceState.Parse(activity.TraceStateString).GetValueOrDefault(ActivityDepth.TraceStateKey))
                ?? GetDepth(activity.Parent).MakeLocalChild();

            activity.SetCustomProperty(CustomPropertyNames.Depth, depth);
        }

        return depth;
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

    public static string? GetLabel(this Activity activity)
    {
        if (activity is null)
        {
            throw new ArgumentNullException(nameof(activity));
        }

        return activity.GetCustomProperty(CustomPropertyNames.Label) switch
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

        activity.SetCustomProperty(CustomPropertyNames.Label, label);
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

    public static Instrument? GetCustomDurationMetric(this Activity activity)
    {
        return activity.GetCustomProperty(CustomPropertyNames.CustomDurationMetric) switch
        {
            null => null,
            Instrument instrument and (Histogram<double> or Histogram<long>) => instrument,
            _ => throw new InvalidOperationException("Invalid duration metric in activity"),
        };
    }

    public static void SetCustomDurationMetric(this Activity activity, Histogram<long> metric, params Tag[] tags)
    {
        activity.SetCustomDurationMetric((Instrument)metric, tags);
    }

    public static void SetCustomDurationMetric(this Activity activity, Histogram<double> metric, params Tag[] tags)
    {
        activity.SetCustomDurationMetric((Instrument)metric, tags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SetCustomDurationMetric(this Activity activity, Instrument instrument, params Tag[] tags)
    {
        if (activity is null)
        {
            throw new ArgumentNullException(nameof(activity));
        }

        activity.SetCustomProperty(CustomPropertyNames.CustomDurationMetric, instrument);
        activity.SetCustomProperty(CustomPropertyNames.CustomDurationMetricTags, tags);
    }

    public static void AddTagsToCustomDurationMetric(this Activity activity, params Tag[] tags)
    {
        if (activity is null)
        {
            throw new ArgumentNullException(nameof(activity));
        }

        if (activity.GetCustomDurationMetric() is null)
        {
            throw new ArgumentException("Activity has no associated custom duration metric");
        }

        Tag[] allTags = tags
            .Concat(activity.GetCustomDurationMetricTags())
#if NET
            .DistinctBy(static x => x.Key)
#else
            .GroupBy(static x => x.Key, static (_, xs) => xs.First())
#endif
            .ToArray();
        activity.SetCustomProperty(CustomPropertyNames.CustomDurationMetricTags, allTags);
    }

    internal static Tag[] GetCustomDurationMetricTags(this Activity activity)
    {
        return activity.GetCustomProperty(CustomPropertyNames.CustomDurationMetricTags) switch
        {
            Tag[] tags => tags,
            null => [ ],
            _ => throw new InvalidOperationException("Invalid custom duration metric tags in activity"),
        };
    }

    internal static LogBehavior? GetLogBehavior(this Activity activity)
    {
        return activity.GetCustomProperty(CustomPropertyNames.LogBehavior) switch
        {
            LogBehavior lb => lb,
            null => null,
            _ => throw new InvalidOperationException("Invalid log behavior in activity"),
        };
    }

    internal static void SetLogBehavior(this Activity activity, LogBehavior logBehavior)
    {
        activity.SetCustomProperty(CustomPropertyNames.LogBehavior, logBehavior);
        if (logBehavior != LogBehavior.Show)
        {
            activity.SetCustomProperty(CustomPropertyNames.Depth, activity.GetDepth().MakeHidden());
        }
    }
}
