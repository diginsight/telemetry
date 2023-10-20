using Diginsight.Strings;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Diginsight;

public static class LogStringExtensions
{
    private static readonly IEnumerable<Type> FixedForbiddenTypes = new[]
    {
        typeof(Thread),
        typeof(CancellationToken),
        typeof(CancellationTokenSource),
        typeof(IAsyncStateMachine),
        typeof(MarshalByRefObject),
#if NET6_0_OR_GREATER
        typeof(TaskCompletionSource),
#endif
        typeof(TaskCompletionSource<>),
        typeof(IEnumerator<>),
        typeof(IAsyncEnumerator<>),
    };

    private static readonly IMemoryCache ForbiddenTypesCache = new MemoryCache(
        Options.Create(new MemoryCacheOptions() { SizeLimit = 2000 })
    );

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToLogString(
        this object? obj,
        ILogStringComposer? logStringComposer = null,
        Action<LogStringThresholdConfiguration>? configureThresholds = null,
        Action<IDictionary<string, object?>>? configureMetaProperties = null
    )
    {
        return (logStringComposer ?? LogStringComposers.Default)
            .MakeLogString(obj, configureThresholds, configureMetaProperties);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string MakeLogString(
        this ILogStringComposer logStringComposer,
        object? obj,
        Action<LogStringThresholdConfiguration>? configureThresholds = null,
        Action<IDictionary<string, object?>>? configureMetaProperties = null
    )
    {
        StringBuilder stringBuilder = new ();
        logStringComposer.Append(obj, stringBuilder, configureThresholds, configureMetaProperties);
        return stringBuilder.ToString();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StringBuilder Append(this StringBuilder stringBuilder, ILoggable loggable, LoggingContext loggingContext)
    {
        loggable.AppendTo(stringBuilder, loggingContext);
        return stringBuilder;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static StringBuilder AppendLogString(
        this StringBuilder stringBuilder,
        object? obj,
        LoggingContext loggingContext,
        bool incrementDepth = true,
        Action<LogStringThresholdConfiguration>? configureThresholds = null,
        Action<IDictionary<string, object?>>? configureMetaProperties = null
    )
    {
        loggingContext.Append(obj, stringBuilder, incrementDepth, configureThresholds, configureMetaProperties);
        return stringBuilder;
    }

    public static IDisposable? IncrementDepth(this LoggingContext loggingContext, bool condition, out bool isMaxDepth)
    {
        if (condition)
        {
            return loggingContext.IncrementDepth(out isMaxDepth);
        }
        else
        {
            isMaxDepth = false;
            return null;
        }
    }

    internal static bool IsForbidden(this Type type)
    {
        static bool IsAwaitable(Type type)
        {
            return type.GetMethod("GetAwaiter", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, Array.Empty<ParameterModifier>()) is { IsGenericMethod: false } method
                && IsAwaiter(method.ReturnType);
        }

        static bool IsAwaiter(Type type)
        {
            return typeof(INotifyCompletion).IsAssignableFrom(type)
                && type.GetProperty("IsCompleted", BindingFlags.Public | BindingFlags.Instance, null, typeof(bool), Type.EmptyTypes, Array.Empty<ParameterModifier>()) is not null
                && type.GetMethod("GetResult", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, Array.Empty<ParameterModifier>()) is { IsGenericMethod: false };
        }

        return ForbiddenTypesCache.GetOrCreate(
            type,
            e =>
            {
                e.SlidingExpiration = TimeSpan.FromMinutes(30);
                e.Size = 1;

                return FixedForbiddenTypes.Any(x => x.IsGenericAssignableFrom(type))
                    || IsAwaitable(type)
                    || IsAwaiter(type);
            }
        );
    }
}
