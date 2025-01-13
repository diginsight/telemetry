using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Diginsight.Diagnostics.AspNetCore;

internal static class HttpHeadersHelper
{
    private static readonly Regex SpecRegex = new ("^([^=]+?)(=(?:[a-z]+)?)?$", RegexOptions.IgnoreCase);

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
