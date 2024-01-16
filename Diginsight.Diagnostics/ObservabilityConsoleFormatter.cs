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
        TState state = logEntry.State;
        if (state is ObservabilityTextWriter.IOtlpOnly)
        {
            return;
        }

        object? innerState;
        bool isActivity;
        TimeSpan? duration;
        if (state is ObservabilityTextWriter.IActivityMark activityMark)
        {
            innerState = activityMark.State;
            isActivity = true;
            duration = activityMark.Duration;
        }
        else
        {
            innerState = state;
            isActivity = false;
            duration = null;
        }

        int width;
        try
        {
            width = Console.WindowWidth;
        }
        catch (Exception)
        {
            width = int.MaxValue;
        }

        DateTimeOffset timestampDto = innerState is DeferredLoggerFactory.ITimestamped timestamped
            ? timestamped.Timestamp
            : timeProvider.GetUtcNow();
        DateTime timestampDt = formatterOptions.UseUtcTimestamp ? timestampDto.UtcDateTime : timestampDto.LocalDateTime;

        ObservabilityTextWriter.Write(
            textWriter,
            timestampDt,
            logEntry.LogLevel,
            logEntry.Category,
            logEntry.Formatter(state, logEntry.Exception),
            logEntry.Exception,
            isActivity,
            duration,
            lineDescriptorProvider.GetLineDescriptor(width)
        );
    }
}
