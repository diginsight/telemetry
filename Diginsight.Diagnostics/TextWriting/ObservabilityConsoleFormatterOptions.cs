using Microsoft.Extensions.Logging.Console;
using System.Collections;
using MaybeInt = System.ValueTuple<int?>;

namespace Diginsight.Diagnostics.TextWriting;

public sealed class ObservabilityConsoleFormatterOptions : ConsoleFormatterOptions, IObservabilityTextWritingOptions
{
    private readonly object lockObj = new ();
    private readonly IDictionary<MaybeInt, LineDescriptor> descriptorCache = new Dictionary<MaybeInt, LineDescriptor>();

    private string? pattern;
    private IDictionary<int, IEnumerable<ILineToken>?>? lineTokensCache;

    public string? Pattern
    {
        get => pattern;
        set
        {
            value = value.HardTrim();
            if (pattern == value)
            {
                return;
            }

            lock (lockObj)
            {
                pattern = value;
                ResetCaches();
            }
        }
    }

    public IDictionary<string, string?> Patterns { get; }

    public ObservabilityConsoleFormatterOptions()
    {
        UseUtcTimestamp = true;
        Patterns = new PatternDictionary(this);
    }

    public LineDescriptor GetLineDescriptor(int? width)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Must be positive");
        }

        lock (lockObj)
        {
            MaybeInt descriptorKey = new (width);
            if (!descriptorCache.TryGetValue(descriptorKey, out LineDescriptor descriptor))
            {
                descriptorCache[descriptorKey] = descriptor = MakeLineDescriptor();

                LineDescriptor MakeLineDescriptor()
                {
                    if (lineTokensCache is null)
                    {
                        lineTokensCache = (Pattern, Patterns.Count) switch
                        {
                            (not null, > 0) => throw new InvalidOperationException($"Cannot specify both {nameof(Pattern)} and {nameof(Patterns)}"),
                            (null, > 0) => Patterns.Keys
                                .Select(static x => int.TryParse(x, out int n) && n > 0 ? n : throw new InvalidOperationException("Pattern keys must be positive integers"))
                                .ToDictionary(static n => n, static _ => (IEnumerable<ILineToken>?)null),
                            _ => new Dictionary<int, IEnumerable<ILineToken>?>() { [1] = null },
                        };

                        if (!lineTokensCache.ContainsKey(1))
                        {
                            throw new InvalidOperationException("Pattern keys must include '1'");
                        }
                    }

                    int finalWidth = width ?? int.MaxValue;
                    int targetWidth = lineTokensCache.Keys.Where(x => x <= finalWidth).Max();

                    if (lineTokensCache[targetWidth] is not { } lineTokens)
                    {
                        string? selectedPattern = Patterns.Any() ? Patterns[targetWidth.ToStringInvariant()] : Pattern;
                        lineTokensCache[targetWidth] = lineTokens = LineDescriptor.Parse(selectedPattern);
                    }

                    IList<ILineToken> finalLineTokens = new List<ILineToken>(lineTokens.Select(static x => x.Clone()));

                    if (TimestampFormat is { } timestampFormat && finalLineTokens.OfType<TimestampToken>().FirstOrDefault() is { Format: null } timestampToken)
                    {
                        timestampToken.Format = timestampFormat;
                    }

                    if (width is { } maxLineLength)
                    {
                        if (finalLineTokens is [ .., MessageToken messageToken ])
                        {
                            messageToken.MaxLineLength = maxLineLength;
                        }
                        else
                        {
                            finalLineTokens.Add(new MessageToken() { MaxLineLength = maxLineLength });
                        }
                    }

                    return new LineDescriptor(finalLineTokens);
                }
            }

            return descriptor;
        }
    }

    private void ResetCaches()
    {
        lineTokensCache = null;
        descriptorCache.Clear();
    }

    private sealed class PatternDictionary : IDictionary<string, string?>
    {
        private readonly IDictionary<string, string?> underlying = new Dictionary<string, string?>();
        private readonly ObservabilityConsoleFormatterOptions owner;

        public int Count => underlying.Count;
        public bool IsReadOnly => underlying.IsReadOnly;
        public ICollection<string> Keys => underlying.Keys;
        public ICollection<string?> Values => underlying.Values;

        public string? this[string key]
        {
            get => underlying[key];
            set
            {
                lock (owner.lockObj)
                {
                    underlying[key] = value.HardTrim();
                    owner.ResetCaches();
                }
            }
        }

        public PatternDictionary(ObservabilityConsoleFormatterOptions owner)
        {
            this.owner = owner;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerator<KeyValuePair<string, string?>> GetEnumerator() => underlying.GetEnumerator();

        public void Add(KeyValuePair<string, string?> item)
        {
            lock (owner.lockObj)
            {
                underlying.Add(new KeyValuePair<string, string?>(item.Key, item.Value.HardTrim()));
                owner.ResetCaches();
            }
        }

        public void Clear()
        {
            lock (owner.lockObj)
            {
                underlying.Clear();
                owner.ResetCaches();
            }
        }

        public bool Contains(KeyValuePair<string, string?> item) => underlying.Contains(item);

        public void CopyTo(KeyValuePair<string, string?>[] array, int arrayIndex) => underlying.CopyTo(array, arrayIndex);

        public bool Remove(KeyValuePair<string, string?> item)
        {
            lock (owner.lockObj)
            {
                bool result = underlying.Remove(item);
                owner.ResetCaches();
                return result;
            }
        }

        public void Add(string key, string? value)
        {
            lock (owner.lockObj)
            {
                underlying.Add(key, value.HardTrim());
                owner.ResetCaches();
            }
        }

        public bool ContainsKey(string key) => underlying.ContainsKey(key);

        public bool Remove(string key)
        {
            lock (owner.lockObj)
            {
                bool result = underlying.Remove(key);
                owner.ResetCaches();
                return result;
            }
        }

        public bool TryGetValue(string key, out string? value) => underlying.TryGetValue(key, out value);
    }
}
