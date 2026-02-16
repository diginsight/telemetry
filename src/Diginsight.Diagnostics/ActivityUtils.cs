using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Diginsight.Diagnostics;

public static class ActivityUtils
{
#if !(NET || NETSTANDARD2_1_OR_GREATER)
    private static readonly char[] PipeSeparators = [ '|' ];
#endif

    public static readonly ActivityListener DepthSetterActivityListener = new ()
    {
        Sample = static (ref creationOptions) =>
        {
            ActivityContext parent = creationOptions.Parent;
            string? rawParentDepth = TraceState.Parse(parent.TraceState).GetValueOrDefault(ActivityDepth.TraceStateKey);

            ActivityDepth parentDepth = ActivityDepth.FromTraceStateValue(rawParentDepth) ?? default;
            ActivityDepth depth = parent.IsRemote ? parentDepth.MakeRemoteChild() : parentDepth.MakeLocalChild();
            TraceState traceState = TraceState.Parse(creationOptions.TraceState);
            traceState[ActivityDepth.TraceStateKey] = depth.ToTraceStateValue();

            creationOptions = creationOptions with { TraceState = traceState.ToString() };
            return ActivitySamplingResult.PropagationData;
        },
        ShouldListenTo = static _ => true,
    };

    public static bool NameMatchesPattern(string name, string namePattern)
    {
        return new Regex($"^{string.Join(".*", namePattern.Split('*').Select(Regex.Escape))}$", RegexOptions.NonBacktracking | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).IsMatch(name);
    }

    public static bool FullNameMatchesPattern(string sourceName, string operationName, string fullNamePattern)
    {
#if NET || NETSTANDARD2_1_OR_GREATER
        return fullNamePattern.Split('|', 3) switch
#else
        return fullNamePattern.Split(PipeSeparators, 3) switch
#endif
        {
            [ _ ] => NameMatchesPattern(operationName, fullNamePattern),
            [ var sourceNamePattern, var operationNamePattern ] => (sourceNamePattern, operationNamePattern) switch
            {
                ("", "") => throw new ArgumentException("Invalid source+activity name pattern"),
                ("", _) => NameMatchesPattern(operationName, operationNamePattern),
                (_, "") => NameMatchesPattern(sourceName, sourceNamePattern),
                (_, _) => NameMatchesPattern(sourceName, sourceNamePattern) &&
                    NameMatchesPattern(operationName, operationNamePattern),
            },
            _ => throw new ArgumentException("Invalid source+activity name pattern"),
        };
    }

    extension(Activity)
    {
        public static IDisposable? WithCurrent(Activity? activity)
        {
            if (Activity.Current is not { } current)
                return null;

            Activity.Current = activity;
            return new CallbackDisposable(() => { Activity.Current = current; });
        }
    }
}
