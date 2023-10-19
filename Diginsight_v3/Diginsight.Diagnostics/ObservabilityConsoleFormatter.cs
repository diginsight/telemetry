using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
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
        if (logEntry.State is ObservabilityTextWriter.IOtlpOnly)
        {
            return;
        }

        IObservabilityConsoleFormatterOptions formatterOptions = formatterOptionsMonitor.CurrentValue;

        bool isActivity;
        TimeSpan? duration;
        if (logEntry.State is ObservabilityTextWriter.IActivityMark activityMark)
        {
            isActivity = true;
            duration = activityMark.Duration;
        }
        else
        {
            isActivity = false;
            duration = null;
        }

        Func<TState, Exception?, string> formatter =
#if NET7_0_OR_GREATER
            logEntry.Formatter;
#else
            logEntry.Formatter ?? (static (s, _) => s?.ToString() ?? "");
#endif

        ObservabilityTextWriter.Write(
            textWriter,
            formatterOptions.UseUtcTimestamp ? DateTime.UtcNow : DateTime.Now,
            formatterOptions.TimestampFormat,
            formatterOptions.TimestampCulture is { } cultureName ? CultureInfo.GetCultureInfo(cultureName) : null,
            logEntry.LogLevel,
            logEntry.Category,
            formatterOptions.MaxCategoryLength,
            formatter(logEntry.State, logEntry.Exception),
            logEntry.Exception,
            isActivity,
            duration
        );
    }
}
