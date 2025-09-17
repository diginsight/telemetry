using System.Globalization;

namespace Diginsight.Stringify;

public sealed class DirectStringifiable : IStringifiable
{
    private readonly object obj;
    private readonly string? format;
    private readonly IFormatProvider? formatProvider;

    bool IStringifiable.IsDeep => false;
    object? IStringifiable.Subject => null;

    public DirectStringifiable(object obj, string? format = null, IFormatProvider? formatProvider = null)
    {
        this.obj = obj;
        this.format = format;
        this.formatProvider = formatProvider;
    }

    public void AppendTo(StringifyContext stringifyContext)
    {
        stringifyContext.AppendDirect(
            sb =>
            {
                if (format is null)
                    sb.Append(obj);
                else
                    sb.AppendFormat(formatProvider ?? CultureInfo.InvariantCulture, format, obj);
            }
        );
    }
}
