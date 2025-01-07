using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics.TextWriting;

public sealed class LineDescriptor
{
    private const string MessageThenNothingErrMsg = "'Message' token must be followed by nothing";
    private const string DuplicatedTokenErrMsg = "Duplicated token";
    private const string IndentationBeforeMessageErrMsg = "'Indentation' token can be followed only by 'Message' token";

    private static readonly Histogram<double> ParseDuration = SelfObservabilityUtils.Meter.CreateHistogram<double>("diginsight.parse_line_pattern_duration", "ms");

    private static readonly IEnumerable<ILineTokenParser> DefaultLineTokenParsers =
    [
        new TimestampTokenParser(),
        new CategoryTokenParser(),
        new LogLevelTokenParser(),
        new SimpleTokenParser("traceid", TraceIdToken.Instance),
        new SimpleTokenParser("spanid", SpanIdToken.Instance),
        new SimpleTokenParser("delta", DeltaToken.Instance),
        new SimpleTokenParser("duration", DurationToken.Instance),
        new DepthTokenParser(),
        new IndentationTokenParser(),
        new MessageTokenParser(),
    ];

    private static readonly IEnumerable<ILineToken> DefaultLineTokensCore =
    [
        new TimestampToken(),
        new CategoryToken(),
        new LogLevelToken(),
        TraceIdToken.Instance,
        DeltaToken.Instance,
        DurationToken.Instance,
        new DepthToken(),
        new IndentationToken(),
        new MessageToken(),
    ];

    public static IEnumerable<ILineToken> DefaultLineTokens => DefaultLineTokensCore.Select(static x => x.Clone());

    public static readonly LineDescriptor DefaultDescriptor = new (Apply(DefaultLineTokensCore));

    public IEnumerable<IPrefixTokenAppender> Appenders { get; }

    public int MaxIndentedDepth { get; }

    public int MaxMessageLength { get; }

    public int MaxLineLength { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LineDescriptor(IEnumerable<ILineToken> lineTokens)
        : this(lineTokens.ToArray(), true) { }

    public LineDescriptor(MutableLineDescriptor descriptor)
    {
        Appenders = descriptor.Appenders;
        MaxIndentedDepth = descriptor.MaxIndentedDepth ?? 0;
        MaxMessageLength = descriptor.MaxMessageLength ?? 0;
        MaxLineLength = descriptor.MaxLineLength ?? 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private LineDescriptor(IEnumerable<ILineToken> lineTokens, bool validate)
        : this(validate ? ValidateAndApply(lineTokens) : Apply(lineTokens)) { }

    private static MutableLineDescriptor ValidateAndApply(IEnumerable<ILineToken> lineTokens)
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

        return Apply(lineTokens);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static MutableLineDescriptor Apply(IEnumerable<ILineToken> lineTokens)
    {
        MutableLineDescriptor descriptor = new ();
        foreach (ILineToken lineToken in lineTokens)
        {
            lineToken.Apply(ref descriptor);
        }
        return descriptor;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<ILineToken> Parse(string? pattern, IEnumerable<ILineTokenParser>? customLineTokenParsers = null)
    {
        return pattern is not null ? ParseCore(pattern, customLineTokenParsers) : DefaultLineTokens;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LineDescriptor ParseFull(string? pattern, IEnumerable<ILineTokenParser>? customLineTokenParsers = null)
    {
        return pattern is not null ? new LineDescriptor(ParseCore(pattern, customLineTokenParsers), false) : DefaultDescriptor;
    }

    private static IEnumerable<ILineToken> ParseCore(string pattern, IEnumerable<ILineTokenParser>? customLineTokenParsers)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        bool success = true;

        try
        {
            IEnumerable<ILineTokenParser> lineTokenParsers = customLineTokenParsers is not null
                ? DefaultLineTokenParsers.Concat(customLineTokenParsers).ToArray()
                : DefaultLineTokenParsers;

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
                    foreach (ILineTokenParser parser in lineTokenParsers)
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
            ParseDuration.Record(
                stopwatch.Elapsed.TotalMilliseconds,
                new Tag("status", (success ? ActivityStatusCode.Ok : ActivityStatusCode.Error).ToString())
            );
        }
    }
}
