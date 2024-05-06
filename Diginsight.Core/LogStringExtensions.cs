using Diginsight.Strings;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Diginsight;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class LogStringExtensions
{
    private static readonly Histogram<double> LogStringDuration = SelfObservabilityUtils.Meter.CreateHistogram<double>("diginsight.log_string_duration", "ms");

    private static readonly IEnumerable<Type> FixedForbiddenTypes =
    [
        typeof(Thread),
        typeof(CancellationToken),
        typeof(CancellationTokenSource),
        typeof(MarshalByRefObject),
#if NET
        typeof(TaskCompletionSource),
#endif
        typeof(TaskCompletionSource<>),
    ];

    private static readonly IMemoryCache ForbiddenTypesCache = new MemoryCache(
        Options.Create(new MemoryCacheOptions() { SizeLimit = 2000 })
    );

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ILogStringable ToLogStringable(
        this object? obj,
        IAppendingContextFactory? appendingContextFactory = null
    )
    {
        return (appendingContextFactory ?? AppendingContextFactoryBuilder.DefaultFactory)
            .ToLogStringable(obj);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToLogString(
        this object? obj,
        IAppendingContextFactory? appendingContextFactory = null,
        Action<LogStringVariableConfiguration>? configureVariables = null,
        Action<IDictionary<string, object?>>? configureMetaProperties = null
    )
    {
        return (appendingContextFactory ?? AppendingContextFactoryBuilder.DefaultFactory)
            .MakeLogString(obj, configureVariables, configureMetaProperties);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string MakeLogString(
        this IAppendingContextFactory appendingContextFactory,
        object? obj,
        Action<LogStringVariableConfiguration>? configureVariables = null,
        Action<IDictionary<string, object?>>? configureMetaProperties = null
    )
    {
        bool success = true;
        Stopwatch stopwatch = Stopwatch.StartNew();

        try
        {
            appendingContextFactory.MakeAppendingContext(out StringBuilder stringBuilder)
                .ComposeAndAppend(
                    obj,
                    configureVariables: configureVariables,
                    configureMetaProperties: configureMetaProperties
                );
            return stringBuilder.ToString();
        }
        catch (Exception)
        {
            success = false;
            throw;
        }
        finally
        {
            LogStringDuration.Record(stopwatch.Elapsed.TotalMilliseconds, new KeyValuePair<string, object?>("success", success));
        }
    }

    internal static bool IsForbidden(this Type type)
    {
        static bool IsAwaitable(Type type)
        {
            return type.GetMethod("GetAwaiter", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, [ ]) is { IsGenericMethod: false } method
                && IsAwaiter(method.ReturnType);
        }

        static bool IsAwaiter(Type type)
        {
            return typeof(INotifyCompletion).IsAssignableFrom(type)
                && type.GetProperty("IsCompleted", BindingFlags.Public | BindingFlags.Instance, null, typeof(bool), Type.EmptyTypes, [ ]) is not null
                && type.GetMethod("GetResult", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, [ ]) is { IsGenericMethod: false };
        }

        static bool IsEnumerator(Type type)
        {
            return (typeof(IEnumerator).IsAssignableFrom(type) || typeof(IEnumerator<>).IsGenericAssignableFrom(type))
                && !(typeof(IEnumerable).IsAssignableFrom(type) || typeof(IEnumerable<>).IsGenericAssignableFrom(type));
        }

        static bool IsAsyncStateMachine(Type type)
        {
            return (typeof(IAsyncStateMachine).IsAssignableFrom(type) || typeof(IAsyncEnumerator<>).IsGenericAssignableFrom(type))
                && !typeof(IAsyncEnumerable<>).IsGenericAssignableFrom(type);
        }

        return ForbiddenTypesCache.GetOrCreate(
            type,
            e =>
            {
                e.SlidingExpiration = TimeSpan.FromMinutes(30);
                e.Size = 1;

                return FixedForbiddenTypes.Any(x => x.IsGenericAssignableFrom(type))
                    || IsAwaitable(type)
                    || IsAwaiter(type)
                    || IsEnumerator(type)
                    || IsAsyncStateMachine(type);
            }
        );
    }
}
