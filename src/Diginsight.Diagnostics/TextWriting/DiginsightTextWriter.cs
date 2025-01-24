using Diginsight.Logging;
using Microsoft.Extensions.Logging;
using Pastel;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace Diginsight.Diagnostics.TextWriting;

public static class DiginsightTextWriter
{
    private static readonly Histogram<double> WriteDuration = SelfObservabilityUtils.Meter.CreateHistogram<double>("diginsight.write_text_duration", "us");

    private static readonly IDictionary<(int, int, int, int), IMessageLineResizer> ResizerCache =
        new Dictionary<(int, int, int, int), IMessageLineResizer>(ResizerKeyEqualityComparer.Instance);

    public static bool DisplayTiming { get; set; }

    private sealed class ResizerKeyEqualityComparer : IEqualityComparer<(int, int, int, int)>
    {
        public static readonly IEqualityComparer<(int, int, int, int)> Instance = new ResizerKeyEqualityComparer();

        private ResizerKeyEqualityComparer() { }

        public bool Equals((int, int, int, int) x, (int, int, int, int) y)
        {
            return x is (0, 0, _, _) && y is (0, 0, _, _) || x.Equals(y);
        }

        public int GetHashCode((int, int, int, int) obj)
        {
            return (obj is (0, 0, _, _) ? (0, 0, 0, 0) : obj).GetHashCode();
        }
    }

    public static void ExpandState(
        ref object? state,
        out bool isActivity,
        out TimeSpan? duration,
        out DateTimeOffset? maybeTimestamp,
        out Activity? activity,
        out Func<LineDescriptor, LineDescriptor>? sealLineDescriptor
    )
    {
        LogMetadataCarrier.ExtractMetadata(ref state, out IEnumerable<ILogMetadata> metadataCollection);

        ActivityLifecycleLogEmitter.ILogMetadata? activityMetadata = metadataCollection.OfType<ActivityLifecycleLogEmitter.ILogMetadata>().FirstOrDefault();
        DeferredLoggerFactory.ILogMetadata? deferredMetadata = metadataCollection.OfType<DeferredLoggerFactory.ILogMetadata>().FirstOrDefault();
        TextWriterLogMetadata? writerMetadata = metadataCollection.OfType<TextWriterLogMetadata>().FirstOrDefault();

        activity = deferredMetadata?.Activity;
        maybeTimestamp = deferredMetadata?.Timestamp;

        if (activityMetadata is not null)
        {
            isActivity = true;
            duration = activityMetadata.Duration;
            activity ??= activityMetadata.Activity;

            bool isStop = duration is not null;
            maybeTimestamp ??=
                activity.GetCustomProperty(isStop ? ActivityCustomPropertyNames.EmitStopTimestamp : ActivityCustomPropertyNames.EmitStartTimestamp) as DateTimeOffset?;
        }
        else
        {
            isActivity = false;
            duration = null;
        }

        sealLineDescriptor = writerMetadata?.SealLineDescriptor;
    }

    public static bool Write(
        TextWriter textWriter,
        bool useColor,
        DateTime timestamp,
        Activity? activity,
        LogLevel logLevel,
        string category,
        string message,
        Exception? exception,
        bool isActivity,
        TimeSpan? duration,
        LineDescriptor lineDescriptor,
        Func<LineDescriptor, LineDescriptor>? sealLineDescriptor
    )
    {
        if (DisplayTiming)
        {
            if (lineDescriptor.MaxMessageLength is (> 7 or < -7) and var maxMessageLength)
            {
                MutableLineDescriptor mutableLineDescriptor = new (lineDescriptor)
                {
                    MaxMessageLength = (Math.Abs(maxMessageLength) - 7) * Math.Sign(maxMessageLength),
                };
                lineDescriptor = new LineDescriptor(mutableLineDescriptor);
            }
            else if (lineDescriptor.MaxLineLength is (> 7 or < -7) and var maxLineLength)
            {
                MutableLineDescriptor mutableLineDescriptor = new (lineDescriptor)
                {
                    MaxLineLength = (Math.Abs(maxLineLength) - 7) * Math.Sign(maxLineLength),
                };
                lineDescriptor = new LineDescriptor(mutableLineDescriptor);
            }

            using StringWriter stringWriter = new ();
            if (!Write(
                    stringWriter,
                    useColor,
                    timestamp,
                    activity,
                    logLevel,
                    category,
                    message,
                    exception,
                    isActivity,
                    duration,
                    lineDescriptor,
                    sealLineDescriptor,
                    out double timing
                ))
            {
                return false;
            }

            textWriter.Write("{0,5}µ {1}", ((long)timing).ToString(CultureInfo.InvariantCulture), stringWriter);
            return true;
        }
        else
        {
            return Write(
                textWriter,
                useColor,
                timestamp,
                activity,
                logLevel,
                category,
                message,
                exception,
                isActivity,
                duration,
                lineDescriptor,
                sealLineDescriptor,
                out _
            );
        }
    }

