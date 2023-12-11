using Diginsight.Strings;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Diginsight;

public static class LogStringExtensions
{
    private static readonly Histogram<double> LogStringDuration = AutoObservabilityUtils.Meter.CreateHistogram<double>("diginsight.log_string_duration", "ms");

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
            StringBuilder stringBuilder = new ();
            appendingContextFactory.MakeAppendingContext(stringBuilder).ComposeAndAppend(obj, false, true, configureVariables, configureMetaProperties);
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

    internal static IEnumerable<Type> GetClosure(this Type type)
    {
        Type? currentType = type;
        while (currentType is not null)
        {
            yield return currentType;
            currentType = currentType.BaseType;
        }

        foreach (Type @interface in type.GetInterfaces())
        {
            yield return @interface;
        }
    }
}
