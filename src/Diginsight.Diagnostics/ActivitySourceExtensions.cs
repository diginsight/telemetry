using Diginsight.Runtime;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class ActivitySourceExtensions
{
    extension(ActivitySource activitySource)
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Activity? CreateRichActivity(
            string activityName,
            object inputs,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null
        )
        {
            if (inputs is null)
            {
                throw new ArgumentNullException(nameof(inputs));
            }

            return activitySource.CoreCreateRichActivity(null, () => inputs, activityName, true, activityKind, logLevel, false);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Activity? CreateRichActivity(
            string activityName,
            Func<object>? makeInputs = null,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null
        )
        {
            return activitySource.CoreCreateRichActivity(null, makeInputs, activityName, true, activityKind, logLevel, false);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Activity? CreateRichActivity(
            ILogger logger,
            string activityName,
            object inputs,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null
        )
        {
            if (inputs is null)
            {
                throw new ArgumentNullException(nameof(inputs));
            }

            return activitySource.CoreCreateRichActivity(logger, () => inputs, activityName, true, activityKind, logLevel, false);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Activity? CreateRichActivity(
            ILogger logger,
            string activityName,
            Func<object>? makeInputs = null,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null
        )
        {
            return activitySource.CoreCreateRichActivity(logger, makeInputs, activityName, true, activityKind, logLevel, false);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Activity? StartRichActivity(
            string activityName,
            object inputs,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null
        )
        {
            if (inputs is null)
            {
                throw new ArgumentNullException(nameof(inputs));
            }

            return activitySource.CoreCreateRichActivity(null, () => inputs, activityName, true, activityKind, logLevel, true);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Activity? StartRichActivity(
            string activityName,
            Func<object>? makeInputs = null,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null
        )
        {
            return activitySource.CoreCreateRichActivity(null, makeInputs, activityName, true, activityKind, logLevel, true);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Activity? StartRichActivity(
            ILogger logger,
            string activityName,
            object inputs,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null
        )
        {
            if (inputs is null)
            {
                throw new ArgumentNullException(nameof(inputs));
            }

            return activitySource.CoreCreateRichActivity(logger, () => inputs, activityName, true, activityKind, logLevel, true);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Activity? StartRichActivity(
            ILogger logger,
            string activityName,
            Func<object>? makeInputs = null,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null
        )
        {
            return activitySource.CoreCreateRichActivity(logger, makeInputs, activityName, true, activityKind, logLevel, true);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Activity? CreateMethodActivity(
            object inputs,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null,
            [CallerMemberName] string callerMemberName = ""
        )
        {
            if (inputs is null)
            {
                throw new ArgumentNullException(nameof(inputs));
            }

            return activitySource.CoreCreateRichActivity(null, () => inputs, callerMemberName, false, activityKind, logLevel, false);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Activity? CreateMethodActivity(
            Func<object>? makeInputs = null,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null,
            [CallerMemberName] string callerMemberName = ""
        )
        {
            return activitySource.CoreCreateRichActivity(null, makeInputs, callerMemberName, false, activityKind, logLevel, false);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Activity? CreateMethodActivity(
            ILogger logger,
            object inputs,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null,
            [CallerMemberName] string callerMemberName = ""
        )
        {
            if (inputs is null)
            {
                throw new ArgumentNullException(nameof(inputs));
            }

            return activitySource.CoreCreateRichActivity(logger, () => inputs, callerMemberName, false, activityKind, logLevel, false);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Activity? CreateMethodActivity(
            ILogger logger,
            Func<object>? makeInputs = null,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null,
            [CallerMemberName] string callerMemberName = ""
        )
        {
            return activitySource.CoreCreateRichActivity(logger, makeInputs, callerMemberName, false, activityKind, logLevel, false);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Activity? StartMethodActivity(
            object inputs,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null,
            [CallerMemberName] string callerMemberName = ""
        )
        {
            if (inputs is null)
            {
                throw new ArgumentNullException(nameof(inputs));
            }

            return activitySource.CoreCreateRichActivity(null, () => inputs, callerMemberName, false, activityKind, logLevel, true);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Activity? StartMethodActivity(
            Func<object>? makeInputs = null,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null,
            [CallerMemberName] string callerMemberName = ""
        )
        {
            return activitySource.CoreCreateRichActivity(null, makeInputs, callerMemberName, false, activityKind, logLevel, true);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Activity? StartMethodActivity(
            ILogger logger,
            object inputs,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null,
            [CallerMemberName] string callerMemberName = ""
        )
        {
            if (inputs is null)
            {
                throw new ArgumentNullException(nameof(inputs));
            }

            return activitySource.CoreCreateRichActivity(logger, () => inputs, callerMemberName, false, activityKind, logLevel, true);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Activity? StartMethodActivity(
            ILogger logger,
            Func<object>? makeInputs = null,
            ActivityKind activityKind = ActivityKind.Internal,
            LogLevel? logLevel = null,
            [CallerMemberName] string callerMemberName = ""
        )
        {
            return activitySource.CoreCreateRichActivity(logger, makeInputs, callerMemberName, false, activityKind, logLevel, true);
        }

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

            Activity? activity = activitySource.CreateActivity(finalActivityName, activityKind);
            if (activity is null)
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
