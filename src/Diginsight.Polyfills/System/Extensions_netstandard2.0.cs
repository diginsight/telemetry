#if !(NET || NETSTANDARD2_1_OR_GREATER)
namespace System;

public static partial class Extensions
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
}
#endif
