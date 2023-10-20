using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace Diginsight.Strings;

internal sealed class PrimitiveLogStringProvider : ILogStringProvider
{
    private static readonly string PTR_FORMAT = $"^{{0:X{IntPtr.Size}}}";
    private static readonly IDictionary<Type, (Enum[] Values, Enum Zero)> EnumCache = new Dictionary<Type, (Enum[] Values, Enum Zero)>();

    public bool TryAsLoggable(object obj, [NotNullWhen(true)] out ILoggable? loggable)
    {
        switch (obj)
        {
            case string:
                loggable = new LoggableDirect(obj, "\"{0}\"");
                return true;

            case bool:
                loggable = new LoggableDirect(obj);
                return true;

            case char:
                loggable = new LoggableDirect(obj, "'{0}'");
                return true;

            case byte or sbyte:
                loggable = new LoggableDirect(obj, "#{0:X2}");
                return true;

            case short or ushort or int or uint or long or ulong or float or double or decimal:
                loggable = new LoggableConvertible((IConvertible)obj);
                return true;

            case IntPtr or UIntPtr:
                loggable = new LoggableDirect(obj, PTR_FORMAT);
                return true;

            case Enum e:
                loggable = e.GetType().IsDefined(typeof(FlagsAttribute)) ? new LoggableFlaggedEnum(e) : new LoggableConvertible(e);
                return true;
        }

        loggable = null;
        return false;
    }

    private sealed class LoggableConvertible : ILoggable
    {
        private readonly IConvertible convertible;

        public bool IsDeep => false;
        public bool CanCycle => false;

        public LoggableConvertible(IConvertible convertible)
        {
            this.convertible = convertible;
        }

        public void AppendTo(StringBuilder stringBuilder, LoggingContext loggingContext)
        {
            stringBuilder.Append(convertible.ToString(CultureInfo.InvariantCulture));
        }
    }

    private sealed class LoggableFlaggedEnum : ILoggable
    {
        private readonly Enum e;

        public bool IsDeep => false;
        public bool CanCycle => false;

        public LoggableFlaggedEnum(Enum e)
        {
            this.e = e;
        }

        public void AppendTo(StringBuilder stringBuilder, LoggingContext loggingContext)
        {
            Type enumType = e.GetType();

            Enum[] values;
            Enum zero;
            lock (((ICollection)EnumCache).SyncRoot)
            {
                (Enum[] Values, Enum Zero) ValuesAndZeroCore()
                {
                    Enum z = (Enum)Enum.ToObject(enumType, 0);
                    return (Enum.GetValues(enumType).Cast<Enum>().Where(x => !z.Equals(x)).ToArray(), z);
                }

                if (!EnumCache.TryGetValue(enumType, out var valuesAndZero))
                {
                    EnumCache[enumType] = valuesAndZero = ValuesAndZeroCore();
                }

                (values, zero) = valuesAndZero;
            }

            Enum[] flaggedValues = values.Where(x => e.HasFlag(x)).DefaultIfEmpty(zero).ToArray();
            Enum[] skimmedFlaggedValues = flaggedValues.Where(x => flaggedValues.All(y => x.Equals(y) || !y.HasFlag(x))).ToArray();

#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            stringBuilder.AppendJoin('|', (IEnumerable<Enum>)skimmedFlaggedValues);
#else
            stringBuilder.Append(skimmedFlaggedValues[0]);
            foreach (Enum skimmedFlaggedValue in skimmedFlaggedValues.Skip(1))
            {
                stringBuilder.Append('|');
                stringBuilder.Append(skimmedFlaggedValue);
            }
#endif
        }
    }
}
