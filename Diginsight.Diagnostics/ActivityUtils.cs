using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics;

public static class ActivityUtils
{
#if !(NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
    private static readonly char[] StarSeparators = [ '*' ];
#endif

    public static bool NameMatchesPattern(string name, string namePattern)
    {
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        return namePattern.Split('*', 3) switch
#else
        return namePattern.Split(StarSeparators, 3) switch
#endif
        {
            [ _ ] => string.Equals(name, namePattern, StringComparison.OrdinalIgnoreCase),
            [ var startToken, var endToken ] => (startToken, endToken) switch
            {
                ("", "") => true,
                ("", _) => name.EndsWith(endToken, StringComparison.OrdinalIgnoreCase),
                (_, "") => name.StartsWith(startToken, StringComparison.OrdinalIgnoreCase),
                (_, _) => name.StartsWith(startToken, StringComparison.OrdinalIgnoreCase) &&
                    name.EndsWith(endToken, StringComparison.OrdinalIgnoreCase),
            },
            _ => throw new ArgumentException("Invalid activity name pattern"),
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool NameMatchesPattern(string name, IEnumerable<string> namePatterns)
    {
        return namePatterns.Any(x => NameMatchesPattern(name, x));
    }
}
