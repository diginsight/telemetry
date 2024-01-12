using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics.TextWriting;

public readonly struct LineDescriptor
{
    private const string MessageThenNothingErrMsg = "'Message' token must be followed by nothing";
    private const string DuplicatedTokenErrMsg = "Duplicated token";
    private const string IndentationBeforeMessageErrMsg = "'Indentation' token can be followed only by 'Message' token";

    private static readonly Histogram<double> ParseDuration = AutoObservabilityUtils.Meter.CreateHistogram<double>("diginsight.parse_line_pattern_duration", "ms");

    public static readonly IEnumerable<ILineToken> DefaultLineTokens =
    [
        new TimestampToken(),
        new CategoryToken(),
        new LogLevelToken(),
        TraceIdToken.Instance,
        DeltaToken.Instance,
        DurationToken.Instance,
        DepthToken.Instance,
        new IndentationToken(),
        new MessageToken(),
    ];

    private static readonly IEnumerable<IPrefixTokenAppender> DefaultAppenders = Apply(DefaultLineTokens).Appenders;

    private readonly IEnumerable<IPrefixTokenAppender>? appenders;

    public IEnumerable<IPrefixTokenAppender> Appenders => appenders ?? DefaultAppenders;

    public int MaxIndentedDepth { get; }

    public int MaxMessageLength { get; }

    public int MaxLineLength { get; }

    public LineDescriptor(IEnumerable<ILineToken> lineTokens)
        : this(lineTokens.ToArray(), true) { }

    private LineDescriptor(IEnumerable<ILineToken> lineTokens, bool validate)
    {
        if (validate)
        {
            ILineToken[] lineTokenArray = lineTokens.ToArray();
            int count = lineTokenArray.Length;
            if (count != lineTokens.Select(static x => x.GetType()).Count())
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

        MutableLineDescriptor descriptor = Apply(lineTokens);

        appenders = descriptor.Appenders;
        MaxIndentedDepth = descriptor.MaxIndentedDepth ?? 0;
        MaxMessageLength = descriptor.MaxMessageLength ?? 0;
        MaxLineLength = descriptor.MaxLineLength ?? 0;
    }

    private static MutableLineDescriptor Apply(IEnumerable<ILineToken> lineTokens)
    {
        MutableLineDescriptor descriptor = new();
        foreach (ILineToken lineToken in lineTokens)
        {
            lineToken.Apply(ref descriptor);
        }
        return descriptor;
    }

    public static IEnumerable<ILineToken> Parse(string? pattern)
    {
        return pattern is not null ? ParseCore(pattern) : DefaultLineTokens;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LineDescriptor ParseFull(string? pattern)
    {
        return pattern is not null ? new LineDescriptor(ParseCore(pattern), false) : default;
    }

    private static IEnumerable<ILineToken> ParseCore(string pattern)
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
                if (patternSpan[0] != '{')
                {
                    throw new FormatException("Expected '{'");
                }

                patternSpan = patternSpan[1..];
                int closingIndex = patternSpan.IndexOf('}');
                if (closingIndex < 0)
                {
                    throw new FormatException("No matching '}'");
                }

                ReadOnlySpan<char> tokenSpan = patternSpan[..closingIndex];

                void ThrowIfAfterIndentation()
                {
                    if (lastWasIndentation)
                    {
                        throw new FormatException(IndentationBeforeMessageErrMsg);
                    }
                }

                ILineToken lineToken;
                if (tokenSpan.StartsWith("category".AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
                    ThrowIfAfterIndentation();
                    tokenSpan = tokenSpan[8..];
                    lineToken = CategoryToken.Parse(tokenSpan);
                }
                else if (tokenSpan.Equals("delta".AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
                    ThrowIfAfterIndentation();
                    lineToken = DeltaToken.Instance;
                }
                else if (tokenSpan.Equals("depth".AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
                    ThrowIfAfterIndentation();
                    lineToken = DepthToken.Instance;
                }
                else if (tokenSpan.Equals("duration".AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
                    ThrowIfAfterIndentation();
                    lineToken = DurationToken.Instance;
                }
                else if (tokenSpan.StartsWith("indentation".AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
                    ThrowIfAfterIndentation();
                    tokenSpan = tokenSpan[11..];
                    lineToken = IndentationToken.Parse(tokenSpan);
                    lastWasIndentation = true;
                }
                else if (tokenSpan.StartsWith("loglevel".AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
                    ThrowIfAfterIndentation();
                    tokenSpan = tokenSpan[8..];
                    lineToken = LogLevelToken.Parse(tokenSpan);
                }
                else if (tokenSpan.StartsWith("message".AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
                    tokenSpan = tokenSpan[7..];
                    lineToken = MessageToken.Parse(tokenSpan);
                }
                else if (tokenSpan.StartsWith("timestamp".AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
                    ThrowIfAfterIndentation();
                    tokenSpan = tokenSpan[9..];
                    lineToken = TimestampToken.Parse(tokenSpan);
                }
                else if (tokenSpan.Equals("traceid".AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
                    ThrowIfAfterIndentation();
                    lineToken = TraceIdToken.Instance;
                }
                else
                {
                    throw new FormatException("Unknown token");
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
}
