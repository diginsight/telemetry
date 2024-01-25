using Diginsight.Diagnostics.TextWriting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace Diginsight.Diagnostics;

internal sealed class ObservabilityConsoleFormatter : ConsoleFormatter
{
    public const string FormatterName = "observability";

    private readonly IConsoleLineDescriptorProvider lineDescriptorProvider;
    private readonly IObservabilityConsoleFormatterOptions formatterOptions;
    private readonly TimeProvider timeProvider;

    public ObservabilityConsoleFormatter(
        IConsoleLineDescriptorProvider lineDescriptorProvider,
        IOptionsMonitor<ObservabilityConsoleFormatterOptions> formatterOptionsMonitor,
        TimeProvider? timeProvider = null
    )
        : base(FormatterName)
    {
        this.lineDescriptorProvider = lineDescriptorProvider;
        formatterOptions = formatterOptionsMonitor.CurrentValue;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider? scopeProvider,
        TextWriter textWriter
    )
    {
        object? innerState = logEntry.State;
        bool isActivity = false;
        TimeSpan? duration = null;
        DateTimeOffset? maybeTimestamp = null;

        while (true)
        {
            if (innerState is ObservabilityTextWriter.IOtlpOnly)
            {
                return;
            }

            if (innerState is ObservabilityTextWriter.IActivityMark activityMark)
            {
                innerState = activityMark.State;
                isActivity = true;
                duration = activityMark.Duration;
            }
            else if (innerState is DeferredLoggerFactory.ITimestamped timestamped)
            {
                innerState = timestamped.State;
                maybeTimestamp = timestamped.Timestamp;
            }
            else
            {
                break;
            }
        }

        DateTimeOffset finalTimestamp = maybeTimestamp ?? timeProvider.GetUtcNow();

        int width;
        try
        {
            width = Console.WindowWidth;
        }
        catch (Exception)
        {
            width = int.MaxValue;
        }

        ObservabilityTextWriter.Write(
            textWriter,
            formatterOptions.UseUtcTimestamp ? finalTimestamp.UtcDateTime : finalTimestamp.LocalDateTime,
            logEntry.LogLevel,
            logEntry.Category,
            logEntry.Formatter(logEntry.State, logEntry.Exception),
            logEntry.Exception,
            isActivity,
            duration,
            lineDescriptorProvider.GetLineDescriptor(width)
        );
    }
}
