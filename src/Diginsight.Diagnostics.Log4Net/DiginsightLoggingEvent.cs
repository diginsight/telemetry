using log4net.Core;

namespace Diginsight.Diagnostics.Log4Net;

internal sealed class DiginsightLoggingEvent : LoggingEvent
{
    public IServiceProvider ServiceProvider { get; }

    public bool IsActivity { get; }

    public TimeSpan? Duration { get; }

    public DiginsightLoggingEvent(
        IServiceProvider serviceProvider,
        LoggingEvent wrapped,
        bool isActivity,
        TimeSpan? duration,
        DateTimeOffset timestamp
    )
        : base(TransformLoggingEventData(wrapped, timestamp))
    {
        ServiceProvider = serviceProvider;
        IsActivity = isActivity;
        Duration = duration;
    }

    private static LoggingEventData TransformLoggingEventData(LoggingEvent loggingEvent, DateTimeOffset timestamp)
    {
        LoggingEventData data = loggingEvent.GetLoggingEventData(FixFlags.All);
        data.TimeStampUtc = timestamp.UtcDateTime;
        return data;
    }
}
