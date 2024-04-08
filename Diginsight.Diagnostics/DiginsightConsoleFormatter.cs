using Diginsight.Diagnostics.TextWriting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace Diginsight.Diagnostics;

internal sealed class DiginsightConsoleFormatter : ConsoleFormatter
{
    public const string FormatterName = "diginsight";

    private readonly IConsoleLineDescriptorProvider lineDescriptorProvider;
    private readonly IDiginsightConsoleFormatterOptions formatterOptions;
    private readonly TimeProvider timeProvider;

    public DiginsightConsoleFormatter(
        IConsoleLineDescriptorProvider lineDescriptorProvider,
        IOptionsMonitor<DiginsightConsoleFormatterOptions> formatterOptionsMonitor,
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
        try
        {
            object? innerState = logEntry.State;
            bool isActivity = false;
            TimeSpan? duration = null;
            DateTimeOffset? maybeTimestamp = null;

            while (true)
            {
                if (innerState is ActivityLifecycleLogEmitter.IActivityMark activityMark)
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

            DiginsightTextWriter.Write(
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
        catch (Exception exception)
        {
            textWriter.WriteLine($"### {exception.GetType().Name} {exception.Message} ###");
        }
    }
}
