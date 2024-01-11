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

#if NET8_0_OR_GREATER
            CompositeFormat = CompositeFormat.Parse($"{{0:{format ?? "'['yyyy-MM-dd'T'HH:mm:ss.fff']'"}}}");
#endif
        }
    }

#if NET8_0_OR_GREATER
    public CompositeFormat? CompositeFormat { get; private set; }
#endif

    public CultureInfo? Culture { get; set; }
}
