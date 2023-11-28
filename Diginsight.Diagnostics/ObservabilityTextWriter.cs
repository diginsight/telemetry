using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using System.Diagnostics;
using System.Globalization;
#if NET7_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace Diginsight.Diagnostics;

public static class ObservabilityTextWriter
{
    public static void Write(
        TextWriter textWriter,
        DateTime timestamp,
#if NET7_0_OR_GREATER
        [StringSyntax(StringSyntaxAttribute.DateTimeFormat)]
#endif
        string? timestampFormat,
        CultureInfo? timestampCulture,
        LogLevel logLevel,
        string category,
        string message,
        Exception? exception,
        int categoryLength,
        int maxMessageLength,
        int maxLineLength,
        int maxIndentedDepth,
        bool isActivity,
        TimeSpan? duration
    )
    {
        Activity? activity = Activity.Current;
        int depth = GetDepth(activity);

        static int GetDepth(Activity? activity)
        {
            int depth = 0;
            for (; activity is not null; activity = activity.Parent)
            {
                depth++;
            }

            return depth;
        }

        double? durationMsec = duration?.TotalMilliseconds;

        static string FormatDuration(double? durationMsec)
        {
            return durationMsec switch
            {
                null => "",
                < 1 => string.Format(CultureInfo.InvariantCulture, ".{0:000}m", durationMsec.Value * 1000),
                < 10000 => string.Format(CultureInfo.InvariantCulture, "{0:0}m", durationMsec.Value),
                < 100000 => string.Format(CultureInfo.InvariantCulture, "{0}s", Math.Round(durationMsec.Value / 1000, 1)),
                _ => string.Format(CultureInfo.InvariantCulture, "{0:0}s", durationMsec.Value / 1000),
            };
        }

        int indentationLength = maxIndentedDepth < 0 || depth <= maxIndentedDepth
            ? depth * 2 - (isActivity ? 1 : 0)
            : maxIndentedDepth * 2;
        string indentation = new string(' ', indentationLength);

        const char ellipsisGlyph = '…';

        string finalCategory;
        if (categoryLength >= 1)
        {
            if (category.Length < categoryLength)
            {
                finalCategory = category.PadRight(categoryLength);
            }
            else if (category.Length > categoryLength)
            {
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                finalCategory = $"{ellipsisGlyph}{category[^(categoryLength - 1)..]}";
#else
                finalCategory = $"{ellipsisGlyph}{category.Substring(category.Length - (categoryLength - 1))}";
#endif
            }
            else
            {
                finalCategory = category;
            }
        }
        else
        {
            finalCategory = "";
        }

        string logLevelStr = logLevel switch
        {
            LogLevel.Trace => "TRCE",
            LogLevel.Debug => "DBUG",
            LogLevel.Information => "INFO",
            LogLevel.Warning => "WARN",
            LogLevel.Error => "EROR",
            LogLevel.Critical => "CRIT",
            LogLevel.None => throw new InvalidOperationException($"Unexpected {nameof(LogLevel)}"),
            _ => throw new UnreachableException($"Unrecognized {nameof(LogLevel)}"),
        };

        const string lastLogTimestampCustomPropertyName = "lastLogTimestamp";
        const string lastWasStartCustomPropertyName = "lastWasStart";

        DateTime? prevTimestamp;
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

        ActivityTraceId? traceId;
        bool lastWasStart;
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

        double? deltaMsec = lastWasStart ? null : (timestamp - prevTimestamp)?.TotalMilliseconds;

        string actualPrefix = string.Format(
            CultureInfo.InvariantCulture,
            "[{0}] {1} {2} {3,32} {4,5} {5,5} {6,2} {7}",
            timestamp.ToString(timestampFormat ?? "yyyy-MM-dd'T'HH:mm:ss.fff", timestampCulture ?? CultureInfo.InvariantCulture),
            finalCategory,
            logLevelStr,
            traceId,
            FormatDuration(deltaMsec),
            FormatDuration(durationMsec),
            depth,
            indentation
        );
        int prefixLength = actualPrefix.Length;
        string blankPrefix = new string(' ', prefixLength);

        const char newLine = '\n';

        string fullMessage = message;
        if (exception is not null)
        {
            activity.RecordException(exception);
            fullMessage += $"{newLine}{exception}";
        }
        fullMessage = fullMessage.Replace("\r", "");

        int finalMaxMessageLength = CalculateFinalMaxMessageLength(maxMessageLength, maxLineLength, indentationLength, prefixLength, out bool chop);

        static int CalculateFinalMaxMessageLength(int maxMessage, int maxLine, int indentation, int prefix, out bool chop)
        {
            chop = maxMessage < 0 || maxLine < 0;

            int absMaxLine = Math.Abs(maxLine);
            int absMaxMessage = Math.Abs(maxMessage);

            if (absMaxLine == 0)
            {
                return absMaxMessage - indentation;
            }
            if (absMaxMessage == 0)
            {
                return absMaxLine - prefix;
            }
            return Math.Min(absMaxLine, prefix + absMaxMessage - indentation) - prefix;
        }

        bool first = true;
        foreach (string line in ResizeMessage(fullMessage.Split(newLine), finalMaxMessageLength, chop))
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

        static IEnumerable<string> ResizeMessage(IEnumerable<string> lines, int maxLength, bool chop)
        {
            if (maxLength == 0)
            {
                return lines;
            }

            if (maxLength < 10)
            {
                maxLength = 10;
                chop = true;
            }

            Stack<string> inputLines = new (lines.Reverse());
            ICollection<string> outputLines = new List<string>();

#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            while (inputLines.TryPop(out string? line))
            {
#else
            while (inputLines.Count > 0)
            {
                string line = inputLines.Pop();
#endif
                if (line.Length <= maxLength)
                {
                    outputLines.Add(line);
                }
                else if (chop)
                {
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                    outputLines.Add($"{line[..(maxLength - 1)]}{ellipsisGlyph}");
#else
                    outputLines.Add($"{line.Substring(0, maxLength - 1)}{ellipsisGlyph}");
#endif
                }
                else
                {
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                    outputLines.Add($"{line[..(maxLength - 1)]}↵");
                    inputLines.Push(line[(maxLength - 1)..]);
#else
                    outputLines.Add($"{line.Substring(0, maxLength - 1)}↵");
                    inputLines.Push(line.Substring(maxLength - 1));
#endif
                }
            }

            return outputLines;
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
    }

    public interface IOtlpOnly
    {
        Tags State { get; }
    }
}
