using System.Diagnostics;

namespace Diginsight.Diagnostics;

public static class ActivityUtils
{
#if !(NET || NETSTANDARD2_1_OR_GREATER)
    private static readonly char[] StarSeparators = [ '*' ];
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
#if NET || NETSTANDARD2_1_OR_GREATER
    return namePattern.Split('*', 4) switch
#else
        return namePattern.Split(StarSeparators, 4) switch
#endif
        {
            [_] => string.Equals(name, namePattern, StringComparison.OrdinalIgnoreCase),
            [var startToken, var endToken] => (startToken, endToken) switch
            {
                ("", "") => true,
                ("", _) => name.EndsWith(endToken, StringComparison.OrdinalIgnoreCase),
                (_, "") => name.StartsWith(startToken, StringComparison.OrdinalIgnoreCase),
                (_, _) => name.StartsWith(startToken, StringComparison.OrdinalIgnoreCase) &&
                    name.EndsWith(endToken, StringComparison.OrdinalIgnoreCase),
            },
            [var startToken, var middleToken, var endToken] => (startToken, middleToken, endToken) switch
            {
                ("", "", "") => true,
                ("", _, "") => name.Contains(middleToken, StringComparison.OrdinalIgnoreCase),
                ("", _, _) => name.Contains(middleToken, StringComparison.OrdinalIgnoreCase) &&
                    name.EndsWith(endToken, StringComparison.OrdinalIgnoreCase),
                (_, "", _) => name.StartsWith(startToken, StringComparison.OrdinalIgnoreCase) &&
                    name.EndsWith(endToken, StringComparison.OrdinalIgnoreCase),
                (_, _, "") => name.StartsWith(startToken, StringComparison.OrdinalIgnoreCase) &&
                    name.Contains(middleToken, StringComparison.OrdinalIgnoreCase),
                (_, _, _) => name.StartsWith(startToken, StringComparison.OrdinalIgnoreCase) &&
                    name.Contains(middleToken, StringComparison.OrdinalIgnoreCase) &&
                    name.EndsWith(endToken, StringComparison.OrdinalIgnoreCase),
            },
            _ => throw new ArgumentException("Invalid activity name pattern"),
        };
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

    public static IDisposable? WithCurrent(Activity? activity)
    {
        if (Activity.Current is not { } current)
            return null;

        Activity.Current = activity;
        return new CallbackDisposable(() => { Activity.Current = current; });
    }
}
