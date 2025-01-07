using Diginsight.Diagnostics.TextWriting;
using Microsoft.Extensions.Options;
#if !NET9_0_OR_GREATER
using Lock = object;
#endif

namespace Diginsight.Diagnostics;

internal sealed class ConsoleLineDescriptorProvider : IConsoleLineDescriptorProvider
{
    private readonly IEnumerable<ILineTokenParser> customLineTokenParsers;
    private readonly IDiginsightConsoleFormatterOptions formatterOptions;

    private readonly Lock lockObj = new ();
    private readonly IDictionary<ValueTuple<int?>, LineDescriptor> descriptorCache = new Dictionary<ValueTuple<int?>, LineDescriptor>();
    private IDictionary<int, IEnumerable<ILineToken>?>? lineTokensCache;

    public ConsoleLineDescriptorProvider(
        IEnumerable<ILineTokenParser> customLineTokenParsers,
        IOptions<DiginsightConsoleFormatterOptions> formatterOptions
    )
    {
        this.customLineTokenParsers = customLineTokenParsers;
        this.formatterOptions = formatterOptions.Value;
    }

    public LineDescriptor GetLineDescriptor(int? width)
    {
        if (width <= 0)
        {
            width = null;
        }

        lock (lockObj)
        {
            ValueTuple<int?> descriptorKey = new (width);
            return descriptorCache.TryGetValue(descriptorKey, out LineDescriptor? descriptor)
                ? descriptor
                : descriptorCache[descriptorKey] = MakeLineDescriptor();

            LineDescriptor MakeLineDescriptor()
            {
                string? pattern = formatterOptions.Pattern;
                IReadOnlyDictionary<int, string?> patterns = formatterOptions.Patterns;

                if (lineTokensCache is null)
                {
                    lineTokensCache = (pattern, patterns.Count) switch
                    {
                        (not null, > 0) => throw new InvalidOperationException($"Cannot specify both {nameof(formatterOptions.Pattern)} and {nameof(formatterOptions.Patterns)}"),
                        (null, > 0) => patterns
                            .Where(static x => x.Value is not null)
                            .Select(static x => x.Key)
                            .Select(static n => n > 0 ? n : throw new InvalidOperationException("Pattern keys must be positive integers"))
                            .ToDictionary(static n => n, static IEnumerable<ILineToken>? (_) => null),
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
                    string? selectedPattern = patterns.Any() ? patterns[targetWidth].HardTrim() : pattern;
                    lineTokensCache[targetWidth] = lineTokens = LineDescriptor.Parse(selectedPattern, customLineTokenParsers);
                }

                IList<ILineToken> finalLineTokens = new List<ILineToken>(lineTokens.Select(static x => x.Clone()));

                if (formatterOptions.TimestampFormat is { } timestampFormat &&
                    finalLineTokens.OfType<TimestampToken>().FirstOrDefault() is { Format: null } timestampToken)
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
    }
}
