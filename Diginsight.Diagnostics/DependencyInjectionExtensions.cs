using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System.Runtime.CompilerServices;
#if !NET7_0_OR_GREATER
using System.Reflection;
#endif

namespace Diginsight.Diagnostics;

public static class DependencyInjectionExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpenTelemetryBuilder AddObservability(this IServiceCollection services, IConfiguration rootForClassConfigurationGetter)
    {
        return services
            .AddClassConfigurationGetter(rootForClassConfigurationGetter)
            .AddOpenTelemetry();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ILoggingBuilder AddObservability(this ILoggingBuilder loggingBuilder)
    {
        return loggingBuilder
            .Configure(
                static loggerFactoryOptions => { loggerFactoryOptions.ActivityTrackingOptions = ActivityTrackingOptions.SpanId | ActivityTrackingOptions.TraceId | ActivityTrackingOptions.TraceFlags; }
            )
            .AddOpenTelemetry(
                static openTelemetryLoggerOptions =>
                {
                    openTelemetryLoggerOptions.IncludeFormattedMessage = true;
                    openTelemetryLoggerOptions.IncludeScopes = true;
                }
            );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ILoggingBuilder AddObservabilityConsole(this ILoggingBuilder loggingBuilder)
    {
        return loggingBuilder
            .AddObservability()
            .AddConsoleFormatter<ObservabilityConsoleFormatter, ObservabilityConsoleFormatterOptions>()
            .AddConsole(
                static consoleLoggerOptions => { consoleLoggerOptions.FormatterName = ObservabilityConsoleFormatter.FormatterName; }
            );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MeterProviderBuilder AddObservability(this MeterProviderBuilder meterProviderBuilder)
    {
        return meterProviderBuilder;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TracerProviderBuilder AddObservability(this TracerProviderBuilder tracerProviderBuilder)
    {
        return tracerProviderBuilder
            .AddProcessor<ObservabilityLogProcessor>()
            .ConfigureServices(static services => services.AddLogStringComposer());
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

    public static void EnsureObservability(this IServiceProvider serviceProvider)
    {
        _ = serviceProvider.GetService<TracerProvider>();
    }
}
