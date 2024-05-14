using System.ComponentModel;
using System.Globalization;

namespace Diginsight;

internal sealed class ExpirationConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

#if NET
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
#else
    public override object? ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object? value)
#endif
    {
        return value is string s ? Expiration.Parse(s, culture) : base.ConvertFrom(context, culture, value);
    }
}
