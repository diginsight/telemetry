using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics;

public static class ActivityUtils
{
#if !(NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
    private static readonly char[] StarSeparators = [ '*' ];
#endif

    private static readonly ActivityListener DepthActivityListener = new ()
    {
        Sample = static (ref ActivityCreationOptions<ActivityContext> creationOptions) =>
        {
            ActivityContext parent = creationOptions.Parent;
            string? rawParentDepth = TraceState.Parse(parent.TraceState).GetValueOrDefault(ActivityDepth.DepthTraceStateKey);

            ActivityDepth depth = (ActivityDepth.FromTraceStateValue(rawParentDepth) ?? default).MakeChild(parent.IsRemote);
            TraceState traceState = TraceState.Parse(creationOptions.TraceState);
            traceState[ActivityDepth.DepthTraceStateKey] = depth.ToTraceStateValue();

            creationOptions = creationOptions with { TraceState = traceState.ToString() };
            return ActivitySamplingResult.PropagationData;
        },
        ShouldListenTo = static _ => true,
    };

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
    public static bool NameMatchesPattern(string name, IEnumerable<string> namePatterns)
    {
        return namePatterns.Any(x => NameMatchesPattern(name, x));
    }

    public static void AddActivityListeners(ActivityLifecycleLogEmitter emitter, Func<ActivitySource, bool> shouldEmit)
    {
        ActivitySource.AddActivityListener(DepthActivityListener);

        ActivitySource.AddActivityListener(
            new ActivityListener()
            {
                ActivityStarted = emitter.OnStart,
                ActivityStopped = emitter.OnEnd,
                Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ShouldListenTo = shouldEmit,
            }
        );
    }
}
