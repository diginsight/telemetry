using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Diginsight.Strings;

internal sealed class PrimitiveLogStringProvider : ILogStringProvider
{
    private static readonly string PTR_FORMAT = $"^{{0:X{IntPtr.Size}}}";
    private static readonly IDictionary<Type, (Enum[] Values, Enum Zero)> EnumCache = new Dictionary<Type, (Enum[] Values, Enum Zero)>();

    public ILogStringable? TryAsLogStringable(object obj)
    {
        return obj switch
        {
            string => new DirectLogStringable(obj, "\"{0}\""),
            bool => new DirectLogStringable(obj),
            char => new DirectLogStringable(obj, "'{0}'"),
            byte or sbyte => new DirectLogStringable(obj, "#{0:X2}"),
            short or ushort or int or uint or long or ulong or float or double or decimal => new LogStringableConvertible((IConvertible)obj),
            IntPtr or UIntPtr => new DirectLogStringable(obj, PTR_FORMAT),
            Enum e => e.GetType().IsDefined(typeof(FlagsAttribute)) ? new LogStringableFlaggedEnum(e) : new LogStringableConvertible(e),
            _ => null,
        };
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

        public void AppendTo(AppendingContext appendingContext)
        {
            appendingContext.AppendDirect(convertible.ToString(CultureInfo.InvariantCulture));
        }
    }

    private sealed class LogStringableFlaggedEnum : ILogStringable
    {
        private readonly Enum @enum;

        public bool IsDeep => false;
        public bool CanCycle => false;

        public LogStringableFlaggedEnum(Enum @enum)
        {
            this.@enum = @enum;
        }

        public void AppendTo(AppendingContext appendingContext)
        {
            Type enumType = @enum.GetType();

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

            Enum[] flaggedValues = values.Where(@enum.HasFlag).DefaultIfEmpty(zero).ToArray();
            IEnumerable<Enum> skimmedFlaggedValues = flaggedValues.Where(x => flaggedValues.All(y => x.Equals(y) || !y.HasFlag(x)));

            using IEnumerator<Enum> enumerator = skimmedFlaggedValues.GetEnumerator();
            appendingContext.AppendEnumerator(
                enumerator,
                static (ac, e) => { ac.AppendDirect(e.Current.ToString()); },
                AllottingCounter.Unlimited,
                "|"
            );
        }
    }
}
