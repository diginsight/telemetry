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
        Sample = static (ref ActivityCreationOptions<ActivityContext> creationOptions) =>
        {
            ActivityContext parent = creationOptions.Parent;
            string? rawParentDepth = TraceState.Parse(parent.TraceState).GetValueOrDefault(ActivityDepth.DepthTraceStateKey);

            ActivityDepth parentDepth = ActivityDepth.FromTraceStateValue(rawParentDepth) ?? default;
            ActivityDepth depth = parent.IsRemote ? parentDepth.MakeRemoteChild() : parentDepth.MakeLocalChild();
            TraceState traceState = TraceState.Parse(creationOptions.TraceState);
            traceState[ActivityDepth.DepthTraceStateKey] = depth.ToTraceStateValue();

            creationOptions = creationOptions with { TraceState = traceState.ToString() };
            return ActivitySamplingResult.PropagationData;
        },
        ShouldListenTo = static _ => true,
    };

    public static bool NameMatchesPattern(string name, string namePattern)
    {
#if NET || NETSTANDARD2_1_OR_GREATER
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

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //public static bool NameMatchesPatterns(string name, IEnumerable<string> namePatterns)
    //{
    //    return namePatterns.Any(x => NameMatchesPattern(name, x));
    //}

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //public static bool? NameCompliesWithPatterns(string name, IEnumerable<string> namePatterns, IEnumerable<string> notNamePatterns)
    //{
    //    return (namePatterns.Any(), notNamePatterns.Any()) switch
    //    {
    //        (true, true) => NameMatchesPatterns(name, namePatterns) && !NameMatchesPatterns(name, notNamePatterns),
    //        (true, false) => NameMatchesPatterns(name, namePatterns),
    //        (false, true) => !NameMatchesPatterns(name, notNamePatterns),
    //        (false, false) => null,
    //    };
    //}

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

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //public static bool FullNameMatchesPatterns(string sourceName, string operationName, IEnumerable<string> fullNamePatterns)
    //{
    //    return fullNamePatterns.Any(x => FullNameMatchesPattern(sourceName, operationName, x));
    //}

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //public static bool? FullNameCompliesWithPatterns(
    //    string sourceName, string operationName, IEnumerable<string> fullNamePatterns, IEnumerable<string> notFullNamePatterns
    //)
    //{
    //    return (fullNamePatterns.Any(), notFullNamePatterns.Any()) switch
    //    {
    //        (true, true) => FullNameMatchesPatterns(sourceName, operationName, fullNamePatterns)
    //            && !FullNameMatchesPatterns(sourceName, operationName, notFullNamePatterns),
    //        (true, false) => FullNameMatchesPatterns(sourceName, operationName, fullNamePatterns),
    //        (false, true) => !FullNameMatchesPatterns(sourceName, operationName, notFullNamePatterns),
    //        (false, false) => null,
    //    };
    //}

    public static IDisposable? WithCurrent(Activity? activity)
    {
        if (Activity.Current is not { } current)
            return null;

        Activity.Current = activity;
        return new CallbackDisposable(() => { Activity.Current = current; });
    }
}
