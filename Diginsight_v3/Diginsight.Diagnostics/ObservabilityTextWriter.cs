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
        int maxCategoryLength,
        string message,
        Exception? exception,
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

        string indentation = new string(' ', depth * 2 - (isActivity ? 1 : 0));

        string finalCategory;
        if (maxCategoryLength >= 1)
        {
            if (category.Length < maxCategoryLength)
            {
                finalCategory = category.PadRight(maxCategoryLength);
            }
            else if (category.Length > maxCategoryLength)
            {
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                finalCategory = $"…{category[^(maxCategoryLength - 1)..]}";
#else
                finalCategory = $"…{category.Substring(category.Length - (maxCategoryLength - 1))}";
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
        string blankPrefix = new string(' ', actualPrefix.Length);

        string fullMessage = message;
        if (exception is not null)
        {
            activity.RecordException(exception);
            fullMessage += $"\n{exception}";
        }
        fullMessage = fullMessage.Replace("\r", "");

        bool first = true;
        foreach (string line in fullMessage.Split('\n'))
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

    public interface IActivityMark
    {
        TimeSpan? Duration { get; }
    }

    public interface IOtlpOnly { }
}
