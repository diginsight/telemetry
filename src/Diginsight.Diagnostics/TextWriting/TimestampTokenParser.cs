using System.Globalization;

namespace Diginsight.Diagnostics.TextWriting;

internal sealed class TimestampTokenParser : ILineTokenParser
{
    public string TokenName => "timestamp";

    public ILineToken Parse(ReadOnlySpan<char> tokenDetailSpan)
    {
        string? format;
        CultureInfo? culture;

        if (tokenDetailSpan.IsEmpty)
        {
            format = null;
            culture = null;
        }
        else
        {
            if (tokenDetailSpan[0] != '|')
            {
                throw new FormatException("Expected '|' or nothing");
            }

            tokenDetailSpan = tokenDetailSpan[1..];
            int separatorIndex = tokenDetailSpan.LastIndexOf('|');
            if (separatorIndex < 0)
            {
                format = tokenDetailSpan.ToString();
                culture = null;
            }
            else
            {
                ReadOnlySpan<char> innerSpan = tokenDetailSpan[..separatorIndex];
                format = innerSpan.IsEmpty ? null : innerSpan.ToString();

                try
                {
                    culture = CultureInfo.GetCultureInfo(tokenDetailSpan[(separatorIndex + 1)..].ToString());
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

        return new TimestampToken() { FormatUnsafe = format, Culture = culture };
    }
}
