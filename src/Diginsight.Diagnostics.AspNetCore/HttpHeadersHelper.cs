using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;

namespace Diginsight.Diagnostics.AspNetCore;

internal static class HttpHeadersHelper
{
    private static readonly Regex SpecRegex = new (@"^([^+\-]+?)(-?)$", RegexOptions.IgnoreCase);

    public static bool? ShouldInclude(string activityName, string headerName, IHttpContextAccessor httpContextAccessor)
    {
        IEnumerable<string> rawSpecs = (httpContextAccessor.HttpContext?.Request.Headers[headerName] ?? default).NormalizeHttpHeaderValue();
        foreach (string rawSpec in rawSpecs.Reverse())
        {
            if (SpecRegex.Match(rawSpec) is not { Success: true } match)
                continue;

            if (ActivityUtils.NameMatchesPattern(activityName, match.Groups[1].Value))
                return match.Groups[2].Value != "-";
        }

        return null;
    }
}
