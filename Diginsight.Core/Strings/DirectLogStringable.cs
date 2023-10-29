using System.Globalization;
using System.Text;

namespace Diginsight.Strings;

public sealed class DirectLogStringable : ILogStringable
{
    private readonly object obj;
    private readonly string? format;

    public bool IsDeep => false;
    public bool CanCycle => false;

    public DirectLogStringable(object obj, string? format = null)
    {
        this.obj = obj;
        this.format = format;
    }

    public void AppendTo(StringBuilder stringBuilder, AppendingContext appendingContext)
    {
        if (format is null)
            stringBuilder.Append(obj);
        else
            stringBuilder.AppendFormat(CultureInfo.InvariantCulture, format, obj);
    }
}
