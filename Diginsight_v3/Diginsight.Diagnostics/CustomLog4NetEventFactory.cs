using log4net.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Log4Net.AspNetCore.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Text;

namespace Diginsight.Diagnostics
{
    public class CustomLoggingEventFactory : ILog4NetLoggingEventFactory
    {
        public LoggingEvent CreateLoggingEvent<TState>(
            in MessageCandidate<TState> messageCandidate,
            log4net.Core.ILogger logger,
            Log4NetProviderOptions options,
            IExternalScopeProvider scopeProvider)
        {
            Type callerStackBoundaryDeclaringType = typeof(LoggerExtensions);

            string message = messageCandidate.Formatter(
                messageCandidate.State,
            messageCandidate.Exception
            );

            Level logLevel = options.LogLevelTranslator.TranslateLogLevel(
                messageCandidate.LogLevel,
                options
            );

            bool isActivity;
            TimeSpan? duration;
            if (messageCandidate.State is ObservabilityTextWriter.IActivityMark activityMark)
            {
                isActivity = true;
                duration = activityMark.Duration;
            }
            else
            {
                return null;
            }

            if (logLevel == null)
                return null;

            if (string.IsNullOrEmpty(message) && messageCandidate.Exception == null)
                return null;

            return new LoggingEvent(
                callerStackBoundaryDeclaringType: callerStackBoundaryDeclaringType,
                repository: logger.Repository,
                loggerName: logger.Name,
                level: logLevel,
                message: new Log4NetMessage
                {
                    Message = message,
                    IsActivity = isActivity,
                    Duration = duration
                },
                exception: messageCandidate.Exception);
        }
    }
}
