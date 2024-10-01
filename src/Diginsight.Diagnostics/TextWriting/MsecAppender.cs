using Pastel;
using System.Globalization;
using System.Text;

namespace Diginsight.Diagnostics.TextWriting;

public abstract class MsecAppender : IPrefixTokenAppender
{
    public abstract void Append(StringBuilder sb, ref int length, in LinePrefixData linePrefixData, bool useColor);

    protected static void Append(StringBuilder sb, ref int length, double? msec, bool useColor)
    {
        string str = msec switch
        {
            null => "",
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
