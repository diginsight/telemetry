using Diginsight.Diagnostics.TextWriting;
using log4net.Core;
using log4net.Layout;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Diginsight.Diagnostics.Log4Net;

/// <summary>
/// Represents a Log4Net layout that formats logging events with Diginsight text output.
/// </summary>
public sealed class DiginsightLayout : ILayout
{
    private static readonly string FallbackLoggerName = $"{typeof(DiginsightLayout).Namespace!}.$Layout";

    private LineDescriptor? lineDescriptor;

    /// <summary>
    /// Gets the time zone used to format timestamps.
    /// </summary>
    public TimeZoneInfo? TimeZone { get; set; } = TimeZoneInfo.Utc;

    /// <summary>
    /// Gets the system time zone identifier used to format timestamps.
    /// </summary>
    public string? TimeZoneId
    {
        get => TimeZone?.Id;
        set => TimeZone = value is null ? null : TimeZoneInfo.FindSystemTimeZoneById(value);
    }

    /// <summary>
    /// Gets the line descriptor pattern used to format log output.
    /// </summary>
    /// <remarks>
    /// The pattern is parsed according to <see cref="Diginsight.Diagnostics.TextWriting.LineDescriptor" />. When <c>null</c>, the default line descriptor is used.
    /// </remarks>
    public string? Pattern { get; set; }

    string ILayout.ContentType => "text/plain";
    string? ILayout.Header => null;
    string? ILayout.Footer => null;
    bool ILayout.IgnoresException => true;

    /// <inheritdoc />
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
                myLoggingEvent.LoggerName ?? FallbackLoggerName,
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

    private static LogLevel TranslateLogLevel(Level? level)
    {
        return level is null ? LogLevel.Trace
            : level >= Level.Critical ? LogLevel.Critical
            : level >= Level.Error ? LogLevel.Error
            : level >= Level.Warn ? LogLevel.Warning
            : level >= Level.Info ? LogLevel.Information
            : level >= Level.Debug ? LogLevel.Debug
            : LogLevel.Trace;
    }
}
