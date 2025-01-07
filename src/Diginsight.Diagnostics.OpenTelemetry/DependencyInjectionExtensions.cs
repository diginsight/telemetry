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

[EditorBrowsable(EditorBrowsableState.Never)]
public static class DependencyInjectionExtensions
{
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MeterProviderBuilder AddDiginsight(this MeterProviderBuilder meterProviderBuilder)
    {
        return meterProviderBuilder;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TracerProviderBuilder AddDiginsight(this TracerProviderBuilder tracerProviderBuilder)
    {
        return tracerProviderBuilder;
    }

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

    public static MeterProviderBuilder AddMetrics<T>(this MeterProviderBuilder builder)
#if NET7_0_OR_GREATER
        where T : ICustomMetrics<T>
#else
        where T : CustomMetrics
#endif
    {
#if NET7_0_OR_GREATER
        builder.AddMeter(T.ObservabilityName);
        builder.AddViews(T.Views);
#else
        T customMetrics = (T)typeof(T).GetField("Instance", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
        builder.AddMeter(customMetrics.ObservabilityName);
        builder.AddViews(customMetrics.Views);
#endif
        return builder;
    }

    public static void EnsureDiginsightOpenTelemetry(this IServiceProvider serviceProvider)
    {
        _ = serviceProvider.GetService<TracerProvider>();
        _ = serviceProvider.GetService<MeterProvider>();
    }
}
