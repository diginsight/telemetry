using Diginsight.Diagnostics.TextWriting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
#if NET7_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace Diginsight.Diagnostics;

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
            double? durationMsec = duration?.TotalMilliseconds;

            static void Checkpoint(
                Activity? activity,
                DateTime timestamp,
                bool isActivity,
                double? durationMsec,
                out DateTime? prevTimestamp,
                out ActivityTraceId? traceId,
                out bool lastWasStart
            )
            {
                const string lastLogTimestampCustomPropertyName = "lastLogTimestamp";
                const string lastWasStartCustomPropertyName = "lastWasStart";

                if (activity is null)
                {
                    prevTimestamp = null;
                }
                else
                {
                    prevTimestamp = activity.GetCustomProperty(lastLogTimestampCustomPropertyName) switch
                    {
                        DateTime dt => dt,
                        null => activity.Parent?.GetCustomProperty(lastLogTimestampCustomPropertyName) switch
                        {
                            DateTime dt => dt,
                            null => null,
                            _ => throw new InvalidOperationException("Invalid last log timestamp in activity"),
                        },
                        _ => throw new InvalidOperationException("Invalid last log timestamp in activity"),
                    };
                }

                if (activity is not null)
                {
                    lastWasStart = activity.GetCustomProperty(lastWasStartCustomPropertyName) switch
                    {
                        bool b => b,
                        null => false,
                        _ => throw new InvalidOperationException($"Invalid '{lastWasStartCustomPropertyName}' in activity"),
                    };
                    activity.SetCustomProperty(lastWasStartCustomPropertyName, isActivity && durationMsec is null);

                    activity.SetCustomProperty(lastLogTimestampCustomPropertyName, timestamp);
                    if (durationMsec is not null)
                    {
                        activity.Parent?.SetCustomProperty(lastLogTimestampCustomPropertyName, timestamp);
                    }

                    traceId = activity.TraceId;
                }
                else
                {
                    traceId = null;
                    lastWasStart = false;
                }
            }

            Checkpoint(activity, timestamp, isActivity, durationMsec, out DateTime? prevTimestamp, out ActivityTraceId? traceId, out bool lastWasStart);

            StringBuilder prefixSb = new ();
            StrongBox<int>? depthBox = null;

            new TimestampAppender(timestampFormat, timestampCulture).Append(prefixSb, timestamp);
            prefixSb.Append(' ');
            new CategoryAppender(categoryLength).Append(prefixSb, category);
            prefixSb.Append(' ');
            new LogLevelAppender(4).Append(prefixSb, logLevel);
            prefixSb.Append(' ');
            TraceIdAppender.Instance.Append(prefixSb, traceId);
            prefixSb.Append(' ');
            DeltaAppender.Instance.Append(prefixSb, lastWasStart, timestamp, prevTimestamp);
            prefixSb.Append(' ');
            DurationAppender.Instance.Append(prefixSb, durationMsec);
            prefixSb.Append(' ');
            DepthAppender.Instance.Append(prefixSb, ref depthBox, activity);
            prefixSb.Append(' ');
            new IndentationAppender(maxIndentedDepth).Append(prefixSb, ref depthBox, activity, isActivity, out int indentationLength);

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

