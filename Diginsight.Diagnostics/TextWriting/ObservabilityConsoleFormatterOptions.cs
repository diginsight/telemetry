using Microsoft.Extensions.Logging.Console;
using System.Collections;

namespace Diginsight.Diagnostics.TextWriting;

public sealed class ObservabilityConsoleFormatterOptions : ConsoleFormatterOptions, IObservabilityTextWritingOptions
{
    private readonly object lockObj = new ();

    private string? pattern;
    private IDictionary<int, LineDescriptor?>? descriptorCache;

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
                descriptorCache = null;
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
            if (descriptorCache is null)
            {
                descriptorCache = (Pattern, Patterns.Count) switch
                {
                    (not null, > 0) => throw new InvalidOperationException($"Cannot specify both {nameof(Pattern)} and {nameof(Patterns)}"),
                    (null, > 0) => Patterns.Keys
                        .Select(static x => int.TryParse(x, out int n) && n > 0 ? n : throw new InvalidOperationException("Pattern keys must be positive integers"))
                        .ToDictionary(static n => n, static _ => (LineDescriptor?)null),
                    _ => new Dictionary<int, LineDescriptor?>() { [1] = null },
                };

                if (!descriptorCache.ContainsKey(1))
                {
                    throw new InvalidOperationException("Pattern keys must include '1'");
                }
            }

            int finalWidth = width ?? int.MaxValue;
            int targetWidth = descriptorCache.Keys.Where(x => x <= finalWidth).Max();

            if (descriptorCache[targetWidth] is not { } descriptor)
            {
                string? selectedPattern = Patterns.Any() ? Patterns[targetWidth.ToStringInvariant()] : Pattern;
                IEnumerable<ILineToken> lineTokens = LineDescriptor.Parse(selectedPattern).ToArray();

                if (TimestampFormat is { } timestampFormat && lineTokens.OfType<TimestampToken>().FirstOrDefault() is { Format: null } timestampToken)
                {
                    timestampToken.Format = timestampFormat;
                }

                descriptorCache[targetWidth] = descriptor = new LineDescriptor(lineTokens);
            }

            return descriptor;
        }
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
                    owner.descriptorCache = null;
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
                owner.descriptorCache = null;
            }
        }

        public void Clear()
        {
            lock (owner.lockObj)
            {
                underlying.Clear();
                owner.descriptorCache = null;
            }
        }

        public bool Contains(KeyValuePair<string, string?> item) => underlying.Contains(item);

        public void CopyTo(KeyValuePair<string, string?>[] array, int arrayIndex) => underlying.CopyTo(array, arrayIndex);

        public bool Remove(KeyValuePair<string, string?> item)
        {
            lock (owner.lockObj)
            {
                bool result = underlying.Remove(item);
                owner.descriptorCache = null;
                return result;
            }
        }

        public void Add(string key, string? value)
        {
            lock (owner.lockObj)
            {
                underlying.Add(key, value.HardTrim());
                owner.descriptorCache = null;
            }
        }

        public bool ContainsKey(string key) => underlying.ContainsKey(key);

        public bool Remove(string key)
        {
            lock (owner.lockObj)
            {
                bool result = underlying.Remove(key);
                owner.descriptorCache = null;
                return result;
            }
        }

        public bool TryGetValue(string key, out string? value) => underlying.TryGetValue(key, out value);
    }
}
