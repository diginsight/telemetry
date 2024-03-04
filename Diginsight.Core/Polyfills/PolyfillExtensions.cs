#if !NET6_0_OR_GREATER
using System.Collections;
#endif

using System.ComponentModel;

[EditorBrowsable(EditorBrowsableState.Never)]
// ReSharper disable once CheckNamespace
public static class PolyfillExtensions
{
#if !(NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
    public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> kvp, out TKey key, out TValue value)
    {
        key = kvp.Key;
        value = kvp.Value;
    }

    public static TValue? GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key)
        where TKey : notnull
    {
        return dictionary.GetValueOrDefault(key, default);
    }

    public static TValue? GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key, TValue? defaultValue)
        where TKey : notnull
    {
        return dictionary.TryGetValue(key, out TValue obj) ? obj : defaultValue;
    }
#endif

#if !NET6_0_OR_GREATER
    public static bool TryGetNonEnumeratedCount<TSource>(this IEnumerable<TSource>? source, out int count)
    {
        switch (source)
        {
            case null:
                throw new ArgumentNullException(nameof(source));

            case ICollection<TSource> coll:
                count = coll.Count;
                return true;

            case ICollection coll:
                count = coll.Count;
                return true;

            default:
                count = 0;
                return false;
        }
    }
#endif
}
