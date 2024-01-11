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

    public void Apply(ref LineDescriptor lineDescriptor)
    {
        lineDescriptor.CustomAppenders.Add(new TimestampAppender(Format, Culture));
    }
}
