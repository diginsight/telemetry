using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;

namespace Diginsight.Diagnostics.AspNetCore;

internal static class HttpHeadersHelper
{
    private static readonly Regex SpecRegex = new (@"^([^+\-]+?)(-?)$", RegexOptions.IgnoreCase);

    public static bool? ShouldInclude(string? activitySourceName, string activityName, string headerName, IHttpContextAccessor httpContextAccessor)
    {
        IEnumerable<string> rawSpecs = (httpContextAccessor.HttpContext?.Request.Headers[headerName] ?? default).NormalizeHttpHeaderValue();
        foreach (string rawSpec in rawSpecs.Reverse())
        {
            if (SpecRegex.Match(rawSpec) is not { Success: true } match)
                continue;

            string namePattern = match.Groups[1].Value;
            bool matches = activitySourceName is null
                ? ActivityUtils.NameMatchesPattern(activityName, namePattern)
                : ActivityUtils.FullNameMatchesPattern(activitySourceName, activityName, namePattern);
            if (matches)
            {
                return match.Groups[2].Value != "-";
            }
        }

        return null;
    }
}
