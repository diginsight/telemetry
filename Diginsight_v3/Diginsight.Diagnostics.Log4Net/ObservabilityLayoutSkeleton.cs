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

        if (loggingEvent.MessageObject is not Log4NetMessage message)
        {
            return;
        }

        ObservabilityTextWriter.Write(
            writer,
            formatterOptions.UseUtcTimestamp ? loggingEvent.TimeStampUtc : loggingEvent.TimeStamp,
            formatterOptions.TimestampFormat,
            formatterOptions.TimestampCulture is { } cultureName ? CultureInfo.GetCultureInfo(cultureName) : null,
            TranslateLogLevel(loggingEvent.Level),
            "category", // TODO Category
            formatterOptions.MaxCategoryLength,
            message.Message,
            loggingEvent.ExceptionObject,
            message.IsActivity,
            message.Duration
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
