using Diginsight.Diagnostics.TextWriting;
using log4net.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Log4Net.AspNetCore.Entities;
using ILogger = log4net.Core.ILogger;

namespace Diginsight.Diagnostics.Log4Net;

internal sealed class ObservabilityLoggingEventFactory : ILog4NetLoggingEventFactory
{
    private readonly TimeProvider timeProvider;
    private readonly ILog4NetLoggingEventFactory decoratee = new Log4NetLoggingEventFactory();

    public ObservabilityLoggingEventFactory(TimeProvider? timeProvider = null)
    {
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public LoggingEvent? CreateLoggingEvent<TState>(
        in MessageCandidate<TState> messageCandidate,
        ILogger logger,
        Log4NetProviderOptions options,
        IExternalScopeProvider scopeProvider
    )
    {
        object? innerState = messageCandidate.State;
        bool isActivity = false;
        TimeSpan? duration = null;
        DateTimeOffset? maybeTimestamp = null;

        while (true)
        {
            if (innerState is ObservabilityTextWriter.IOtlpOnly)
            {
                return null;
            }

            if (innerState is ObservabilityTextWriter.IActivityMark activityMark)
            {
                innerState = activityMark.State;
                isActivity = true;
                duration = activityMark.Duration;
            }
            else if (innerState is DeferredLoggerFactory.ITimestamped timestamped)
            {
                innerState = timestamped.State;
                maybeTimestamp = timestamped.Timestamp;
            }
            else
            {
                break;
            }
        }

        LoggingEvent? loggingEvent = decoratee.CreateLoggingEvent(messageCandidate, logger, options, scopeProvider);
        if (loggingEvent is null)
        {
            return null;
        }

        return new ObservabilityLoggingEvent(
            loggingEvent,
            isActivity,
            duration,
            maybeTimestamp ?? timeProvider.GetUtcNow()
        );
    }
}
