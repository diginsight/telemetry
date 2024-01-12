using System.Globalization;
using System.Text;

namespace Diginsight.Diagnostics.TextWriting;

internal sealed class TimestampAppender : IPrefixTokenAppender
{
#if NET8_0_OR_GREATER
    private readonly CompositeFormat format;
#else
    private readonly string format;
#endif
    private readonly CultureInfo culture;

    public TimestampAppender(string? format, CultureInfo? culture)
    {
#if NET8_0_OR_GREATER
        string tmpFormat =
#else
        this.format =
#endif
            $"{{0:{format ?? "yyyy-MM-dd'T'HH:mm:ss.fff"}}}";
#if NET8_0_OR_GREATER
        this.format = CompositeFormat.Parse(tmpFormat);
#endif

        this.culture = culture ?? CultureInfo.InvariantCulture;
    }

    public void Append(StringBuilder sb, in LinePrefixData linePrefixData)
    {
        sb.AppendFormat(culture, format, linePrefixData.Timestamp);
    }
}
