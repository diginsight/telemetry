using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Diginsight.Diagnostics;

public static class ActivityUtils
{
#if !(NET || NETSTANDARD2_1_OR_GREATER)
    private static readonly char[] PipeSeparators = [ '|' ];
#endif

    // Cache compiled regex patterns to avoid repeated compilation
    private static readonly ConcurrentDictionary<string, Regex> PatternCache = new(StringComparer.OrdinalIgnoreCase);

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
        if (!namePattern.Contains('*')) { return name.Equals(namePattern, StringComparison.OrdinalIgnoreCase); } // Fast path: exact match (no wildcards)
        string[] parts = namePattern.Split('*'); // Fast path: simple wildcard patterns (most common case)
        if (parts.Length == 2 && parts[0] == "") { return name.EndsWith(parts[1], StringComparison.OrdinalIgnoreCase); } // Pattern: "*something" (ends with)
        if (parts.Length == 2 && parts[1] == "") { return name.StartsWith(parts[0], StringComparison.OrdinalIgnoreCase); } // Pattern: "something*" (starts with)
        if (parts.Length == 3 && parts[0] == "" && parts[2] == "") { return name.Contains(parts[1], StringComparison.OrdinalIgnoreCase); } // Pattern: "*something*" (contains)

        Regex regex = PatternCache.GetOrAdd(namePattern, static pattern =>
        {
            string regexPattern = $"^{string.Join(".*", pattern.Split('*').Select(Regex.Escape))}$";
            return new Regex(regexPattern, RegexOptions.NonBacktracking | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }); // Complex pattern: use cached regex (rare case)

        return regex.IsMatch(name);
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
                (_, "") => NameMatchesPattern(sourceName, sourceNamePattern),
                ("", _) => NameMatchesPattern(operationName, operationNamePattern),
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
