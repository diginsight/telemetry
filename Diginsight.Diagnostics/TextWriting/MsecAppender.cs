using System.Globalization;
using System.Text;

namespace Diginsight.Diagnostics.TextWriting;

internal abstract class MsecAppender : IPrefixTokenAppender
{
    protected static void Append(StringBuilder sb, double? msec)
    {
        string str = msec switch
        {
            null => "",
            < 1 => string.Format(CultureInfo.InvariantCulture, ".{0:000}m", msec.Value * 1000),
            < 10000 => string.Format(CultureInfo.InvariantCulture, "{0:0}m", msec.Value),
            < 100000 => string.Format(CultureInfo.InvariantCulture, "{0}s", Math.Round(msec.Value / 1000, 1)),
            _ => string.Format(CultureInfo.InvariantCulture, "{0:0}s", msec.Value / 1000),
        };

        sb.Append(str.PadLeft(5));
    }
}
