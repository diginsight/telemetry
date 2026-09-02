using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics;

/// <summary>
/// Provides extension methods for registering Diginsight OpenTelemetry services.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Adds Diginsight OpenTelemetry services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The OpenTelemetry builder.</returns>
    /// <exception cref="UnreachableException">Thrown when the entry assembly is not present or unnamed.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IOpenTelemetryBuilder AddDiginsightOpenTelemetry(this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IOnCreateServiceProvider, EnsureOpenTelemetry>());

        IOpenTelemetryBuilder openTelemetryBuilder = services.AddOpenTelemetry();

        openTelemetryBuilder
            .ConfigureResource(
                static resourceBuilder =>
                {
                    resourceBuilder.AddService(
                        Assembly.GetEntryAssembly()!.GetName().Name ?? throw new UnreachableException("Entry assembly is not present or unnamed"),
                        serviceInstanceId: Environment.MachineName
                    );
                }
            );

        return openTelemetryBuilder;
    }

    private sealed class EnsureOpenTelemetry : IOnCreateServiceProvider
    {
        public EnsureOpenTelemetry(
            TracerProvider? tracerProvider = null,
            MeterProvider? meterProvider = null
        )
        {
            _ = tracerProvider;
            _ = meterProvider;
        }

        public void Run() { }
    }

    /// <summary>
    /// Adds Diginsight OpenTelemetry logging to the logging builder.
    /// </summary>
    /// <param name="loggingBuilder">The logging builder.</param>
    /// <param name="configure">The action used to configure the OpenTelemetry logger options.</param>
    /// <returns>The logging builder.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ILoggingBuilder AddDiginsightOpenTelemetry(this ILoggingBuilder loggingBuilder, Action<OpenTelemetryLoggerOptions>? configure = null)
    {
        return loggingBuilder
            .AddDiginsightCore()
            .AddOpenTelemetry(
                openTelemetryLoggerOptions =>
                {
                    openTelemetryLoggerOptions.IncludeFormattedMessage = true;
                    openTelemetryLoggerOptions.IncludeScopes = true;
                    configure?.Invoke(openTelemetryLoggerOptions);
                }
            );
    }

    /// <summary>
    /// Adds Diginsight metric configuration to the meter provider builder.
    /// </summary>
    /// <param name="meterProviderBuilder">The meter provider builder.</param>
    /// <returns>The meter provider builder.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MeterProviderBuilder AddDiginsight(this MeterProviderBuilder meterProviderBuilder)
    {
        return meterProviderBuilder;
    }

    /// <summary>
    /// Adds Diginsight trace configuration to the tracer provider builder.
    /// </summary>
    /// <param name="tracerProviderBuilder">The tracer provider builder.</param>
    /// <returns>The tracer provider builder.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TracerProviderBuilder AddDiginsight(this TracerProviderBuilder tracerProviderBuilder)
    {
        return tracerProviderBuilder;
    }

    /// <summary>
    /// Adds metric views to the meter provider builder.
    /// </summary>
    /// <param name="builder">The meter provider builder.</param>
    /// <param name="views">The metric views to add.</param>
    /// <returns>The meter provider builder.</returns>
    public static MeterProviderBuilder AddViews(
        this MeterProviderBuilder builder,
        params (string InstrumentName, MetricStreamConfiguration MetricStreamConfiguration)[] views
    )
    {
        foreach (var (instrumentName, metricStreamConfiguration) in views)
        {
            builder.AddView(instrumentName, metricStreamConfiguration);
        }

        return builder;
    }

    /// <summary>
    /// Adds the meter and views declared by a custom metrics type.
    /// </summary>
    /// <typeparam name="T">The custom metrics type.</typeparam>
    /// <param name="builder">The meter provider builder.</param>
    /// <returns>The meter provider builder.</returns>
    public static MeterProviderBuilder AddMetrics<T>(this MeterProviderBuilder builder)
        where T :
#if NET
        ICustomMetrics<T>
#else
        CustomMetrics
#endif
    {
#if NET
        builder.AddMeter(T.ObservabilityName);
        builder.AddViews(T.Views);
#else
        T customMetrics = (T)typeof(T).GetField("Instance", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
        builder.AddMeter(customMetrics.ObservabilityName);
        builder.AddViews(customMetrics.Views);
#endif
        return builder;
    }
}
