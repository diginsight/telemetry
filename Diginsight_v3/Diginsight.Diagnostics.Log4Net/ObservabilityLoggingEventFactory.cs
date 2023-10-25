using log4net.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Log4Net.AspNetCore.Entities;
using ILogger = log4net.Core.ILogger;

namespace Diginsight.Diagnostics.Log4Net;

internal sealed class ObservabilityLoggingEventFactory : ILog4NetLoggingEventFactory
{
    private readonly ILog4NetLoggingEventFactory decoratee = new Log4NetLoggingEventFactory();

    public LoggingEvent? CreateLoggingEvent<TState>(
        in MessageCandidate<TState> messageCandidate,
        ILogger logger,
        Log4NetProviderOptions options,
        IExternalScopeProvider scopeProvider
    )
    {
        TState state = messageCandidate.State;
        if (state is ObservabilityTextWriter.IOtlpOnly)
        {
            return null;
        }

        LoggingEvent? loggingEvent = decoratee.CreateLoggingEvent(messageCandidate, logger, options, scopeProvider);
        if (loggingEvent is null)
        {
            return null;
        }

        bool isActivity;
        TimeSpan? duration;
        if (state is ObservabilityTextWriter.IActivityMark activityMark)
        {
            isActivity = true;
            duration = activityMark.Duration;
        }
        else
        {
            isActivity = false;
            duration = null;
        }

        return new ObservabilityLoggingEvent(loggingEvent, isActivity, duration);
    }
}
