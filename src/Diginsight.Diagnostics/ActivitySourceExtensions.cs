using Diginsight.Runtime;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class ActivitySourceExtensions
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Activity? CreateRichActivity(
        this ActivitySource activitySource,
        string activityName,
        object inputs,
        ActivityKind activityKind = ActivityKind.Internal,
        LogLevel logLevel = LogLevel.Debug
    )
    {
        if (inputs is null)
        {
            throw new ArgumentNullException(nameof(inputs));
        }

        return CoreCreateRichActivity(activitySource, null, () => inputs, activityName, true, activityKind, logLevel, false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Activity? CreateRichActivity(
        this ActivitySource activitySource,
        string activityName,
        Func<object>? makeInputs = null,
        ActivityKind activityKind = ActivityKind.Internal,
        LogLevel logLevel = LogLevel.Debug
    )
    {
        return CoreCreateRichActivity(activitySource, null, makeInputs, activityName, true, activityKind, logLevel, false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Activity? CreateRichActivity(
        this ActivitySource activitySource,
        ILogger logger,
        string activityName,
        object inputs,
        ActivityKind activityKind = ActivityKind.Internal,
        LogLevel logLevel = LogLevel.Debug
    )
    {
        if (inputs is null)
        {
            throw new ArgumentNullException(nameof(inputs));
        }

        return CoreCreateRichActivity(activitySource, logger, () => inputs, activityName, true, activityKind, logLevel, false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Activity? CreateRichActivity(
        this ActivitySource activitySource,
        ILogger logger,
        string activityName,
        Func<object>? makeInputs = null,
        ActivityKind activityKind = ActivityKind.Internal,
        LogLevel logLevel = LogLevel.Debug
    )
    {
        return CoreCreateRichActivity(activitySource, logger, makeInputs, activityName, true, activityKind, logLevel, false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Activity? StartRichActivity(
        this ActivitySource activitySource,
        string activityName,
        object inputs,
        ActivityKind activityKind = ActivityKind.Internal,
        LogLevel logLevel = LogLevel.Debug
    )
    {
        if (inputs is null)
        {
            throw new ArgumentNullException(nameof(inputs));
        }

        return CoreCreateRichActivity(activitySource, null, () => inputs, activityName, true, activityKind, logLevel, true);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Activity? StartRichActivity(
        this ActivitySource activitySource,
        string activityName,
        Func<object>? makeInputs = null,
        ActivityKind activityKind = ActivityKind.Internal,
        LogLevel logLevel = LogLevel.Debug
    )
    {
        return CoreCreateRichActivity(activitySource, null, makeInputs, activityName, true, activityKind, logLevel, true);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Activity? StartRichActivity(
        this ActivitySource activitySource,
        ILogger logger,
        string activityName,
        object inputs,
        ActivityKind activityKind = ActivityKind.Internal,
        LogLevel logLevel = LogLevel.Debug
    )
    {
        if (inputs is null)
        {
            throw new ArgumentNullException(nameof(inputs));
        }

        return CoreCreateRichActivity(activitySource, logger, () => inputs, activityName, true, activityKind, logLevel, true);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Activity? StartRichActivity(
        this ActivitySource activitySource,
        ILogger logger,
        string activityName,
        Func<object>? makeInputs = null,
        ActivityKind activityKind = ActivityKind.Internal,
        LogLevel logLevel = LogLevel.Debug
    )
    {
        return CoreCreateRichActivity(activitySource, logger, makeInputs, activityName, true, activityKind, logLevel, true);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Activity? CreateMethodActivity(
        this ActivitySource activitySource,
        object inputs,
        ActivityKind activityKind = ActivityKind.Internal,
        LogLevel logLevel = LogLevel.Debug,
        [CallerMemberName] string callerMemberName = ""
    )
    {
        if (inputs is null)
        {
            throw new ArgumentNullException(nameof(inputs));
        }

        return CoreCreateRichActivity(activitySource, null, () => inputs, callerMemberName, false, activityKind, logLevel, false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Activity? CreateMethodActivity(
        this ActivitySource activitySource,
        Func<object>? makeInputs = null,
        ActivityKind activityKind = ActivityKind.Internal,
        LogLevel logLevel = LogLevel.Debug,
        [CallerMemberName] string callerMemberName = ""
    )
    {
        return CoreCreateRichActivity(activitySource, null, makeInputs, callerMemberName, false, activityKind, logLevel, false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Activity? CreateMethodActivity(
        this ActivitySource activitySource,
        ILogger logger,
        object inputs,
        ActivityKind activityKind = ActivityKind.Internal,
        LogLevel logLevel = LogLevel.Debug,
        [CallerMemberName] string callerMemberName = ""
    )
    {
        if (inputs is null)
        {
            throw new ArgumentNullException(nameof(inputs));
        }

        return CoreCreateRichActivity(activitySource, logger, () => inputs, callerMemberName, false, activityKind, logLevel, false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Activity? CreateMethodActivity(
        this ActivitySource activitySource,
        ILogger logger,
        Func<object>? makeInputs = null,
        ActivityKind activityKind = ActivityKind.Internal,
        LogLevel logLevel = LogLevel.Debug,
        [CallerMemberName] string callerMemberName = ""
    )
    {
        return CoreCreateRichActivity(activitySource, logger, makeInputs, callerMemberName, false, activityKind, logLevel, false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Activity? StartMethodActivity(
        this ActivitySource activitySource,
        object inputs,
        ActivityKind activityKind = ActivityKind.Internal,
        LogLevel logLevel = LogLevel.Debug,
        [CallerMemberName] string callerMemberName = ""
    )
    {
        if (inputs is null)
        {
            throw new ArgumentNullException(nameof(inputs));
        }

        return CoreCreateRichActivity(activitySource, null, () => inputs, callerMemberName, false, activityKind, logLevel, true);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Activity? StartMethodActivity(
        this ActivitySource activitySource,
        Func<object>? makeInputs = null,
        ActivityKind activityKind = ActivityKind.Internal,
        LogLevel logLevel = LogLevel.Debug,
        [CallerMemberName] string callerMemberName = ""
    )
    {
        return CoreCreateRichActivity(activitySource, null, makeInputs, callerMemberName, false, activityKind, logLevel, true);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Activity? StartMethodActivity(
        this ActivitySource activitySource,
        ILogger logger,
        object inputs,
        ActivityKind activityKind = ActivityKind.Internal,
        LogLevel logLevel = LogLevel.Debug,
        [CallerMemberName] string callerMemberName = ""
    )
    {
        if (inputs is null)
        {
            throw new ArgumentNullException(nameof(inputs));
        }

        return CoreCreateRichActivity(activitySource, logger, () => inputs, callerMemberName, false, activityKind, logLevel, true);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Activity? StartMethodActivity(
        this ActivitySource activitySource,
        ILogger logger,
        Func<object>? makeInputs = null,
        ActivityKind activityKind = ActivityKind.Internal,
        LogLevel logLevel = LogLevel.Debug,
        [CallerMemberName] string callerMemberName = ""
    )
    {
        return CoreCreateRichActivity(activitySource, logger, makeInputs, callerMemberName, false, activityKind, logLevel, true);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Activity? CoreCreateRichActivity(
        ActivitySource activitySource,
        ILogger? logger,
        Func<object>? makeInputs,
        string activityNameHint,
        bool isStandalone,
        ActivityKind activityKind,
        LogLevel logLevel,
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
