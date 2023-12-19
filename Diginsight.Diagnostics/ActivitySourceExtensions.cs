using Microsoft.Extensions.Logging;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics;

public static class ActivitySourceExtensions
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Activity? CreateActivity(
        this ActivitySource activitySource,
        string activityName,
        ActivityKind activityKind = ActivityKind.Internal,
        LogLevel logLevel = LogLevel.Debug,
        [CallerMemberName] string callerMemberName = "",
        int stackDepth = 0
    )
    {
        return activitySource.CoreCreateActivity(null, null, activityName, activityKind, logLevel, callerMemberName, stackDepth + 1, false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Activity? CreateActivity(
        this ActivitySource activitySource,
        ILogger logger,
        string activityName,
        ActivityKind activityKind = ActivityKind.Internal,
        LogLevel logLevel = LogLevel.Debug,
        [CallerMemberName] string callerMemberName = "",
        int stackDepth = 0
    )
    {
        return activitySource.CoreCreateActivity(logger, null, activityName, activityKind, logLevel, callerMemberName, stackDepth + 1, false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Activity? StartActivity(
        this ActivitySource activitySource,
        string activityName,
        ActivityKind activityKind = ActivityKind.Internal,
        LogLevel logLevel = LogLevel.Debug,
        [CallerMemberName] string callerMemberName = "",
        int stackDepth = 0
    )
    {
        return activitySource.CoreCreateActivity(null, null, activityName, activityKind, logLevel, callerMemberName, stackDepth + 1, true);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Activity? StartActivity(
        this ActivitySource activitySource,
        ILogger logger,
        string activityName,
        ActivityKind activityKind = ActivityKind.Internal,
        LogLevel logLevel = LogLevel.Debug,
        [CallerMemberName] string callerMemberName = "",
        int stackDepth = 0
    )
    {
        return activitySource.CoreCreateActivity(logger, null, activityName, activityKind, logLevel, callerMemberName, stackDepth + 1, true);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Activity? CreateMethodActivity(
        this ActivitySource activitySource,
        object inputs,
        ActivityKind activityKind = ActivityKind.Internal,
        LogLevel logLevel = LogLevel.Debug,
        [CallerMemberName] string callerMemberName = "",
        int stackDepth = 0
    )
    {
        if (inputs is null)
        {
            throw new ArgumentNullException(nameof(inputs));
        }

        return activitySource.CreateMethodActivity(() => inputs, activityKind, logLevel, callerMemberName, stackDepth + 1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Activity? CreateMethodActivity(
        this ActivitySource activitySource,
        Func<object>? makeInputs = null,
        ActivityKind activityKind = ActivityKind.Internal,
        LogLevel logLevel = LogLevel.Debug,
        [CallerMemberName] string callerMemberName = "",
        int stackDepth = 0
    )
    {
        return activitySource.CoreCreateActivity(null, makeInputs, null, activityKind, logLevel, callerMemberName, stackDepth + 1, false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Activity? CreateMethodActivity(
        this ActivitySource activitySource,
        ILogger logger,
        object inputs,
        ActivityKind activityKind = ActivityKind.Internal,
        LogLevel logLevel = LogLevel.Debug,
        [CallerMemberName] string callerMemberName = "",
        int stackDepth = 0
    )
    {
        if (inputs is null)
        {
            throw new ArgumentNullException(nameof(inputs));
        }

        return activitySource.CreateMethodActivity(logger, () => inputs, activityKind, logLevel, callerMemberName, stackDepth + 1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Activity? CreateMethodActivity(
        this ActivitySource activitySource,
        ILogger logger,
        Func<object>? makeInputs = null,
        ActivityKind activityKind = ActivityKind.Internal,
        LogLevel logLevel = LogLevel.Debug,
        [CallerMemberName] string callerMemberName = "",
        int stackDepth = 0
    )
    {
        return activitySource.CoreCreateActivity(logger, makeInputs, null, activityKind, logLevel, callerMemberName, stackDepth + 1, false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Activity? StartMethodActivity(
        this ActivitySource activitySource,
        object inputs,
        ActivityKind activityKind = ActivityKind.Internal,
        LogLevel logLevel = LogLevel.Debug,
        [CallerMemberName] string callerMemberName = "",
        int stackDepth = 0
    )
    {
        if (inputs is null)
        {
            throw new ArgumentNullException(nameof(inputs));
        }

        return activitySource.StartMethodActivity(() => inputs, activityKind, logLevel, callerMemberName, stackDepth + 1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Activity? StartMethodActivity(
        this ActivitySource activitySource,
        Func<object>? makeInputs = null,
        ActivityKind activityKind = ActivityKind.Internal,
        LogLevel logLevel = LogLevel.Debug,
        [CallerMemberName] string callerMemberName = "",
        int stackDepth = 0
    )
    {
        return activitySource.CoreCreateActivity(null, makeInputs, null, activityKind, logLevel, callerMemberName, stackDepth + 1, true);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Activity? StartMethodActivity(
        this ActivitySource activitySource,
        ILogger logger,
        object inputs,
        ActivityKind activityKind = ActivityKind.Internal,
        LogLevel logLevel = LogLevel.Debug,
        [CallerMemberName] string callerMemberName = "",
        int stackDepth = 0
    )
    {
        if (inputs is null)
        {
            throw new ArgumentNullException(nameof(inputs));
        }

        return activitySource.StartMethodActivity(logger, () => inputs, activityKind, logLevel, callerMemberName, stackDepth + 1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Activity? StartMethodActivity(
        this ActivitySource activitySource,
        ILogger logger,
        Func<object>? makeInputs = null,
        ActivityKind activityKind = ActivityKind.Internal,
        LogLevel logLevel = LogLevel.Debug,
        [CallerMemberName] string callerMemberName = "",
        int stackDepth = 0
    )
    {
        return activitySource.CoreCreateActivity(logger, makeInputs, null, activityKind, logLevel, callerMemberName, stackDepth + 1, true);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Activity? CoreCreateActivity(
        this ActivitySource activitySource,
        ILogger? logger,
        Func<object>? makeInputs,
        string? customActivityName,
        ActivityKind activityKind,
        LogLevel logLevel,
        string callerMemberName,
        int stackDepth,
        bool start
    )
    {
        (Type callerType, string? localFunctionName) = RuntimeUtils.GetCaller(stackDepth);

        string finalActivityName;
        if (customActivityName is null)
        {
            string fullCallerMemberName = localFunctionName switch
            {
                "" => $"{callerMemberName}+<anon>",
                not null => $"{callerMemberName}+{localFunctionName}",
                null => callerMemberName,
            };
            finalActivityName = $"{callerType.Name}.{fullCallerMemberName}";
        }
        else
        {
            finalActivityName = customActivityName;
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
        activity.SetCustomProperty(ActivityCustomPropertyNames.IsStandalone, customActivityName is not null);

        if (start)
        {
            activity.Start();
        }

        return activity;
    }

    public static void StoreOutput(this ILogger logger, object? output)
    {
        if (Activity.Current is not { } activity)
        {
            return;
        }

        activity.SetCustomProperty(ActivityCustomPropertyNames.Logger, logger);
        activity.SetCustomProperty(ActivityCustomPropertyNames.Output, new StrongBox<object?>(output));
    }

    public static void StoreNamedOutputs(this ILogger logger, object namedOutputs)
    {
        if (namedOutputs is null)
        {
            throw new ArgumentNullException(nameof(namedOutputs));
        }
        if (Activity.Current is not { } activity)
        {
            return;
        }

        activity.SetCustomProperty(ActivityCustomPropertyNames.Logger, logger);
        activity.SetCustomProperty(ActivityCustomPropertyNames.NamedOutputs, namedOutputs);
    }
}
