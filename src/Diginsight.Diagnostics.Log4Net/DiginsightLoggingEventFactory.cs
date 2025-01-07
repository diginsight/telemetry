using Diginsight.Diagnostics.TextWriting;
using log4net.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Log4Net.AspNetCore.Entities;
using System.Diagnostics;
using ILogger = log4net.Core.ILogger;

namespace Diginsight.Diagnostics.Log4Net;

internal sealed class DiginsightLoggingEventFactory : ILog4NetLoggingEventFactory
{
    private readonly IServiceProvider serviceProvider;
    private readonly TimeProvider timeProvider;
    private readonly ILog4NetLoggingEventFactory decoratee = new Log4NetLoggingEventFactory();

    public DiginsightLoggingEventFactory(
        IServiceProvider serviceProvider,
        TimeProvider? timeProvider = null
    )
    {
        this.serviceProvider = serviceProvider;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public LoggingEvent? CreateLoggingEvent<TState>(
        in MessageCandidate<TState> messageCandidate,
        ILogger logger,
        Log4NetProviderOptions options,
        IExternalScopeProvider scopeProvider
    )
    {
        try
        {
            object? state = messageCandidate.State;
            DiginsightTextWriter.ExpandState(
                ref state,
                out bool isActivity,
                out TimeSpan? duration,
                out DateTimeOffset? maybeTimestamp,
                out Activity? activity,
                out Func<LineDescriptor, LineDescriptor>? sealLineDescriptor
            );

            LoggingEvent? loggingEvent = decoratee.CreateLoggingEvent(messageCandidate, logger, options, scopeProvider);
            if (loggingEvent is null)
            {
                return null;
            }

            return new DiginsightLoggingEvent(
                serviceProvider,
                loggingEvent,
                isActivity,
                duration,
                maybeTimestamp ?? timeProvider.GetUtcNow(),
                activity ?? Activity.Current,
                sealLineDescriptor
            );
        }
        catch (Exception)
        {
            return null;
        }
    }
}
