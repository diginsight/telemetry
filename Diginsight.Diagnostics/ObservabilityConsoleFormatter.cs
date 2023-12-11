using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace Diginsight.Diagnostics;

internal sealed class ObservabilityConsoleFormatter : ConsoleFormatter
{
    public const string FormatterName = "observability";

    private readonly IOptionsMonitor<ObservabilityTextFormatterOptions> formatterOptionsMonitor;

    public ObservabilityConsoleFormatter(
        IOptionsMonitor<ObservabilityTextFormatterOptions> formatterOptionsMonitor
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
        TState state = logEntry.State;
        if (state is ObservabilityTextWriter.IOtlpOnly)
        {
            return;
        }

        bool isActivity;
        TimeSpan? duration;
        if (state is ObservabilityTextWriter.IActivityMark activityMark)
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

        IObservabilityTextFormatterOptions formatterOptions = formatterOptionsMonitor.CurrentValue;

        ObservabilityTextWriter.Write(
            textWriter,
            formatterOptions.UseUtcTimestamp ? DateTime.UtcNow : DateTime.Now,
            logEntry.LogLevel,
            logEntry.Category,
            formatter(state, logEntry.Exception),
            logEntry.Exception,
            isActivity,
            duration,
            formatterOptions
        );
    }
}
