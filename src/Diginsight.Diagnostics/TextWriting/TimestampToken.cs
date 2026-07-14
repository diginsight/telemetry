using System.Globalization;
using System.Text;

namespace Diginsight.Diagnostics.TextWriting;

public sealed class TimestampToken : ILineToken
{
    private string? format;

    public string? Format
    {
        get => format;
        set
        {
            if (value is not null)
            {
                try
                {
                    _ = DateTime.UtcNow.ToString(value);
                }
                catch (FormatException)
                {
                    throw new ArgumentException("Invalid timestamp format");
                }
            }

            format = value;
        }
    }

    internal string? FormatUnsafe
    {
        set => format = value;
    }

    public CultureInfo? Culture { get; set; }

    public void Apply(ref MutableLineDescriptor lineDescriptor)
    {
        lineDescriptor.Appenders.Add(new Appender(Format, Culture));
    }

    public ILineToken Clone() => new TimestampToken() { FormatUnsafe = format, Culture = Culture };

    private sealed class Appender : IPrefixTokenAppender
    {
        private readonly
#if NET
            CompositeFormat
#else
            string
#endif
            format;

        private readonly CultureInfo culture;

        public Appender(string? format, CultureInfo? culture)
        {
#if NET
            string tmpFormat
#else
            this.format
#endif
                = $"{{0:{format ?? "yyyy-MM-dd'T'HH:mm:ss.fff"}}}";
#if NET
            this.format = CompositeFormat.Parse(tmpFormat);
#endif

            this.culture = culture ?? CultureInfo.InvariantCulture;
        }

        public void Append(StringBuilder sb, ref int length, in LinePrefixData linePrefixData, bool useColor)
        {
            int previousLength = sb.Length;
            sb.AppendFormat(culture, format, linePrefixData.Timestamp);
            length += sb.Length - previousLength;
        }
    }
}
