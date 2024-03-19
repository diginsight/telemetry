using Diginsight.Diagnostics.TextWriting;
using log4net.Core;
using log4net.Layout;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Diginsight.Diagnostics.Log4Net;

internal sealed class DiginsightLayoutSkeleton : LayoutSkeleton
{
    private readonly ILog4NetLineDescriptorProvider lineDescriptorProvider;
    private readonly IDiginsightLayoutSkeletonOptions layoutSkeletonOptions;

    public DiginsightLayoutSkeleton(
        ILog4NetLineDescriptorProvider lineDescriptorProvider,
        IOptions<DiginsightLayoutSkeletonOptions> layoutSkeletonOptions
    )
    {
        this.lineDescriptorProvider = lineDescriptorProvider;
        this.layoutSkeletonOptions = layoutSkeletonOptions.Value;
    }

    public override void ActivateOptions() { }

    public override void Format(TextWriter writer, LoggingEvent loggingEvent)
    {
        try
        {
            DiginsightLoggingEvent myLoggingEvent = (DiginsightLoggingEvent)loggingEvent;

            DiginsightTextWriter.Write(
                writer,
                layoutSkeletonOptions.UseUtcTimestamp ? loggingEvent.TimeStampUtc : loggingEvent.TimeStamp,
                TranslateLogLevel(loggingEvent.Level),
                myLoggingEvent.LoggerName,
                myLoggingEvent.RenderedMessage,
                loggingEvent.ExceptionObject,
                myLoggingEvent.IsActivity,
                myLoggingEvent.Duration,
                lineDescriptorProvider.GetLineDescriptor()
            );
        }
        catch (Exception exception)
        {
            writer.WriteLine($"### {exception.GetType().Name} {exception.Message} ###");
        }
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
