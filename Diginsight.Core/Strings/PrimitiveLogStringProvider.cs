using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Diginsight.Strings;

internal sealed class PrimitiveLogStringProvider : ILogStringProvider
{
    private static readonly IDictionary<Type, (Enum[] Values, Enum Zero)> EnumCache = new Dictionary<Type, (Enum[] Values, Enum Zero)>();

    public ILogStringable? TryToLogStringable(object obj)
    {
        return obj switch
        {
            string s => new LogStringableString(s),
            bool => new DirectLogStringable(obj),
            char => new DirectLogStringable(obj, "'{0}'"),
            byte or sbyte => new DirectLogStringable(obj, "#{0:X2}"),
            short or ushort or int or uint or long or ulong or float or double or decimal => new LogStringableConvertible((IConvertible)obj),
            IntPtr or UIntPtr => new DirectLogStringable(obj, $"^{{0:X{IntPtr.Size}}}"),
            Enum e => e.GetType().IsDefined(typeof(FlagsAttribute)) ? new LogStringableFlaggedEnum(e) : new LogStringableConvertible(e),
            _ => null,
        };
    }

    private sealed class LogStringableString : ILogStringable
    {
        private readonly string str;

        bool ILogStringable.IsDeep => false;
        object? ILogStringable.Subject => null;

        public LogStringableString(string str)
        {
            this.str = str;
        }

        public void AppendTo(AppendingContext appendingContext)
        {
            appendingContext.AppendDirect('"');
            if (appendingContext.VariableConfiguration.GetEffectiveMaxStringLength() is { } length && str.Length > length)
            {
                appendingContext.AppendDirect(str[..length]);
                appendingContext.AppendEllipsis();
            }
            else
            {
                appendingContext.AppendDirect(str);
            }
            appendingContext.AppendDirect('"');
        }
    }

    private sealed class LogStringableConvertible : ILogStringable
    {
        private readonly IConvertible convertible;

        bool ILogStringable.IsDeep => false;
        object ILogStringable.Subject => convertible;

        public LogStringableConvertible(IConvertible convertible)
        {
            this.convertible = convertible;
        }

        public void AppendTo(AppendingContext appendingContext)
        {
            appendingContext.AppendDirect(convertible.ToStringInvariant());
        }
    }

    private sealed class LogStringableFlaggedEnum : ILogStringable
    {
        private readonly Enum @enum;

        bool ILogStringable.IsDeep => false;
        object? ILogStringable.Subject => null;

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
