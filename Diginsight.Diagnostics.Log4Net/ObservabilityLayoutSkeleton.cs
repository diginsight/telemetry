using log4net.Core;
using log4net.Layout;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace Diginsight.Diagnostics.Log4Net;

internal sealed class ObservabilityLayoutSkeleton : LayoutSkeleton
{
    private readonly IOptionsMonitor<ObservabilityConsoleFormatterOptions> formatterOptionsMonitor;

    public ObservabilityLayoutSkeleton(
        IOptionsMonitor<ObservabilityConsoleFormatterOptions> formatterOptionsMonitor
    )
    {
        this.formatterOptionsMonitor = formatterOptionsMonitor;
    }

    public override void ActivateOptions() { }

    public override void Format(TextWriter writer, LoggingEvent loggingEvent)
    {
        IObservabilityConsoleFormatterOptions formatterOptions = formatterOptionsMonitor.CurrentValue;
        ObservabilityLoggingEvent myLoggingEvent = (ObservabilityLoggingEvent)loggingEvent;

        ObservabilityTextWriter.Write(
            writer,
            formatterOptions.UseUtcTimestamp ? loggingEvent.TimeStampUtc : loggingEvent.TimeStamp,
            formatterOptions.TimestampFormat,
            formatterOptions.TimestampCulture is { } cultureName ? CultureInfo.GetCultureInfo(cultureName) : null,
            TranslateLogLevel(loggingEvent.Level),
            myLoggingEvent.LoggerName,
            formatterOptions.MaxCategoryLength,
            myLoggingEvent.RenderedMessage,
            loggingEvent.ExceptionObject,
            myLoggingEvent.IsActivity,
            myLoggingEvent.Duration
        );
    }

    private static LogLevel TranslateLogLevel(Level level)
    {
        return level >= Level.Critical ? LogLevel.Critical
            : level >= Level.Error ? LogLevel.Error
            : level >= Level.Warn ? LogLevel.Warning
            : level >= Level.Info ? LogLevel.Information
            : level >= Level.Debug ? LogLevel.Debug
            : LogLevel.Trace;
    }
}