#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    // TODO Promote to netstandard2.0
    internal static (IEnumerable<IPrefixTokenAppender> CustomAppenders, IndentationAppender IndentationAppender, int MaxMessageLength, int MaxLineLength) Parse(string pattern)
    {
        ICollection<IPrefixTokenAppender> customAppenders = new List<IPrefixTokenAppender>();
        int? maxIndentedDepth = null;
        int? maxMessageLength = null;
        int? maxLineLength = null;

        ReadOnlySpan<char> patternSpan = pattern;
        while (!patternSpan.IsEmpty)
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
            if (tokenSpan.StartsWith("category", StringComparison.OrdinalIgnoreCase))
            {
                tokenSpan = tokenSpan[8..];

                int? categoryLength;
                if (tokenSpan.IsEmpty)
                {
                    categoryLength = null;
                }
                else if (tokenSpan[0] == ';')
                {
                    categoryLength = int.TryParse(tokenSpan[1..], out int cl) ? cl : throw new FormatException("Expected integer");
                }
                else
                {
                    throw new FormatException("Expected ';' or nothing");
                }

                customAppenders.Add(new CategoryAppender(categoryLength));
            }
            else if (tokenSpan.Equals("delta", StringComparison.OrdinalIgnoreCase))
            {
                customAppenders.Add(DeltaAppender.Instance);
            }
            else if (tokenSpan.StartsWith("depth", StringComparison.OrdinalIgnoreCase))
            {
                customAppenders.Add(DepthAppender.Instance);

                tokenSpan = tokenSpan[5..];
                if (!tokenSpan.IsEmpty)
                {
                    if (tokenSpan[0] == ';')
                    {
                        maxIndentedDepth = int.TryParse(tokenSpan[1..], out int mid) ? mid : throw new FormatException("Expected integer");
                    }
                    else
                    {
                        throw new FormatException("Expected ';' or nothing");
                    }
                }
            }
            else if (tokenSpan.Equals("duration", StringComparison.OrdinalIgnoreCase))
            {
                customAppenders.Add(DurationAppender.Instance);
            }
            else if (tokenSpan.StartsWith("message", StringComparison.OrdinalIgnoreCase))
            {
                tokenSpan = tokenSpan[7..];

                if (tokenSpan.IsEmpty)
                {
                    maxMessageLength = 0;
                    maxLineLength = 0;
                }
                else
                {
                    if (tokenSpan[0] != ';')
                    {
                        throw new FormatException("Expected ';' or nothing");
                    }

                    tokenSpan = tokenSpan[1..];
                    int semicolonIndex = tokenSpan.IndexOf(';');
                    if (semicolonIndex < 0)
                    {
                        maxMessageLength = int.TryParse(tokenSpan, out int mml) ? mml : throw new FormatException("Expected integer");
                        maxLineLength = 0;
                    }
                    else
                    {
                        ReadOnlySpan<char> innerSpan = tokenSpan[..semicolonIndex];
                        maxMessageLength = innerSpan.IsEmpty ? 0 : int.TryParse(innerSpan, out int mml) ? mml : throw new FormatException("Expected integer");

                        maxLineLength = int.TryParse(tokenSpan[(semicolonIndex + 1)..], out int mll) ? mll : throw new FormatException("Expected integer");
                    }
                }
            }
            else if (tokenSpan.StartsWith("timestamp", StringComparison.OrdinalIgnoreCase))
            {
                tokenSpan = tokenSpan[9..];

                string? tsFormat;
                CultureInfo tsCulture;

                if (tokenSpan.IsEmpty)
                {
                    tsFormat = null;
                    tsCulture = CultureInfo.InvariantCulture;
                }
                else
                {
                    if (tokenSpan[0] != ';')
                    {
                        throw new FormatException("Expected ';' or nothing");
                    }

                    tokenSpan = tokenSpan[1..];
                    int semicolonIndex = tokenSpan.LastIndexOf(';');
                    if (semicolonIndex < 0)
                    {
                        tsFormat = tokenSpan.ToString();
                        tsCulture = CultureInfo.InvariantCulture;
                    }
                    else
                    {
                        ReadOnlySpan<char> innerSpan = tokenSpan[..semicolonIndex];
                        tsFormat = innerSpan.IsEmpty ? null : innerSpan.ToString();

                        try
                        {
                            tsCulture = CultureInfo.GetCultureInfo(tokenSpan[(semicolonIndex + 1)..].ToString());
                        }
                        catch (CultureNotFoundException exception)
                        {
                            throw new FormatException("Culture not found", exception);
                        }
                    }
                }

                if (tsFormat is not null)
                {
                    try
                    {
                        _ = DateTime.UtcNow.ToString(tsFormat);
                    }
                    catch (FormatException)
                    {
                        throw new FormatException("Invalid timestamp format");
                    }
                }

                customAppenders.Add(new TimestampAppender(tsFormat, tsCulture));
            }
            else if (tokenSpan.Equals("traceid", StringComparison.OrdinalIgnoreCase))
            {
                customAppenders.Add(TraceIdAppender.Instance);
            }
            else
            {
                throw new FormatException("Unknown token");
            }

            patternSpan = patternSpan[(closingIndex + 1)..];
            if (patternSpan.IsEmpty)
            {
                continue;
            }

            if (maxLineLength is not null)
            {
                throw new FormatException("'Message' token must be followed by nothing");
            }

            if (!patternSpan.StartsWith(" {", StringComparison.Ordinal))
            {
                throw new FormatException("Expected ' {'");
            }

            patternSpan = patternSpan[1..];
        }

        if (customAppenders.Count != customAppenders.Select(static x => x.GetType()).Count())
        {
            throw new FormatException("Duplicate token");
        }

        return (customAppenders, new IndentationAppender(maxIndentedDepth), maxMessageLength ?? 0, maxLineLength ?? 0);
    }
#endif
}
