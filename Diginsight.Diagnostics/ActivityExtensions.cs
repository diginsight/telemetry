using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class ActivityExtensions
{
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
            depth = ActivityDepth.FromTraceStateValue(TraceState.Parse(activity.TraceStateString).GetValueOrDefault(ActivityDepth.DepthTraceStateKey))
                ?? GetDepth(activity.Parent).MakeChild(false);

            activity.SetCustomProperty(ActivityCustomPropertyNames.Depth, depth);
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
