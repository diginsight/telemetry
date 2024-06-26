﻿using Microsoft.Extensions.Logging;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
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
        ref object? state, out bool isActivity, out TimeSpan? duration, out DateTimeOffset? maybeTimestamp, out Activity? activity
    )
    {
        isActivity = false;
        duration = null;
        maybeTimestamp = null;
        activity = null;

        while (true)
        {
            if (state is ActivityLifecycleLogEmitter.IActivityMark activityMark)
            {
                state = activityMark.State;
                isActivity = true;
                duration = activityMark.Duration;
                activity ??= activityMark.Activity;
            }
            else if (state is DeferredLoggerFactory.IDeferred timestamped)
            {
                state = timestamped.State;
                maybeTimestamp = timestamped.Timestamp;
                activity = timestamped.Activity;
            }
            else
            {
                break;
            }
        }
    }

    public static void Write(
        TextWriter textWriter,
        DateTime timestamp,
        Activity? activity,
        LogLevel logLevel,
        string category,
        string message,
        Exception? exception,
        bool isActivity,
        TimeSpan? duration,
        LineDescriptor lineDescriptor
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
                lineDescriptor = new (mutableLineDescriptor);
            }
            else if (lineDescriptor.MaxLineLength is (> 7 or < -7) and var maxLineLength)
            {
                MutableLineDescriptor mutableLineDescriptor = new (lineDescriptor)
                {
                    MaxLineLength = (Math.Abs(maxLineLength) - 7) * Math.Sign(maxLineLength),
                };
                lineDescriptor = new (mutableLineDescriptor);
            }

            using StringWriter stringWriter = new ();
            Write(stringWriter, timestamp, activity, logLevel, category, message, exception, isActivity, duration, lineDescriptor, out double timing);

            textWriter.Write("{0,5}µ {1}", ((long)timing).ToString(CultureInfo.InvariantCulture), stringWriter);
        }
        else
        {
            Write(textWriter, timestamp, activity, logLevel, category, message, exception, isActivity, duration, lineDescriptor, out _);
        }
    }

    public static void Write(
        TextWriter textWriter,
        DateTime timestamp,
        Activity? activity,
        LogLevel logLevel,
        string category,
        string message,
        Exception? exception,
        bool isActivity,
        TimeSpan? duration,
        LineDescriptor lineDescriptor,
        out double timing
    )
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        try
        {
            LinePrefixData linePrefixData = new (timestamp, logLevel, category, isActivity, duration, activity);

            StringBuilder prefixSb = new ();
            foreach (IPrefixTokenAppender appender in lineDescriptor.Appenders)
            {
                appender.Append(prefixSb, linePrefixData);
                prefixSb.Append(' ');
            }

            int depth = linePrefixData.Activity.GetDepth().Local;
            int maxIndentedDepth = lineDescriptor.MaxIndentedDepth;
            int indentationLength = maxIndentedDepth < 0 || depth <= maxIndentedDepth
                ? depth * 2 - (linePrefixData.IsActivity ? 1 : 0)
                : maxIndentedDepth * 2;

            prefixSb.Append(new string(' ', indentationLength));

            string actualPrefix = prefixSb.ToString();
            int prefixLength = actualPrefix.Length;
            string blankPrefix = new (' ', prefixLength);

            const char newLine = '\n';

            StringBuilder fullMessageSb = new (message);
            if (exception is not null)
            {
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
#if NET7_0_OR_GREATER
            WriteDuration.Record(timing = stopwatch.Elapsed.TotalMicroseconds);
#else
            WriteDuration.Record(timing = stopwatch.Elapsed.TotalMilliseconds / 1000);
#endif
        }
    }
}
