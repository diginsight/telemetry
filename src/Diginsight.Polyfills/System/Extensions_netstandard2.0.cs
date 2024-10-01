#if !(NET || NETSTANDARD2_1_OR_GREATER)
namespace System;

public static partial class Extensions
{
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
}
#endif
