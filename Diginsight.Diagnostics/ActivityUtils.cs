using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics;

public static class ActivityUtils
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Activity? CreateRichActivity(
        this ActivitySource activitySource,
        string activityName,
        ActivityKind activityKind = ActivityKind.Internal,
        LogLevel logLevel = LogLevel.Debug,
        [CallerMemberName] string callerMemberName = "",
        int stackDepth = 0
    )
    {
        return CoreCreateRichActivity(activitySource, null, null, activityName, activityKind, logLevel, callerMemberName, stackDepth + 1, false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Activity? CreateRichActivity(
        this ActivitySource activitySource,
        ILogger logger,
        string activityName,
        ActivityKind activityKind = ActivityKind.Internal,
        LogLevel logLevel = LogLevel.Debug,
        [CallerMemberName] string callerMemberName = "",
        int stackDepth = 0
    )
    {
        return CoreCreateRichActivity(activitySource, logger, null, activityName, activityKind, logLevel, callerMemberName, stackDepth + 1, false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Activity? StartRichActivity(
        this ActivitySource activitySource,
        string activityName,
        ActivityKind activityKind = ActivityKind.Internal,
        LogLevel logLevel = LogLevel.Debug,
        [CallerMemberName] string callerMemberName = "",
        int stackDepth = 0
    )
    {
        return CoreCreateRichActivity(activitySource, null, null, activityName, activityKind, logLevel, callerMemberName, stackDepth + 1, true);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Activity? StartRichActivity(
        this ActivitySource activitySource,
        ILogger logger,
        string activityName,
        ActivityKind activityKind = ActivityKind.Internal,
        LogLevel logLevel = LogLevel.Debug,
        [CallerMemberName] string callerMemberName = "",
        int stackDepth = 0
    )
    {
        return CoreCreateRichActivity(activitySource, logger, null, activityName, activityKind, logLevel, callerMemberName, stackDepth + 1, true);
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

        return CreateMethodActivity(activitySource, () => inputs, activityKind, logLevel, callerMemberName, stackDepth + 1);
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
        return CoreCreateRichActivity(activitySource, null, makeInputs, null, activityKind, logLevel, callerMemberName, stackDepth + 1, false);
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

        return CreateMethodActivity(activitySource, logger, () => inputs, activityKind, logLevel, callerMemberName, stackDepth + 1);
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
        return CoreCreateRichActivity(activitySource, logger, makeInputs, null, activityKind, logLevel, callerMemberName, stackDepth + 1, false);
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

        return StartMethodActivity(activitySource, () => inputs, activityKind, logLevel, callerMemberName, stackDepth + 1);
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
        return CoreCreateRichActivity(activitySource, null, makeInputs, null, activityKind, logLevel, callerMemberName, stackDepth + 1, true);
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

        return StartMethodActivity(activitySource, logger, () => inputs, activityKind, logLevel, callerMemberName, stackDepth + 1);
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
        return CoreCreateRichActivity(activitySource, logger, makeInputs, null, activityKind, logLevel, callerMemberName, stackDepth + 1, true);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Activity? CoreCreateRichActivity(
        ActivitySource activitySource,
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
