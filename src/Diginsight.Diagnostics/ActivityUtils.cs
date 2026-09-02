using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Diginsight.Diagnostics;

/// <summary>
/// Provides utility methods for activity lifecycle logging and propagation.
/// </summary>
public static class ActivityUtils
{
#if !(NET || NETSTANDARD2_1_OR_GREATER)
    private static readonly char[] PipeSeparators = [ '|' ];
#endif

    private static readonly IDictionary<string, Regex> PatternRegexCache = new Dictionary<string, Regex>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Creates an activity listener that propagates activity depth through trace state.
    /// </summary>
    /// <param name="shouldListenTo">The predicate used to determine whether an activity source should be listened to.</param>
    /// <returns>The created activity listener.</returns>
    public static ActivityListener CreateDepthSetterActivityListener(Func<ActivitySource, bool>? shouldListenTo = null) => new ()
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
        ShouldListenTo = shouldListenTo ?? (static _ => true),
    };

    /// <summary>
    /// Determines whether a name matches a wildcard pattern.
    /// </summary>
    /// <param name="name">The name to match.</param>
    /// <param name="namePattern">The wildcard pattern.</param>
    /// <returns><c>true</c> if the name matches the pattern; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// The pattern uses <c>*</c> as a wildcard matching any sequence of characters (including the empty sequence); all other characters are matched literally.
    /// The pattern must match the entire name, and matching is case-insensitive. Compiled patterns are cached for reuse.
    /// </remarks>
    public static bool NameMatchesPattern(string name, string namePattern)
    {
        Regex regex = PatternRegexCache.TryGetValue(namePattern, out Regex? regex0)
            ? regex0
            : PatternRegexCache[namePattern] = new Regex($"^{string.Join(".*", namePattern.Split('*').Select(Regex.Escape))}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return regex.IsMatch(name);
    }

    /// <summary>
    /// Determines whether an activity source name and operation name match a full name pattern.
    /// </summary>
    /// <param name="sourceName">The activity source name.</param>
    /// <param name="operationName">The activity operation name.</param>
    /// <param name="fullNamePattern">The full name pattern.</param>
    /// <returns><c>true</c> if the source name and operation name match the pattern; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentException">Thrown when the full name pattern is invalid.</exception>
    /// <remarks>
    /// The pattern is composed of an optional source name pattern and an operation name pattern separated by a <c>|</c> character,
    /// in the form <c>sourceNamePattern|operationNamePattern</c>.  When no separator is present, the whole pattern is matched against the operation name only.
    /// When a side of the separator is empty, only the non-empty side is matched: an empty source name pattern matches against the operation name,
    /// while an empty operation name pattern matches against the source name. Both sides being empty, or the presence of more than one separator, is invalid.
    /// Each side is a wildcard pattern evaluated by <see cref="NameMatchesPattern" />.
    /// </remarks>
    public static bool FullNameMatchesPattern(string sourceName, string operationName, string fullNamePattern)
    {
#if NET || NETSTANDARD2_1_OR_GREATER
        const char separator = '|';
#else
        char[] separator = PipeSeparators;
#endif

        return fullNamePattern.Split(separator, 3) switch
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

    /// <summary>
    /// Provides static extension members for <see cref="Activity" />.
    /// </summary>
    extension(Activity)
    {
        /// <summary>
        /// Temporarily replaces the current activity.
        /// </summary>
        /// <param name="activity">The activity to set as current.</param>
        /// <returns>A disposable that restores the previous current activity, or <c>null</c> if there is no current activity.</returns>
        public static IDisposable? WithCurrent(Activity? activity)
        {
            if (Activity.Current is not { } current)
                return null;

            Activity.Current = activity;
            return new CallbackDisposable(() => { Activity.Current = current; });
        }
    }
}
