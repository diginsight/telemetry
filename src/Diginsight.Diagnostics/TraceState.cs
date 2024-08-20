using System.Collections;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Entry = System.Collections.Generic.KeyValuePair<Diginsight.Diagnostics.TraceStateKey, string>;
#if NET
using System.Diagnostics.CodeAnalysis;
#endif

namespace Diginsight.Diagnostics;

public sealed class TraceState : IDictionary<TraceStateKey, string>, IReadOnlyDictionary<TraceStateKey, string>
{
    private static readonly Regex SimpleKeyRegex = new (@"^([a-z][a-z0-9_\-*/]{0,255})=");
    private static readonly Regex ComplexKeyRegex = new (@"^([a-z0-9][a-z0-9_\-*/]{0,240})@([a-z0-9_\-*/]{0,13})=");
    private static readonly Regex ValueRegex = new (@"^[\x20-\x7e-[,=]]{0,255}[\x21-\x7e-[,=]]");

    private readonly IList<Entry> items = new List<Entry>();

    public int Count => items.Count;

    public bool IsReadOnly => false;

    ICollection<TraceStateKey> IDictionary<TraceStateKey, string>.Keys => KeysCore;

    public IEnumerable<TraceStateKey> Keys => KeysCore;

    private ICollection<TraceStateKey> KeysCore => items.Select(static x => x.Key).ToArray();

    ICollection<string> IDictionary<TraceStateKey, string>.Values => ValuesCore;

    public IEnumerable<string> Values => ValuesCore;

    private ICollection<string> ValuesCore => items.Select(static x => x.Value).ToArray();

    public string this[TraceStateKey key]
    {
        get => TryGetValue(key, out string? value) ? value : throw new KeyNotFoundException($"No such key '{key}'");
        set
        {
            ValidateValue(value);
            _ = Remove(key);
            items.Insert(0, new Entry(key, value));
        }
    }

    public bool Contains(Entry item) => items.Contains(item);

    public bool ContainsKey(TraceStateKey key) => items.Any(x => key == x.Key);

    public IEnumerator<Entry> GetEnumerator() => items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool TryGetValue(
        TraceStateKey key,
#if NET
        [MaybeNullWhen(false)]
#endif
        out string value
    )
    {
        if (items.Where(x => key == x.Key).Take(1).ToArray() is [ var (_, value0) ])
        {
            value = value0;
            return true;
        }
        else
        {
#if NET
            value = null;
#else
            value = null!;
#endif
            return false;
        }
    }

    public void CopyTo(Entry[] array, int arrayIndex) => items.CopyTo(array, arrayIndex);

    public void Add(Entry item)
    {
        if (Contains(item))
            throw new ArgumentException($"Key '{item.Key}' already exists");

        ValidateValue(item.Value);
        items.Insert(0, item);
    }

    public void Add(TraceStateKey key, string value)
    {
        if (ContainsKey(key))
            throw new ArgumentException($"Key '{key}' already exists");

        ValidateValue(value);
        items.Insert(0, new Entry(key, value));
    }

    public bool Remove(Entry item) => items.Remove(item);

    public bool Remove(TraceStateKey key)
    {
        return TryGetValue(key, out string? value) && items.Remove(new Entry(key, value));
    }

    public void Clear() => items.Clear();

    public override string ToString()
    {
        return string.Join
        (
#if NET || NETSTANDARD2_1_OR_GREATER
            ',',
#else
            ",",
#endif
            items.Select(static x => $"{x.Key}={x.Value}")
        );
    }

    private static void ValidateValue(string value)
    {
        static bool IsValid(char ch, bool space)
        {
            return ch is >= '\x21' and <= '\x7e' and not (',' or '=')
                || space && ch == ' ';
        }

        if (value is null)
            throw new ArgumentNullException("Invalid tracestate value", (Exception?)null);

        int length = value.Length;

        if (length is < 1 or > 256)
            throw new ArgumentException("Invalid tracestate value length");

        for (int i = 0; i < length - 1; i++)
        {
            if (!IsValid(value[i], true))
                throw new ArgumentException("Invalid tracestate value character");
        }

        if (!IsValid(value[length - 1], false))
            throw new ArgumentException("Invalid tracestate value character");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TraceState Parse(string? str) => string.IsNullOrWhiteSpace(str) ? new TraceState() : Parse(str.AsSpan());

    public static TraceState Parse(ReadOnlySpan<char> span)
    {
        TraceState result = new ();

        for (int index = 0;;)
        {
            static void Advance(ref ReadOnlySpan<char> span, ref int index, int offset)
            {
                span = span[offset..];
                index += offset;
            }

            while (!span.IsEmpty && span[0] is ' ' or '\t')
            {
                Advance(ref span, ref index, 1);
            }

            if (span.IsEmpty)
            {
                return result;
            }

            if (span[0] == ',')
            {
                Advance(ref span, ref index, 1);
                continue;
            }

            string? keyTenantId;
            string keySystemId;

#if NET || NETSTANDARD2_1_OR_GREATER
            string tmp = new (span);
#else
            string tmp = new (span.ToArray());
#endif
            if (SimpleKeyRegex.Match(tmp) is { Success: true } simpleMatch)
            {
                keyTenantId = null;
                keySystemId = simpleMatch.Groups[1].Value;
                Advance(ref span, ref index, simpleMatch.Length);
            }
            else if (ComplexKeyRegex.Match(tmp) is { Success: true } complexMatch)
            {
                keyTenantId = complexMatch.Groups[1].Value;
                keySystemId = complexMatch.Groups[2].Value;
                Advance(ref span, ref index, complexMatch.Length);
            }
            else
            {
                throw new FormatException($"Invalid tracestate key at index {index}");
            }

#if NET || NETSTANDARD2_1_OR_GREATER
            tmp = new string(span);
#else
            tmp = new string(span.ToArray());
#endif
            if (ValueRegex.Match(tmp) is not { Success: true } valueMatch)
            {
                throw new FormatException($"Invalid tracestate value at index {index}");
            }

            string value = valueMatch.Value;
            Advance(ref span, ref index, valueMatch.Length);

            result.items.Add(new Entry(new TraceStateKey(keyTenantId, keySystemId, false), value));
        }
    }
}
