using Diginsight.Diagnostics.TextWriting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Pastel;
using System.Diagnostics;

namespace Diginsight.Diagnostics;

internal sealed class DiginsightConsoleFormatter : ConsoleFormatter
{
    public const string FormatterName = "diginsight";

    private readonly IConsoleLineDescriptorProvider lineDescriptorProvider;
    private readonly IDiginsightConsoleFormatterOptions formatterOptions;
    private readonly TimeProvider timeProvider;

    static DiginsightConsoleFormatter()
    {
        ConsoleExtensions.Enable();
    }

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
                ref state,
                out bool isActivity,
                out TimeSpan? duration,
                out DateTimeOffset? maybeTimestamp,
                out Activity? activity,
                out Func<LineDescriptor, LineDescriptor>? sealLineDescriptor
            );

            DateTimeOffset finalTimestamp = maybeTimestamp ?? timeProvider.GetUtcNow();

            int? width;
            try
            {
                width = formatterOptions.TotalWidth switch
                {
                    < 0 => null,
                    0 => Console.WindowWidth,
                    var w => w,
                };
            }
            catch (Exception)
            {
                width = null;
            }

            DiginsightTextWriter.Write(
                textWriter,
                formatterOptions.UseColor,
                TimeZoneInfo.ConvertTime(finalTimestamp, formatterOptions.TimeZone ?? TimeZoneInfo.Local),
                activity ?? Activity.Current,
                logEntry.LogLevel,
                logEntry.Category,
                logEntry.Formatter(logEntry.State, logEntry.Exception),
                logEntry.Exception,
                isActivity,
                duration,
                lineDescriptorProvider.GetLineDescriptor(width),
                sealLineDescriptor
            );
        }
        catch (Exception exception)
        {
            string exceptionText = $"### {exception.GetType().Name} {exception.Message} ###";
            textWriter.WriteLine(
                formatterOptions.UseColor ? exceptionText.Pastel(ConsoleColor.DarkMagenta) : exceptionText
            );
        }
    }
}
