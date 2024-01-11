using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace Diginsight.Diagnostics.TextWriting;

public static class ObservabilityTextWriter
{
    private static readonly Histogram<double> WriteDuration = AutoObservabilityUtils.Meter.CreateHistogram<double>("diginsight.text_write_duration", "ms");
    private static readonly IDictionary<(int, int, int, int), IMessageLineResizer> ResizerCache = new Dictionary<(int, int, int, int), IMessageLineResizer>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(
        TextWriter textWriter,
        DateTime timestamp,
        LogLevel logLevel,
        string category,
        string message,
        Exception? exception,
        bool isActivity,
        TimeSpan? duration,
        IObservabilityTextWriterOptions writerOptions
    )
    {
        Write(
            textWriter,
            timestamp,
            logLevel,
            category,
            message,
            exception,
            isActivity,
            duration,
            writerOptions.TimestampFormat,
            writerOptions.TimestampCulture,
            writerOptions.CategoryLength,
            writerOptions.MaxMessageLength,
            writerOptions.MaxLineLength,
            writerOptions.MaxIndentedDepth
        );
    }

    public static void Write(
        TextWriter textWriter,
        DateTime timestamp,
        LogLevel logLevel,
        string category,
        string message,
        Exception? exception,
        bool isActivity,
        TimeSpan? duration,
#if NET7_0_OR_GREATER
        [StringSyntax(StringSyntaxAttribute.DateTimeFormat)]
#endif
        string? timestampFormat,
        CultureInfo timestampCulture,
        int categoryLength,
        int maxMessageLength,
        int maxLineLength,
        int maxIndentedDepth
    )
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        try
        {
            Activity? activity = Activity.Current;
            LinePrefixData linePrefixData = new (timestamp, logLevel, category, isActivity, duration, activity);

            IEnumerable<IPrefixTokenAppender> appenders =
            [
                new TimestampAppender(timestampFormat, timestampCulture),
                new CategoryAppender(categoryLength),
                LogLevelAppender.UnsafeFor(null),
                TraceIdAppender.Instance,
                DeltaAppender.Instance,
                DurationAppender.Instance,
                DepthAppender.Instance,
            ];

            StringBuilder prefixSb = new ();
            foreach (IPrefixTokenAppender appender in appenders)
            {
                appender.Append(prefixSb, linePrefixData);
                prefixSb.Append(' ');
            }

            int indentationLength;
            {
                int depth = linePrefixData.Depth;
                indentationLength = maxIndentedDepth < 0 || depth <= maxIndentedDepth
                    ? depth * 2 - (linePrefixData.IsActivity ? 1 : 0)
                    : maxIndentedDepth * 2;
            }

            prefixSb.Append(new string(' ', indentationLength));

            string actualPrefix = prefixSb.ToString();
            int prefixLength = actualPrefix.Length;
            string blankPrefix = new string(' ', prefixLength);

            const char newLine = '\n';

            StringBuilder fullMessageSb = new (message);
            if (exception is not null)
            {
                activity?.RecordException(exception);
                fullMessageSb.Append(newLine).Append(exception);
            }
            string fullMessage = fullMessageSb.Replace("\r", "").ToString();

            static IMessageLineResizer GetResizer(int maxMessage, int maxLine, int indentation, int prefix)
            {
                IMessageLineResizer MakeResizer()
                {
                    bool chop = maxMessage < 0 || maxLine < 0;

                    int absMaxLine = Math.Abs(maxLine);
                    int absMaxMessage = Math.Abs(maxMessage);

                    int? final0 = (absMaxLine, absMaxMessage) switch
                    {
                        (0, 0) => null,
                        (0, _) => absMaxMessage - indentation,
                        (_, 0) => absMaxLine - prefix,
                        _ => Math.Min(absMaxLine, prefix + absMaxMessage - indentation) - prefix,
                    };

                    return final0 is not { } final
                        ? NoopMessageLineResizer.Instance
                        : final < 10
                            ? new ChoppingMessageLineResizer(10)
                            : chop
                                ? new ChoppingMessageLineResizer(final)
                                : new BreakingMessageLineResizer(final);
                }

                lock (((ICollection)ResizerCache).SyncRoot)
                {
                    var resizerKey = (maxMessage, maxLine, indentation, prefix);
                    return ResizerCache.TryGetValue(resizerKey, out IMessageLineResizer? resizer)
                        ? resizer
                        : ResizerCache[resizerKey] = MakeResizer();
                }
            }

            IMessageLineResizer resizer = GetResizer(maxMessageLength, maxLineLength, indentationLength, prefixLength);

            bool first = true;
            foreach (string line in resizer.Resize(fullMessage.Split(newLine)))
            {
                if (first)
                {
                    first = false;
                    textWriter.Write(actualPrefix);
                }
                else
                {
                    textWriter.Write(blankPrefix);
                }
                textWriter.WriteLine(line);
            }
        }
        finally
        {
            WriteDuration.Record(stopwatch.Elapsed.TotalMilliseconds);
        }
    }

    public interface IActivityMark
    {
        object? State { get; }
        TimeSpan? Duration { get; }
    }

    public interface IActivityMark<out TState> : IActivityMark
    {
        new TState State { get; }

#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        object? IActivityMark.State => State;
#endif
    }

    public interface IOtlpOnly
    {
        Tags State { get; }
    }

    internal static (IEnumerable<IPrefixTokenAppender> CustomAppenders, IndentationAppender IndentationAppender, int MaxMessageLength, int MaxLineLength) Parse(string pattern)
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
                throw new FormatException("Duplicate token");
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

        LineDescriptor lineDescriptor = new ();
        foreach (ILineToken lineToken in lineTokens)
        {
            lineToken.Apply(ref lineDescriptor);
        }

        return (
            lineDescriptor.CustomAppenders,
            new IndentationAppender(lineDescriptor.MaxIndentedDepth),
            lineDescriptor.MaxMessageLength ?? 0,
            lineDescriptor.MaxLineLength ?? 0
        );
    }
}
