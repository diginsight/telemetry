using System.Globalization;

namespace Diginsight.Stringify;

/// <summary>
/// Represents a stringifiable value that appends a direct formatted representation.
/// </summary>
public sealed class DirectStringifiable : IStringifiable
{
    private readonly object obj;
    private readonly string? format;
    private readonly IFormatProvider? formatProvider;

    bool IStringifiable.IsDeep => false;
    object? IStringifiable.Subject => null;

    /// <summary>
    /// Initializes a new instance of the <see cref="DirectStringifiable" /> class.
    /// </summary>
    /// <param name="obj">The object to stringify.</param>
    /// <param name="format">The composite format string.</param>
    /// <param name="formatProvider">The format provider.</param>
    public DirectStringifiable(object obj, string? format = null, IFormatProvider? formatProvider = null)
    {
        this.obj = obj;
        this.format = format;
        this.formatProvider = formatProvider;
    }

    /// <inheritdoc />
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
