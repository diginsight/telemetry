using System.Globalization;

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

    public CultureInfo? Culture { get; set; }

    public void Apply(ref MutableLineDescriptor lineDescriptor)
    {
        lineDescriptor.Appenders.Add(new TimestampAppender(Format, Culture));
    }

    internal static ILineToken Parse(ReadOnlySpan<char> tokenSpan)
    {
        string? format;
        CultureInfo? culture;

        if (tokenSpan.IsEmpty)
        {
            format = null;
            culture = null;
        }
        else
        {
            if (tokenSpan[0] != ';')
            {
                throw new FormatException("Expected ';' or nothing");
            }

            tokenSpan = tokenSpan[1..];
            int semicolonIndex = tokenSpan.LastIndexOf(';');
            if (semicolonIndex < 0)
            {
                format = tokenSpan.ToString();
                culture = null;
            }
            else
            {
                ReadOnlySpan<char> innerSpan = tokenSpan[..semicolonIndex];
                format = innerSpan.IsEmpty ? null : innerSpan.ToString();

                try
                {
                    culture = CultureInfo.GetCultureInfo(tokenSpan[(semicolonIndex + 1)..].ToString());
                }
                catch (CultureNotFoundException exception)
                {
                    throw new FormatException("Culture not found", exception);
                }
            }
        }

        if (format is not null)
        {
            try
            {
                _ = DateTime.UtcNow.ToString(format);
            }
            catch (FormatException)
            {
                throw new FormatException("Invalid timestamp format");
            }
        }

        return new TimestampToken() { format = format, Culture = culture };
    }
}
