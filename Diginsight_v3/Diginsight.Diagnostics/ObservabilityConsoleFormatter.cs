using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;
using System.Diagnostics;
using System.Globalization;

namespace Diginsight.Diagnostics;

public sealed class ObservabilityConsoleFormatter : ConsoleFormatter
{
    public const string FormatterName = "observability";

    private readonly IOptionsMonitor<ObservabilityConsoleFormatterOptions> formatterOptionsMonitor;

    public ObservabilityConsoleFormatter(
        IOptionsMonitor<ObservabilityConsoleFormatterOptions> formatterOptionsMonitor
    )
        : base(FormatterName)
    {
        this.formatterOptionsMonitor = formatterOptionsMonitor;
    }

    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider? scopeProvider,
        TextWriter textWriter
    )
    {
        if (logEntry.State is IBypassConsole)
        {
            return;
        }

        IObservabilityConsoleFormatterOptions formatterOptions = formatterOptionsMonitor.CurrentValue;

        Activity? activity = Activity.Current;
        int depth = GetDepth(activity);

        bool isActivityMark;
        double? durationMsec;
        if (logEntry.State is IActivityMark activityMark)
        {
            isActivityMark = true;
            durationMsec = activityMark.Duration?.TotalMilliseconds;
        }
        else
        {
            isActivityMark = false;
            durationMsec = null;
        }

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

        string indentation = new string(' ', depth * 2 - (isActivityMark ? 1 : 0));
        Func<TState, Exception?, string> formatter =
#if NET7_0_OR_GREATER
            logEntry.Formatter;
#else
            logEntry.Formatter ?? (static (s, _) => s?.ToString() ?? "");
#endif
        string message = formatter(logEntry.State, logEntry.Exception);

        string category;
        if (formatterOptions.MaxCategoryLength is >= 1 and var maxCategoryLength)
        {
            string tempCategory = logEntry.Category;
            if (tempCategory.Length < maxCategoryLength)
            {
                category = tempCategory.PadRight(maxCategoryLength);
            }
            else if (tempCategory.Length > maxCategoryLength)
            {
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                category = $"…{tempCategory[^(maxCategoryLength - 1)..]}";
#else
                category = $"…{tempCategory.Substring(tempCategory.Length - (maxCategoryLength - 1))}";
#endif
            }
            else
            {
                category = tempCategory;
            }
        }
        else
        {
            category = "";
        }

        string logLevel = logEntry.LogLevel switch
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

        DateTime timestamp = formatterOptions.UseUtcTimestamp ? DateTime.UtcNow : DateTime.Now;

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
            activity.SetCustomProperty(lastWasStartCustomPropertyName, isActivityMark && durationMsec is null);

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
        CultureInfo timestampCulture = formatterOptions.TimestampCulture is { } cultureName ? CultureInfo.GetCultureInfo(cultureName) : CultureInfo.InvariantCulture;

        string actualPrefix = string.Format(
            CultureInfo.InvariantCulture,
            "[{0}] {1} {2} {3,32} {4,5} {5,5} {6,2} {7}",
            timestamp.ToString(formatterOptions.TimestampFormat ?? "yyyy-MM-dd'T'HH:mm:ss.fff", timestampCulture),
            category,
            logLevel,
            traceId,
            FormatDuration(deltaMsec),
            FormatDuration(durationMsec),
            depth,
            indentation
        );
        string blankPrefix = new string(' ', actualPrefix.Length);

        string fullMessage = message;
        if (logEntry.Exception is { } exception)
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

    private static int GetDepth(Activity? activity)
    {
        int depth = 0;
        for (; activity is not null; activity = activity.Parent)
        {
            depth++;
        }

        return depth;
    }
}
