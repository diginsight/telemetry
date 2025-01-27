using Diginsight.Diagnostics.TextWriting;
using log4net.Core;
using log4net.Layout;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Diginsight.Diagnostics.Log4Net;

public sealed class DiginsightLayout : ILayout
{
    private LineDescriptor? lineDescriptor;

    public TimeZoneInfo? TimeZone { get; set; } = TimeZoneInfo.Utc;

    public string? TimeZoneName
    {
        get => TimeZone?.Id;
        set => TimeZone = value is null ? null : TimeZoneInfo.FindSystemTimeZoneById(value);
    }

    public string? Pattern { get; set; }

    string ILayout.ContentType => "text/plain";
    string? ILayout.Header => null;
    string? ILayout.Footer => null;
    bool ILayout.IgnoresException => true;

    public void Format(TextWriter writer, LoggingEvent loggingEvent)
    {
        try
        {
            DiginsightLoggingEvent myLoggingEvent = (DiginsightLoggingEvent)loggingEvent;

            // ReSharper disable once LocalVariableHidesMember
            if (this.lineDescriptor is not { } lineDescriptor)
            {
                IServiceProvider serviceProvider = myLoggingEvent.ServiceProvider;
                IEnumerable<ILineTokenParser> customLineTokenParsers = serviceProvider.GetRequiredService<IEnumerable<ILineTokenParser>>();
                this.lineDescriptor = lineDescriptor = LineDescriptor.ParseFull(Pattern, customLineTokenParsers);
            }

            DiginsightTextWriter.Write(
                writer,
                false,
                TimeZoneInfo.ConvertTime(new DateTimeOffset(loggingEvent.TimeStampUtc), TimeZone ?? TimeZoneInfo.Local),
                myLoggingEvent.Activity,
                TranslateLogLevel(loggingEvent.Level),
                myLoggingEvent.LoggerName,
                myLoggingEvent.RenderedMessage,
                loggingEvent.ExceptionObject,
                myLoggingEvent.IsActivity,
                myLoggingEvent.Duration,
                lineDescriptor,
                myLoggingEvent.SealLineDescriptor
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
