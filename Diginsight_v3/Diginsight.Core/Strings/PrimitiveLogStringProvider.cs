using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Diginsight.Strings;

internal sealed class PrimitiveLogStringProvider : ILogStringProvider
{
    private static readonly string PTR_FORMAT = $"^{{0:X{IntPtr.Size}}}";
    private static readonly IDictionary<Type, (Enum[] Values, Enum Zero)> EnumCache = new Dictionary<Type, (Enum[] Values, Enum Zero)>();

    public bool TryAsLogStringable(object obj, [NotNullWhen(true)] out ILogStringable? logStringable)
    {
        switch (obj)
        {
            case string:
                logStringable = new LogStringableDirect(obj, "\"{0}\"");
                return true;

            case bool:
                logStringable = new LogStringableDirect(obj);
                return true;

            case char:
                logStringable = new LogStringableDirect(obj, "'{0}'");
                return true;

            case byte or sbyte:
                logStringable = new LogStringableDirect(obj, "#{0:X2}");
                return true;

            case short or ushort or int or uint or long or ulong or float or double or decimal:
                logStringable = new LogStringableConvertible((IConvertible)obj);
                return true;

            case IntPtr or UIntPtr:
                logStringable = new LogStringableDirect(obj, PTR_FORMAT);
                return true;

            case Enum e:
                logStringable = e.GetType().IsDefined(typeof(FlagsAttribute)) ? new LogStringableFlaggedEnum(e) : new LogStringableConvertible(e);
                return true;
        }

        logStringable = null;
        return false;
    }

    private sealed class LogStringableConvertible : ILogStringable
    {
        private readonly IConvertible convertible;

        public bool IsDeep => false;
        public bool CanCycle => false;

        public LogStringableConvertible(IConvertible convertible)
        {
            this.convertible = convertible;
        }

        public void AppendTo(StringBuilder stringBuilder, LoggingContext loggingContext)
        {
            stringBuilder.Append(convertible.ToString(CultureInfo.InvariantCulture));
        }
    }

    private sealed class LogStringableFlaggedEnum : ILogStringable
    {
        private readonly Enum e;

        public bool IsDeep => false;
        public bool CanCycle => false;

        public LogStringableFlaggedEnum(Enum e)
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
                (values, zero) = EnumCache.TryGetValue(enumType, out var valuesAndZero)
                    ? valuesAndZero
                    : EnumCache[enumType] = ValuesAndZeroCore();

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                (Enum[] Values, Enum Zero) ValuesAndZeroCore()
                {
                    Enum z = (Enum)Enum.ToObject(enumType, 0);
                    return (Enum.GetValues(enumType).Cast<Enum>().Where(x => !z.Equals(x)).ToArray(), z);
                }
            }

            Enum[] flaggedValues = values.Where(e.HasFlag).DefaultIfEmpty(zero).ToArray();
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
