using Microsoft.Extensions.Caching.Memory;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using MseOptions = Microsoft.Extensions.Options.Options;

namespace Diginsight.Stringify;

/// <summary>
/// Provides extension methods for converting objects to string representations.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class StringifyExtensions
{
    private static readonly Histogram<double> StringifyDuration = SelfObservability.Meter.CreateHistogram<double>("diginsight.stringify_duration", "ms");

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
        MseOptions.Create(new MemoryCacheOptions() { SizeLimit = 2000 })
    );

    /// <param name="obj">The object instance.</param>
    extension(object? obj)
    {
        /// <summary>
        /// Converts the specified object to a stringifiable representation.
        /// </summary>
        /// <param name="stringifyContextFactory">The stringify context factory.</param>
        /// <returns>The stringifiable representation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IStringifiable ToStringifiable(
            IStringifyContextFactory? stringifyContextFactory = null
        )
        {
            return (stringifyContextFactory ?? StringifyContextFactoryBuilder.DefaultFactory)
                .ToStringifiable(obj);
        }

        /// <summary>
        /// Stringifies the specified object.
        /// </summary>
        /// <param name="stringifyContextFactory">The stringify context factory.</param>
        /// <param name="configureVariables">The action used to configure variable options.</param>
        /// <param name="configureMetaProperties">The action used to configure meta properties.</param>
        /// <returns>The compact string representation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string Stringify(
            IStringifyContextFactory? stringifyContextFactory = null,
            Action<StringifyVariableConfiguration>? configureVariables = null,
            Action<IDictionary<string, object?>>? configureMetaProperties = null
        )
        {
            return (stringifyContextFactory ?? StringifyContextFactoryBuilder.DefaultFactory)
                .Stringify(obj, configureVariables, configureMetaProperties);
        }
    }

    /// <summary>
    /// Stringifies the specified object.
    /// </summary>
    /// <param name="stringifyContextFactory">The stringify context factory.</param>
    /// <param name="obj">The object to stringify.</param>
    /// <param name="configureVariables">The action used to configure variable options.</param>
    /// <param name="configureMetaProperties">The action used to configure meta properties.</param>
    /// <returns>The compact string representation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Stringify(
        this IStringifyContextFactory stringifyContextFactory,
        object? obj,
        Action<StringifyVariableConfiguration>? configureVariables = null,
        Action<IDictionary<string, object?>>? configureMetaProperties = null
    )
    {
        bool success = true;
        Stopwatch stopwatch = Stopwatch.StartNew();

        try
        {
            stringifyContextFactory.MakeStringifyContext(out StringBuilder stringBuilder)
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
            StringifyDuration.Record(stopwatch.Elapsed.TotalMilliseconds, new KeyValuePair<string, object?>("success", success));
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
