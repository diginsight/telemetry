using System.Globalization;
using System.Text;

namespace Diginsight.Diagnostics.TextWriting;

/// <summary>
/// Represents a line token that appends the log timestamp to the line prefix.
/// </summary>
public sealed class TimestampToken : ILineToken
{
    private string? format;

    /// <summary>
    /// Gets the timestamp format.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the value is not a valid timestamp format.</exception>
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

    /// <summary>
    /// Gets the culture used to format the timestamp.
    /// </summary>
    public CultureInfo? Culture { get; set; }

    /// <inheritdoc />
    public void Apply(ref MutableLineDescriptor lineDescriptor)
    {
        lineDescriptor.Appenders.Add(new Appender(Format, Culture));
    }

    /// <inheritdoc />
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
