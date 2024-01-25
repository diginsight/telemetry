using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Diginsight;

public static class StringExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [return: NotNullIfNotNull(nameof(obj))]
    public static string? ToStringInvariant(this object? obj)
    {
        return obj switch
        {
            bool b => b.ToString().ToLowerInvariant(),
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => obj?.ToString(),
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string? HardTrim(this string? str)
    {
        str = (str ?? "").Trim();
        return str switch
        {
            "" => null,
            _ => str,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [return: NotNullIfNotNull(nameof(str))]
    public static string? Truncate(this string? str, int length)
    {
        return str?.Length > length ? str[..length] : str;
    }
}
