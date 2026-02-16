#if !NET7_0_OR_GREATER
namespace System.Text.RegularExpressions;

public static class RegexExtensions
{
    extension(RegexOptions)
    {
        public static RegexOptions NonBacktracking => RegexOptions.None;
    }
}
#endif
