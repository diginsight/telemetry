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

        int width;
        try
        {
            width = Console.WindowWidth;
        }
        catch (Exception)
        {
            width = int.MaxValue;
        }

        DateTimeOffset timestampDto = timeProvider.GetUtcNow();
        ObservabilityTextWriter.Write(
            textWriter,
            formatterOptions.UseUtcTimestamp ? timestampDto.UtcDateTime : timestampDto.LocalDateTime,
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
