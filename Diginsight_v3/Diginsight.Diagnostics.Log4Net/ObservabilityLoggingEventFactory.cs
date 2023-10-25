using log4net.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Log4Net.AspNetCore.Entities;
using ILogger = log4net.Core.ILogger;

namespace Diginsight.Diagnostics.Log4Net;

// TODO Register in DI
public sealed class ObservabilityLoggingEventFactory : ILog4NetLoggingEventFactory
{
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

        Func<TState, Exception?, string> formatter =
#if NET7_0_OR_GREATER
            messageCandidate.Formatter;
#else
            messageCandidate.Formatter ?? (static (s, _) => s?.ToString() ?? "");
#endif

        Level? logLevel = options.LogLevelTranslator.TranslateLogLevel(messageCandidate.LogLevel, options);
        if (logLevel == null)
        {
            return null;
        }

        string message = formatter(state, messageCandidate.Exception);
        if (string.IsNullOrEmpty(message) && messageCandidate.Exception is null)
        {
            return null;
        }

        return new LoggingEvent(
            typeof(Microsoft.Extensions.Logging.LoggerExtensions),
            logger.Repository,
            logger.Name,
            logLevel,
            new Log4NetMessage(message, isActivity, duration),
            messageCandidate.Exception
        );
    }
}
