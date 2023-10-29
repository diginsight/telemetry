using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Diginsight;

public static class StringExtensions
{
    public static string? ToStringInvariant(this object? obj)
    {
        return obj switch
        {
            bool b => b.ToString().ToLowerInvariant(),
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => obj?.ToString(),
        };
    }

    public static string? HardTrim(this string? str)
    {
        str = (str ?? "").Trim();
        return str switch
        {
            "" => null,
            _ => str,
        };
    }

    [return: NotNullIfNotNull(nameof(str))]
    public static string? Truncate(this string? str, int length)
    {
        return str?.Length > length
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            ? str[..length]
#else
            ? str.Substring(0, length)
#endif
            : str;
    }
}