    public static bool Write(
        TextWriter textWriter,
        bool useColor,
        DateTime timestamp,
        Activity? activity,
        LogLevel logLevel,
        string category,
        string message,
        Exception? exception,
        bool isActivity,
        TimeSpan? duration,
        LineDescriptor lineDescriptor,
        Func<LineDescriptor, LineDescriptor>? sealLineDescriptor,
        out double timing
    )
    {
        if (activity?.GetLogBehavior() == LogBehavior.Truncate)
        {
            timing = 0;
            return false;
        }

        Stopwatch stopwatch = Stopwatch.StartNew();

        try
        {
            LinePrefixData linePrefixData = new (timestamp, logLevel, category, isActivity, duration, activity);
            lineDescriptor = sealLineDescriptor?.Invoke(lineDescriptor) ?? lineDescriptor;

            StringBuilder prefixSb = new ();
            int prefixLength = 0;
            foreach (IPrefixTokenAppender appender in lineDescriptor.Appenders)
            {
                appender.Append(prefixSb, ref prefixLength, linePrefixData, useColor);

                prefixSb.Append(' ');
                prefixLength++;
            }

            int indentationLength;
            {
                int depth = linePrefixData.Activity.GetDepth().VisualLocal;
                int maxIndentedDepth = lineDescriptor.MaxIndentedDepth;
                int finalDepth = maxIndentedDepth < 0 ? depth : Math.Min(depth, maxIndentedDepth);
                indentationLength = Math.Max(0, finalDepth * 2 - (linePrefixData.IsActivity ? 1 : 0));
            }

            prefixSb.Append(new string(' ', indentationLength));
            prefixLength += indentationLength;

            string actualPrefix = prefixSb.ToString();
            string blankPrefix = new (' ', prefixLength);

            const char nlc = '\n';
            const string nls = "\n";

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [return: NotNullIfNotNull(nameof(str))]
            static string? ReplaceLineEndings(string? str)
            {
#if NET
                return str?.ReplaceLineEndings(nls);
#else
                return str?.Replace("\r\n", nls).Replace('\r', nlc);
#endif
            }

            string finalMessage = ReplaceLineEndings(message);
            string? finalException = ReplaceLineEndings(exception?.ToString());

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
                        _ => Math.Min(absMaxMessage - indentation, absMaxLine - prefix),
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

            IMessageLineResizer resizer = GetResizer(lineDescriptor.MaxMessageLength, lineDescriptor.MaxLineLength, indentationLength, prefixLength);

            bool first = true;
            foreach (string line in resizer.Resize(finalMessage.Split(nlc)))
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

                textWriter.WriteLine(useColor ? line.Pastel(isActivity ? ConsoleColor.Cyan : ConsoleColor.White) : line);
            }

            if (finalException is not null)
            {
                foreach (string line in resizer.Resize(finalException.Split(nlc)))
                {
                    textWriter.Write(blankPrefix);
                    textWriter.WriteLine(useColor ? line.Pastel(ConsoleColor.DarkRed) : line);
                }
            }

            return true;
        }
        finally
        {
#if NET7_0_OR_GREATER
            WriteDuration.Record(timing = stopwatch.Elapsed.TotalMicroseconds);
#else
            WriteDuration.Record(timing = stopwatch.Elapsed.TotalMilliseconds / 1000);
#endif
        }
    }
}
