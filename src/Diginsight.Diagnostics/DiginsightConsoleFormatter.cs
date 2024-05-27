using Diginsight.Diagnostics.TextWriting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using System.Diagnostics;

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
            object? state = logEntry.State;
            DiginsightTextWriter.ExpandState(
                ref state, out bool isActivity, out TimeSpan? duration, out DateTimeOffset? maybeTimestamp, out Activity? activity
            );

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
                activity ?? Activity.Current,
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
