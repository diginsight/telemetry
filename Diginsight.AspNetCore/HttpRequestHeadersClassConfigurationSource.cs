using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.ComponentModel;
using System.Globalization;

namespace Diginsight.AspNetCore;

public sealed class HttpRequestHeadersClassConfigurationSource : IClassConfigurationSource
{
    private readonly IHttpContextAccessor httpContextAccessor;

    public HttpRequestHeadersClassConfigurationSource(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    public bool TryGet<T>(IEnumerable<string> prefixes, string key, out T? value)
    {
        if (httpContextAccessor.HttpContext?.Request.Headers is not { } headerDictionary)
        {
            value = default;
            return false;
        }

        foreach (string fullKey in prefixes.Select(x => x + key))
        {
            if (!headerDictionary.TryGetValue(fullKey, out StringValues stringValues))
            {
                continue;
            }

            try
            {
                var converter = TypeDescriptor.GetConverter(typeof(T));
#pragma warning disable CS8604 // Possible null reference argument
                // ReSharper disable once AssignNullToNotNullAttribute
                value = (T?)converter.ConvertFrom(null, CultureInfo.InvariantCulture, stringValues.LastOrDefault());
#pragma warning restore CS8604
                return true;
            }
            catch (Exception e)
            {
                _ = e;
            }
        }

        value = default;
        return false;
    }
}
