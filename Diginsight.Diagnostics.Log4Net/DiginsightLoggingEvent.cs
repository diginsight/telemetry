using log4net.Core;

namespace Diginsight.Diagnostics.Log4Net;

internal sealed class DiginsightLoggingEvent : LoggingEvent
{
    public bool IsActivity { get; }

    public TimeSpan? Duration { get; }

    public DiginsightLoggingEvent(LoggingEvent wrapped, bool isActivity, TimeSpan? duration, DateTimeOffset timestamp)
        : base(TransformLoggingEventData(wrapped, timestamp))
    {
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
