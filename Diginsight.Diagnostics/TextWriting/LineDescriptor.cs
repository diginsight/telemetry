using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics.TextWriting;

public readonly struct LineDescriptor
{
    private const int DefaultMaxIndentedDepth = 10;
    private const int DefaultMaxMessageLength = 0;
    private const int DefaultMaxLineLength = 0;

    private static readonly Histogram<double> ParseDuration = AutoObservabilityUtils.Meter.CreateHistogram<double>("diginsight.parse_line_pattern_duration", "ms");

    public static readonly IEnumerable<ILineToken> DefaultLineTokens =
    [
        new TimestampToken(),
        new CategoryToken(),
        new LogLevelToken(),
        TraceIdToken.Instance,
        DeltaToken.Instance,
        DurationToken.Instance,
        new DepthToken(),
        new MessageToken(),
    ];

    private static readonly IEnumerable<IPrefixTokenAppender> DefaultAppenders = Apply(DefaultLineTokens).Appenders;

    private readonly IEnumerable<IPrefixTokenAppender>? appenders;
    private readonly int? maxIndentedDepth;
    private readonly int? maxMessageLength;
    private readonly int? maxLineLength;

    public IEnumerable<IPrefixTokenAppender> Appenders => appenders ?? DefaultAppenders;

    public int MaxIndentedDepth => maxIndentedDepth ?? DefaultMaxIndentedDepth;

    public int MaxMessageLength => maxMessageLength ?? DefaultMaxMessageLength;

    public int MaxLineLength => maxLineLength ?? DefaultMaxLineLength;

    public LineDescriptor(IEnumerable<ILineToken> lineTokens)
    {
        MutableLineDescriptor descriptor = Apply(lineTokens);

        appenders = descriptor.Appenders;
        maxIndentedDepth = descriptor.MaxIndentedDepth;
        maxMessageLength = descriptor.MaxMessageLength;
        maxLineLength = descriptor.MaxLineLength;
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
        if (pattern is null)
        {
            return DefaultLineTokens;
        }

        Stopwatch stopwatch = Stopwatch.StartNew();
        bool success = true;

        try
        {
            ICollection<ILineToken> lineTokens = new List<ILineToken>();
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

                ILineToken lineToken;
                if (tokenSpan.StartsWith("category".AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
                    tokenSpan = tokenSpan[8..];
                    lineToken = CategoryToken.Parse(tokenSpan);
                }
                else if (tokenSpan.Equals("delta".AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
                    lineToken = DeltaToken.Instance;
                }
                else if (tokenSpan.StartsWith("depth".AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
                    tokenSpan = tokenSpan[5..];
                    lineToken = DepthToken.Parse(tokenSpan);
                }
                else if (tokenSpan.Equals("duration".AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
                    lineToken = DurationToken.Instance;
                }
                else if (tokenSpan.StartsWith("loglevel".AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
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
                    tokenSpan = tokenSpan[9..];
                    lineToken = TimestampToken.Parse(tokenSpan);
                }
                else if (tokenSpan.Equals("traceid".AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
                    lineToken = TraceIdToken.Instance;
                }
                else
                {
                    throw new FormatException("Unknown token");
                }

                Type lineTokenType = lineToken.GetType();
                if (lineTokens.Any(x => x.GetType() == lineTokenType))
                {
                    throw new FormatException("Duplicated token");
                }

                lineTokens.Add(lineToken);

                patternSpan = patternSpan[(closingIndex + 1)..];
                if (patternSpan.IsEmpty)
                {
                    break;
                }

                if (lineToken is MessageToken)
                {
                    throw new FormatException("'Message' token must be followed by nothing");
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LineDescriptor ParseFull(string? pattern)
    {
        return pattern is not null ? new LineDescriptor(Parse(pattern)) : default;
    }
}
