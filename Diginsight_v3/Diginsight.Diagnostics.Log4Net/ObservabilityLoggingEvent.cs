using log4net.Core;

namespace Diginsight.Diagnostics.Log4Net;

internal sealed class ObservabilityLoggingEvent : LoggingEvent
{
    public bool IsActivity { get; }

    public TimeSpan? Duration { get; }

    public ObservabilityLoggingEvent(LoggingEvent wrapped, bool isActivity, TimeSpan? duration)
        : base(
            typeof(Microsoft.Extensions.Logging.LoggerExtensions),
            wrapped.Repository,
            wrapped.LoggerName,
            wrapped.Level,
            wrapped.MessageObject,
            wrapped.ExceptionObject
        )
    {
        IsActivity = isActivity;
        Duration = duration;
    }
}
