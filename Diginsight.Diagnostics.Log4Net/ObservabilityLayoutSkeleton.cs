using Diginsight.Diagnostics.TextWriting;
using log4net.Core;
using log4net.Layout;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Diginsight.Diagnostics.Log4Net;

internal sealed class ObservabilityLayoutSkeleton : LayoutSkeleton
{
    private readonly IOptionsMonitor<ObservabilityLayoutSkeletonOptions> layoutSkeletonOptionsMonitor;

    public ObservabilityLayoutSkeleton(
        IOptionsMonitor<ObservabilityLayoutSkeletonOptions> layoutSkeletonOptionsMonitor
    )
    {
        this.layoutSkeletonOptionsMonitor = layoutSkeletonOptionsMonitor;
    }

    public override void ActivateOptions() { }

    public override void Format(TextWriter writer, LoggingEvent loggingEvent)
    {
        IObservabilityTextWritingOptions textWritingOptions = layoutSkeletonOptionsMonitor.CurrentValue;
        ObservabilityLoggingEvent myLoggingEvent = (ObservabilityLoggingEvent)loggingEvent;

        ObservabilityTextWriter.Write(
            writer,
            textWritingOptions.UseUtcTimestamp ? loggingEvent.TimeStampUtc : loggingEvent.TimeStamp,
            TranslateLogLevel(loggingEvent.Level),
            myLoggingEvent.LoggerName,
            myLoggingEvent.RenderedMessage,
            loggingEvent.ExceptionObject,
            myLoggingEvent.IsActivity,
            myLoggingEvent.Duration,
            textWritingOptions.GetLineDescriptor(null)
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
