using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;

namespace Diginsight.Diagnostics.AspNetCore;

internal static partial class HttpHeadersHelper
{
#if NET7_0_OR_GREATER
    [GeneratedRegex("^([^=]+?)(=(?:[a-z]+)?)?$", RegexOptions.NonBacktracking | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SpecRegexImpl();

    /// <inheritdoc cref="SpecRegexImpl" />
    private static Regex SpecRegex => SpecRegexImpl();
#else
    private static readonly Regex SpecRegex = new ("^([^=]+?)(=(?:[a-z]+)?)?$", RegexOptions.NonBacktracking | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
#endif

    public static IEnumerable<string?> GetMatches(
        string? activitySourceName, string activityName, string headerName, IHttpContextAccessor httpContextAccessor
    )
    {
        IEnumerable<string> rawSpecs = (httpContextAccessor.HttpContext?.Request.Headers[headerName] ?? default).NormalizeHttpHeaderValue();
        foreach (string rawSpec in rawSpecs.Reverse())
        {
            if (SpecRegex.Match(rawSpec) is not { Success: true } match)
                continue;

            string namePattern = match.Groups[1].Value;
            bool isMatch = activitySourceName is null
                ? ActivityUtils.NameMatchesPattern(activityName, namePattern)
                : ActivityUtils.FullNameMatchesPattern(activitySourceName, activityName, namePattern);
            if (isMatch)
            {
                yield return match.Groups[2] is { Success: true, Value: var matchValue } ? matchValue : null;
            }
        }
    }

    public static bool? ShouldInclude(
        string? activitySourceName, string activityName, string headerName, IHttpContextAccessor httpContextAccessor
    )
    {
        bool[] matches = GetMatches(activitySourceName, activityName, headerName, httpContextAccessor)
            .Select(static x => x is null ? (true, true) : (bool.TryParse(x, out bool result), result))
            .Where(static x => x.Item1)
            .Select(static x => x.Item2)
            .ToArray();

        return matches.Any() ? matches.All(static x => x) : null;
    }
}
