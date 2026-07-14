#if !(NET || NETSTANDARD2_1_OR_GREATER)
using System.ComponentModel;
using System.Diagnostics;

namespace System;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class Extensions
{
    public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> kvp, out TKey key, out TValue value)
    {
        key = kvp.Key;
        value = kvp.Value;
    }

    extension<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> dictionary)
        where TKey : notnull
    {
        public TValue? GetValueOrDefault(TKey key)
        {
            return dictionary.GetValueOrDefault(key, default);
        }

        public TValue? GetValueOrDefault(TKey key, TValue? defaultValue)
        {
            return dictionary.TryGetValue(key, out TValue obj) ? obj : defaultValue;
        }
    }

    extension(StringComparer)
    {
        public static StringComparer FromComparison(StringComparison comparison)
        {
            return comparison switch
            {
                StringComparison.CurrentCulture => StringComparer.CurrentCulture,
                StringComparison.CurrentCultureIgnoreCase => StringComparer.CurrentCultureIgnoreCase,
                StringComparison.InvariantCulture => StringComparer.InvariantCulture,
                StringComparison.InvariantCultureIgnoreCase => StringComparer.InvariantCultureIgnoreCase,
                StringComparison.Ordinal => StringComparer.Ordinal,
                StringComparison.OrdinalIgnoreCase => StringComparer.OrdinalIgnoreCase,
                _ => throw new UnreachableException($"unrecognized {nameof(StringComparison)}"),
            };
        }
    }
}
#endif
