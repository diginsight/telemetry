using Diginsight.Runtime;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics;

[EditorBrowsable(EditorBrowsableState.Never)]
public static partial class ActivitySourceExtensions
{
    extension(ActivitySource activitySource)
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private Activity? CoreCreateRichActivity(
            ILogger? logger,
            Func<object>? makeInputs,
            string activityNameHint,
            bool isStandalone,
            ActivityKind activityKind,
            LogLevel? logLevel,
            bool start
        )
        {
            if (!activitySource.HasListeners())
            {
                return null;
            }

            Type callerType = RuntimeUtils.GetCallerType(2);

            string finalActivityName;
            if (isStandalone)
            {
                finalActivityName = activityNameHint;
            }
            else
            {
                string? localFunctionName = RuntimeUtils.GetCallerName(2).LocalFunction;
                string fullCallerMemberName = localFunctionName switch
                {
                    "" => $"{activityNameHint}+<anon>",
                    not null => $"{activityNameHint}+{localFunctionName}",
                    null => activityNameHint,
                };
                finalActivityName = $"{callerType.Name}.{fullCallerMemberName}";
            }

            return activitySource.CoreCreateRichActivity(logger, makeInputs, finalActivityName, callerType, isStandalone, activityKind, logLevel, start);
        }

        private Activity? CoreCreateRichActivity(
            ILogger? logger,
            Func<object>? makeInputs,
            string activityName,
            Type callerType,
            bool isStandalone,
            ActivityKind activityKind,
            LogLevel? logLevel,
            bool start
        )
        {
            if (activitySource.CreateActivity(activityName, activityKind) is not { } activity)
            {
                return null;
            }

            activity.SetCustomProperty(ActivityCustomPropertyNames.Logger, logger);
            activity.SetCustomProperty(ActivityCustomPropertyNames.LogLevel, logLevel);
            activity.SetCustomProperty(ActivityCustomPropertyNames.MakeInputs, makeInputs);
            activity.SetCustomProperty(ActivityCustomPropertyNames.CallerType, callerType);
            activity.SetCustomProperty(ActivityCustomPropertyNames.IsStandalone, isStandalone);

            if (start)
            {
                activity.Start();
            }

            return activity;
        }
    }
}
