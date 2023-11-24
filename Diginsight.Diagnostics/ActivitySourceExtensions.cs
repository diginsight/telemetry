using Microsoft.Extensions.Logging;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics;

public static class ActivitySourceExtensions
{
    private static readonly IDictionary<MethodBase, (Type, string?)> CallerCache = new Dictionary<MethodBase, (Type, string?)>();

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
        return activitySource.CoreCreateActivity(null, null, activityName, activityKind, logLevel, callerMemberName, stackDepth, false);
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
        return activitySource.CoreCreateActivity(logger, null, activityName, activityKind, logLevel, callerMemberName, stackDepth, false);
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
        return activitySource.CoreCreateActivity(null, null, activityName, activityKind, logLevel, callerMemberName, stackDepth, true);
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
        return activitySource.CoreCreateActivity(logger, null, activityName, activityKind, logLevel, callerMemberName, stackDepth, true);
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
        return activitySource.CoreCreateActivity(null, makeInputs, null, activityKind, logLevel, callerMemberName, stackDepth, false);
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
        return activitySource.CoreCreateActivity(logger, makeInputs, null, activityKind, logLevel, callerMemberName, stackDepth, false);
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
        return activitySource.CoreCreateActivity(null, makeInputs, null, activityKind, logLevel, callerMemberName, stackDepth, true);
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
        return activitySource.CoreCreateActivity(logger, makeInputs, null, activityKind, logLevel, callerMemberName, stackDepth, true);
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
        (Type callerType, string? localFunctionName) = GetCaller(stackDepth + 1);

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

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (Type DeclaringType, string? LocalFunctionName) GetCaller(int stackDepth)
    {
        if (stackDepth < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stackDepth), "Negative stack depth");
        }

        MethodBase method = new StackFrame(stackDepth + 2, false).GetMethod()!;
        // ReSharper disable once InconsistentlySynchronizedField
        if (CallerCache.TryGetValue(method, out var caller))
        {
            return caller;
        }

        lock (((ICollection)CallerCache).SyncRoot)
        {
            if (CallerCache.TryGetValue(method, out caller))
            {
                return caller;
            }

            Type innerDeclaringType = method.DeclaringType!;
            string methodName = method.Name;
            bool isGenerated = innerDeclaringType.FullName!.Contains('<') || methodName.Contains('<');

            string? localFunctionName;
            if (!isGenerated)
            {
                localFunctionName = null;
            }
            else
            {
                string innerDeclaringTypeName = innerDeclaringType.Name;
                ReadOnlySpan<char> span =
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                    methodName == "MoveNext" ? innerDeclaringTypeName[1..^2] : methodName;
#else
                    (methodName == "MoveNext" ? innerDeclaringTypeName.Substring(1, innerDeclaringTypeName.Length - 2) : methodName).AsSpan();
#endif
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                span = span[(span.IndexOf('>') + 1)..];
#else
                span = span.Slice(span.IndexOf('>') + 1);
#endif
                localFunctionName = span[0] switch
                {
                    'b' => "",
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                    'g' => new string(span[3..span.IndexOf('|')]),
#else
                    'g' => new string(span.Slice(3, span.IndexOf('|') - 3).ToArray()),
#endif
                    _ => null,
                };
            }

            Type declaringType = innerDeclaringType;
            if (isGenerated && !method.IsDefined(typeof(CompilerGeneratedAttribute)))
            {
                while (!declaringType.IsDefined(typeof(CompilerGeneratedAttribute)))
                {
                    declaringType = declaringType.DeclaringType!;
                }
                declaringType = declaringType.DeclaringType!;
            }

            return CallerCache[method] = (declaringType, localFunctionName);
        }
    }
}
