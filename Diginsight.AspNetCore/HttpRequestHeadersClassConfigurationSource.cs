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

    public void PopulateAll<T>(IEnumerable<string> prefixes, string key, IDictionary<string, T> dict, IClassConfigurationGetter.SafeConverter<T>? tryConvert)
    {
        if (httpContextAccessor.HttpContext?.Request.Headers is not { } headerDictionary)
        {
            return;
        }

        foreach (string prefix in prefixes)
        {
            if (!headerDictionary.TryGetValue(prefix + key, out StringValues stringValues))
            {
                continue;
            }

            string rawValue = stringValues.LastOrDefault()!;
            if (tryConvert?.Invoke(rawValue, out T value) == true)
            {
                dict[prefix] = value;
            }
            else
            {
                try
                {
                    TypeConverter converter = TypeDescriptor.GetConverter(typeof(T));
                    value = (T)converter.ConvertFrom(null, CultureInfo.InvariantCulture, rawValue)!;
                }
                catch (Exception)
                {
                    continue;
                }
            }

            dict[prefix] = value;
        }
    }

    public bool TryGet<T>(IEnumerable<string> prefixes, string key, out T value, IClassConfigurationGetter.SafeConverter<T>? tryConvert)
    {
        if (httpContextAccessor.HttpContext?.Request.Headers is not { } headerDictionary)
        {
            value = default!;
            return false;
        }

        foreach (string fullKey in prefixes.Select(x => x + key))
        {
            if (!headerDictionary.TryGetValue(fullKey, out StringValues stringValues))
            {
                continue;
            }

            string rawValue = stringValues.LastOrDefault()!;
            if (tryConvert?.Invoke(rawValue, out value) == true)
            {
                return true;
            }

            try
            {
                TypeConverter converter = TypeDescriptor.GetConverter(typeof(T));
                value = (T)converter.ConvertFrom(null, CultureInfo.InvariantCulture, rawValue)!;
                return true;
            }
            catch (Exception e)
            {
                _ = e;
            }
        }

        value = default!;
        return false;
    }
}
