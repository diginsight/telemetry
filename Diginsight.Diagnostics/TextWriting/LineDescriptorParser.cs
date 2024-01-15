using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics.TextWriting;

public sealed class LineDescriptorParser
{
    private const string MessageThenNothingErrMsg = "'Message' token must be followed by nothing";
    private const string DuplicatedTokenErrMsg = "Duplicated token";
    private const string IndentationBeforeMessageErrMsg = "'Indentation' token can be followed only by 'Message' token";

    private static readonly Histogram<double> ParseDuration = AutoObservabilityUtils.Meter.CreateHistogram<double>("diginsight.parse_line_pattern_duration", "ms");

    private static readonly ILineTokenParser[] DefaultLineTokenParsers =
    [
        new TimestampTokenParser(),
        new CategoryTokenParser(),
        new LogLevelTokenParser(),
        new SimpleTokenParser("delta", DeltaToken.Instance),
        new SimpleTokenParser("depth", DepthToken.Instance),
        new SimpleTokenParser("duration", DurationToken.Instance),
        new SimpleTokenParser("traceId", TraceIdToken.Instance),
        new IndentationTokenParser(),
        new MessageTokenParser(),
    ];

    private readonly IEnumerable<ILineTokenParser> customLineTokenParsers;

    public LineDescriptorParser(IEnumerable<ILineTokenParser> customLineTokenParsers)
    {
        this.customLineTokenParsers = customLineTokenParsers;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<ILineToken> Parse(string? pattern)
    {
        return pattern is not null ? ParseCore(pattern) : LineDescriptor.DefaultLineTokens;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LineDescriptor ParseFull(string? pattern)
    {
        return pattern is not null ? new LineDescriptor(ParseCore(pattern), false) : default;
    }

    private IEnumerable<ILineToken> ParseCore(string pattern)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        bool success = true;

        try
        {
            IList<ILineToken> lineTokens = new List<ILineToken>();
            bool lastWasIndentation = false;

            ReadOnlySpan<char> patternSpan = pattern.AsSpan();
            while (true)
            {
                if (patternSpan.IsEmpty || patternSpan[0] != '{')
                {
                    throw new FormatException("Expected '{'");
                }

                patternSpan = patternSpan[1..];
                int closingIndex = patternSpan.IndexOf('}');
                if (closingIndex < 0)
                {
                    throw new FormatException("No matching '}'");
                }

                ILineToken ParseToken(in ReadOnlySpan<char> tokenSpan)
                {
                    foreach (ILineTokenParser parser in DefaultLineTokenParsers.Concat(customLineTokenParsers))
                    {
                        if (!tokenSpan.StartsWith(parser.TokenName.AsSpan(), StringComparison.OrdinalIgnoreCase))
                            continue;

                        ReadOnlySpan<char> tokenDetailSpan = tokenSpan[parser.TokenName.Length..];
                        if (tokenDetailSpan is [ not '|', .. ])
                            continue;

                        if (parser is not MessageTokenParser && lastWasIndentation)
                            throw new FormatException(IndentationBeforeMessageErrMsg);

                        return parser.Parse(tokenDetailSpan);
                    }

                    throw new FormatException("Unknown token");
                }

                ILineToken lineToken = ParseToken(patternSpan[..closingIndex]);

                if (lineToken is IndentationToken)
                {
                    lastWasIndentation = true;
                }

                Type lineTokenType = lineToken.GetType();
                if (lineTokens.Any(x => x.GetType() == lineTokenType))
                {
                    throw new FormatException(DuplicatedTokenErrMsg);
                }

                lineTokens.Add(lineToken);

                patternSpan = patternSpan[(closingIndex + 1)..];
                if (patternSpan.IsEmpty)
                {
                    break;
                }

                if (lineToken is MessageToken)
                {
                    throw new FormatException(MessageThenNothingErrMsg);
                }

                if (patternSpan[0] != ' ')
                {
                    throw new FormatException("Expected ' '");
                }

                patternSpan = patternSpan[1..];
            }

            return lineTokens;
        }
        catch (Exception)
        {
            success = false;
            throw;
        }
        finally
        {
            ParseDuration.Record(stopwatch.Elapsed.TotalMilliseconds, new Tag("status", success));
        }
    }

    internal static void Validate(IEnumerable<ILineToken> lineTokens)
    {
        ILineToken[] lineTokenArray = lineTokens.ToArray();
        int count = lineTokenArray.Length;

        if (count == 0)
        {
            throw new ArgumentException("No tokens");
        }

        if (count != lineTokens.Select(static x => x.GetType()).Distinct().Count())
        {
            throw new ArgumentException(DuplicatedTokenErrMsg);
        }

        int indentationIndex = -1;
        int messageIndex = count;
        for (var i = 0; i < count; i++)
        {
            switch (lineTokenArray[i])
            {
                case IndentationToken:
                    indentationIndex = i;
                    break;

                case MessageToken:
                    messageIndex = i;
                    break;
            }
        }

        if (messageIndex < count - 1)
        {
            throw new ArgumentException(MessageThenNothingErrMsg);
        }

        if (indentationIndex >= 0 && indentationIndex != messageIndex - 1)
        {
            throw new ArgumentException(IndentationBeforeMessageErrMsg);
        }
    }
}
