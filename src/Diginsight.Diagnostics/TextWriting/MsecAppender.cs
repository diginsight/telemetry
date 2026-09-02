using Pastel;
using System.Globalization;
using System.Text;

namespace Diginsight.Diagnostics.TextWriting;

/// <summary>
/// Represents a base class for prefix token appenders that render millisecond values.
/// </summary>
public abstract class MsecAppender : IPrefixTokenAppender
{
    /// <inheritdoc />
    public abstract void Append(StringBuilder sb, ref int length, in LinePrefixData linePrefixData, bool useColor);

    /// <summary>
    /// Appends a millisecond value to the specified string builder.
    /// </summary>
    /// <param name="sb">The string builder to append to.</param>
    /// <param name="length">The current visible prefix length.</param>
    /// <param name="msec">The millisecond value to append.</param>
    /// <param name="useColor">Whether to emit ANSI color sequences.</param>
    protected static void Append(StringBuilder sb, ref int length, double? msec, bool useColor)
    {
        string str = msec switch
        {
            null => "",
            < 0 => "-",
            < 1 => string.Format(CultureInfo.InvariantCulture, ".{0:000}m", msec.Value * 1000),
            < 10000 => string.Format(CultureInfo.InvariantCulture, "{0:0}m", msec.Value),
            < 100000 => string.Format(CultureInfo.InvariantCulture, "{0}s", Math.Round(msec.Value / 1000, 1)),
            _ => string.Format(CultureInfo.InvariantCulture, "{0:0}s", msec.Value / 1000),
        };

        string coloredStr = useColor && msec >= 1000 ? str.Pastel(ConsoleColor.Black).PastelBg(ConsoleColor.DarkGray) : str;

        int remainingLength = 5 - str.Length;
        string finalStr = remainingLength > 0 ? coloredStr.PadLeft(remainingLength + coloredStr.Length) : coloredStr;

        length += Math.Max(str.Length, 5);
        sb.Append(finalStr);
    }
}
