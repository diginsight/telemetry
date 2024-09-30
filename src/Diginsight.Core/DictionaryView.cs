using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Diginsight;

[SuppressMessage("ReSharper", "InlineTemporaryVariable")]
public sealed class DictionaryView<TKeyIn, TValueIn, TKeyOut, TValueOut> : IReadOnlyDictionary<TKeyOut, TValueOut>
{
    private readonly IReadOnlyDictionary<TKeyIn, TValueIn>? dictionary1;
    private readonly IDictionary<TKeyIn, TValueIn>? dictionary2;

    private readonly Func<TKeyIn, TKeyOut> convertKey;
    private readonly Func<TKeyOut, TKeyIn> convertBackKey;
    private readonly Func<TValueIn, TValueOut> convertValue;

    public int Count => dictionary1 is { } dictionary ? dictionary.Count : dictionary2!.Count;

    public IEnumerable<TKeyOut> Keys => (dictionary1 is { } dictionary ? dictionary.Keys : dictionary2!.Keys).Select(convertKey);

    public IEnumerable<TValueOut> Values => (dictionary1 is { } dictionary ? dictionary.Values : dictionary2!.Values).Select(convertValue);

    public TValueOut this[TKeyOut key]
    {
        get
        {
            TKeyIn innerKey = convertBackKey(key);
            return convertValue(dictionary1 is { } dictionary ? dictionary[innerKey] : dictionary2![innerKey]);
        }
    }

    public DictionaryView(
        IReadOnlyDictionary<TKeyIn, TValueIn> dictionary,
        Func<TKeyIn, TKeyOut> convertKey,
        Func<TKeyOut, TKeyIn> convertBackKey,
        Func<TValueIn, TValueOut> convertValue
    )
        : this(dictionary ?? throw new ArgumentNullException(nameof(dictionary)), null, convertKey, convertBackKey, convertValue) { }

    public DictionaryView(
        IDictionary<TKeyIn, TValueIn> dictionary,
        Func<TKeyIn, TKeyOut> convertKey,
        Func<TKeyOut, TKeyIn> convertBackKey,
        Func<TValueIn, TValueOut> convertValue
    )
        : this(null, dictionary ?? throw new ArgumentNullException(nameof(dictionary)), convertKey, convertBackKey, convertValue) { }

    private DictionaryView(
        IReadOnlyDictionary<TKeyIn, TValueIn>? dictionary1,
        IDictionary<TKeyIn, TValueIn>? dictionary2,
        Func<TKeyIn, TKeyOut> convertKey,
        Func<TKeyOut, TKeyIn> convertBackKey,
        Func<TValueIn, TValueOut> convertValue
    )
    {
        this.dictionary1 = dictionary1;
        this.dictionary2 = dictionary2;
        this.convertKey = convertKey ?? throw new ArgumentNullException(nameof(convertKey));
        this.convertBackKey = convertBackKey ?? throw new ArgumentNullException(nameof(convertBackKey));
        this.convertValue = convertValue ?? throw new ArgumentNullException(nameof(convertValue));
    }

    public bool ContainsKey(TKeyOut key)
    {
        TKeyIn innerKey = convertBackKey(key);
        return dictionary1 is { } dictionary ? dictionary.ContainsKey(innerKey) : dictionary2!.ContainsKey(innerKey);
    }

#if NET
    public bool TryGetValue(TKeyOut key, [MaybeNullWhen(false)] out TValueOut value)
#else
    public bool TryGetValue(TKeyOut key, out TValueOut value)
#endif
    {
        TKeyIn innerKey = convertBackKey(key);

        bool result = dictionary1 is { } dictionary
            ? dictionary.TryGetValue(innerKey, out TValueIn? innerValue)
            : dictionary2!.TryGetValue(innerKey, out innerValue);

        value = result
            ? convertValue(innerValue!)
#if NET
            : default;
#else
            : default!;
#endif
        return result;
    }

    public IEnumerator<KeyValuePair<TKeyOut, TValueOut>> GetEnumerator()
    {
        return (dictionary1 is { } dictionary
                ? dictionary.Select(x => new KeyValuePair<TKeyOut, TValueOut>(convertKey(x.Key), convertValue(x.Value)))
                : dictionary2!.Select(x => new KeyValuePair<TKeyOut, TValueOut>(convertKey(x.Key), convertValue(x.Value))))
            .GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
