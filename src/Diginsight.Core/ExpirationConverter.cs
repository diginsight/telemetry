using System.ComponentModel;
using System.Globalization;

namespace Diginsight;

internal sealed class ExpirationConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override object? ConvertFrom(
#if NET
        ITypeDescriptorContext? context, CultureInfo? culture, object value
#else
        ITypeDescriptorContext context, CultureInfo culture, object? value
#endif
    )
    {
        return value is string s ? Expiration.Parse(s, culture) : base.ConvertFrom(context, culture, value);
    }
}
