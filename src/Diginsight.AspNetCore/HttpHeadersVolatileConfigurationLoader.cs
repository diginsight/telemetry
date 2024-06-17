using Diginsight.CAOptions;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;

namespace Diginsight.AspNetCore;

public sealed class HttpHeadersVolatileConfigurationLoader : VolatileConfigurationLoader
{
    private const string HeaderName = "Volatile-Configuration";

    private readonly IHttpContextAccessor httpContextAccessor;

    public HttpHeadersVolatileConfigurationLoader(
        IVolatileConfigurationStorage storage,
        IHttpContextAccessor httpContextAccessor
    )
        : base(storage)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<IEnumerable<KeyValuePair<string, string?>>> LoadImplAsync(CancellationToken cancellationToken = default)
    {
        if (httpContextAccessor.HttpContext is not { } httpContext)
        {
            return [ ];
        }

        IDictionary<string, string?> dict = new Dictionary<string, string?>();
        foreach (string rawSpec in httpContext.Request.Headers[HeaderName].NormalizeHttpHeaderValue())
        {
            if (Statics.SpecRegex.Match(rawSpec) is not { Success: true } match)
                continue;

            dict[match.Groups[1].Value] = match.Groups[2] is { Success: true, Value: var matchValue } ? matchValue : null;
        }

        return dict;
    }
}

file static class Statics
{
    internal static readonly Regex SpecRegex = new ("^([^=]+?)(?:=(.*))?$");
}
