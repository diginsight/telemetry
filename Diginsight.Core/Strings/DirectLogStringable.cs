using System.Globalization;

namespace Diginsight.Strings;

public sealed class DirectLogStringable : ILogStringable
{
    private readonly object obj;
    private readonly string? format;

    bool ILogStringable.IsDeep => false;
    object? ILogStringable.Subject => null;

    public DirectLogStringable(object obj, string? format = null)
    {
        this.obj = obj;
        this.format = format;
    }

    public void AppendTo(AppendingContext appendingContext)
    {
        appendingContext.AppendDirect(
            sb =>
            {
                if (format is null)
                    sb.Append(obj);
                else
                    sb.AppendFormat(CultureInfo.InvariantCulture, format, obj);
            }
        );
    }
}
