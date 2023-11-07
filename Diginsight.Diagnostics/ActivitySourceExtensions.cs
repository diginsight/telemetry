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
    public static Activity? StartMethodActivity(
        this ActivitySource source,
        ILogger logger,
        object inputs,
        [CallerMemberName] string callerMemberName = "",
        LogLevel logLevel = LogLevel.Debug,
        ActivityKind activityKind = ActivityKind.Internal,
        int stackDepth = 0
    )
    {
        (Type callerType, string? localFunctionName) = GetCaller(stackDepth);
        return source.StartMethodActivity(logger, () => inputs, callerType, callerMemberName, localFunctionName, logLevel, activityKind);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Activity? StartMethodActivity(
        this ActivitySource source,
        ILogger logger,
        Func<object?>? makeInputs = null,
        [CallerMemberName] string callerMemberName = "",
        LogLevel logLevel = LogLevel.Debug,
        ActivityKind activityKind = ActivityKind.Internal,
        int stackDepth = 0
    )
    {
        (Type callerType, string? localFunctionName) = GetCaller(stackDepth);
        return source.StartMethodActivity(logger, makeInputs, callerType, callerMemberName, localFunctionName, logLevel, activityKind);
    }

    private static Activity? StartMethodActivity(
        this ActivitySource source,
        ILogger logger,
        Func<object?>? makeInputs,
        Type callerType,
        string callerMemberName,
        string? localFunctionName,
        LogLevel logLevel,
        ActivityKind activityKind
    )
    {
        string fullCallerMemberName = localFunctionName switch
        {
            "" => $"{callerMemberName}+<anon>",
            not null => $"{callerMemberName}+{localFunctionName}",
            null => callerMemberName,
        };

        Activity? activity = source.CreateActivity($"{callerType.Name}.{fullCallerMemberName}", activityKind);
        if (activity is null)
        {
            return null;
        }

        activity.SetCustomProperty(ActivityCustomPropertyNames.Logger, logger);
        activity.SetCustomProperty(ActivityCustomPropertyNames.LogLevel, logLevel);
        activity.SetCustomProperty(ActivityCustomPropertyNames.Inputs, makeInputs);
        activity.SetCustomProperty(ActivityCustomPropertyNames.CallerType, callerType);

        activity.Start();

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
