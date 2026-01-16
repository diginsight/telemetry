using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Diginsight.Stringify;

internal sealed class PrimitiveStringifier : IStringifier
{
    private static readonly IDictionary<Type, (Enum[] Values, Enum Zero)> EnumCache = new Dictionary<Type, (Enum[] Values, Enum Zero)>();

    public IStringifiable? TryStringify(object obj)
    {
        return obj switch
        {
            string s => new StringifiableString(s),
            bool => new DirectStringifiable(obj),
            char => new DirectStringifiable(obj, "'{0}'"),
            byte or sbyte => new DirectStringifiable(obj, "#{0:X2}"),
            short or ushort or int or uint or long or ulong or float or double or decimal => new StringifiableConvertible((IConvertible)obj),
            IntPtr or UIntPtr => new DirectStringifiable(obj, $"^{{0:X{IntPtr.Size}}}"),
            Enum e => e.GetType().IsDefined(typeof(FlagsAttribute)) ? new StringifiableFlaggedEnum(e) : new StringifiableConvertible(e),
            _ => null,
        };
    }

    private sealed class StringifiableString : IStringifiable
    {
        private readonly string str;

        bool IStringifiable.IsDeep => false;
        object? IStringifiable.Subject => null;

        public StringifiableString(string str)
        {
            this.str = str;
        }

        public void AppendTo(StringifyContext stringifyContext)
        {
            stringifyContext.AppendDirect('"');
            if (stringifyContext.VariableConfiguration.EffectiveMaxStringLength is { } length && str.Length > length)
            {
                stringifyContext.AppendDirect(str[..length]);
                stringifyContext.AppendEllipsis();
            }
            else
            {
                stringifyContext.AppendDirect(str);
            }
            stringifyContext.AppendDirect('"');
        }
    }

    private sealed class StringifiableConvertible : IStringifiable
    {
        private readonly IConvertible convertible;

        bool IStringifiable.IsDeep => false;
        object IStringifiable.Subject => convertible;

        public StringifiableConvertible(IConvertible convertible)
        {
            this.convertible = convertible;
        }

        public void AppendTo(StringifyContext stringifyContext)
        {
            stringifyContext.AppendDirect(convertible.ToStringInvariant());
        }
    }

    private sealed class StringifiableFlaggedEnum : IStringifiable
    {
        private readonly Enum @enum;

        bool IStringifiable.IsDeep => false;
        object? IStringifiable.Subject => null;

        public StringifiableFlaggedEnum(Enum @enum)
        {
            this.@enum = @enum;
        }

        public void AppendTo(StringifyContext stringifyContext)
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
            stringifyContext.AppendEnumerator(
                enumerator,
                static (sc, e) => { sc.AppendDirect(e.Current.ToString()); },
                AllottedCounter.Unlimited,
                "|"
            );
        }
    }
}
