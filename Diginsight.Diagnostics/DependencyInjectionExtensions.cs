using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
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
    public static IHostBuilder UseDiginsightServiceProvider(
        this IHostBuilder hostBuilder,
        Action<HostBuilderContext, DiginsightServiceProviderOptions>? configureOptions = null
    )
    {
        return hostBuilder.UseServiceProviderFactory(
            context =>
            {
                DiginsightServiceProviderOptions options = new ();
                configureOptions?.Invoke(context, options);
                return new DiginsightServiceProviderFactory(options);
            }
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IOpenTelemetryBuilder AddDiginsightOpenTelemetry(this IServiceCollection services)
    {
        return services
            .AddOpenTelemetry()
            .ConfigureResource(
                static resourceBuilder =>
                {
                    resourceBuilder.AddService(
                        Assembly.GetEntryAssembly()!.GetName().Name ?? throw new UnreachableException("Entry assembly is not present or unnamed"),
                        serviceInstanceId: Environment.MachineName
                    );
                }
            );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ILoggingBuilder AddDiginsightCore(this ILoggingBuilder loggingBuilder)
    {
        return loggingBuilder.Configure(
            static loggerFactoryOptions => { loggerFactoryOptions.ActivityTrackingOptions = ActivityTrackingOptions.SpanId | ActivityTrackingOptions.TraceId | ActivityTrackingOptions.TraceFlags; }
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ILoggingBuilder AddDiginsightOpenTelemetry(this ILoggingBuilder loggingBuilder)
    {
        return loggingBuilder
            .AddDiginsightCore()
            .AddOpenTelemetry(
                static openTelemetryLoggerOptions =>
                {
                    openTelemetryLoggerOptions.IncludeFormattedMessage = true;
                    openTelemetryLoggerOptions.IncludeScopes = true;
                }
            );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ILoggingBuilder AddDiginsightConsole(
        this ILoggingBuilder loggingBuilder, Action<DiginsightConsoleFormatterOptions>? configureFormatterOptions = null
    )
    {
        loggingBuilder.AddDiginsightCore();

        if (configureFormatterOptions is not null)
        {
            loggingBuilder.AddConsoleFormatter<DiginsightConsoleFormatter, DiginsightConsoleFormatterOptions>(configureFormatterOptions);
        }
        else
        {
            loggingBuilder.AddConsoleFormatter<DiginsightConsoleFormatter, DiginsightConsoleFormatterOptions>();
        }

        loggingBuilder.AddConsole(static consoleLoggerOptions => { consoleLoggerOptions.FormatterName = DiginsightConsoleFormatter.FormatterName; });

        loggingBuilder.Services.TryAddSingleton<IConsoleLineDescriptorProvider, ConsoleLineDescriptorProvider>();

        return loggingBuilder;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MeterProviderBuilder AddDiginsight(this MeterProviderBuilder meterProviderBuilder)
    {
        return meterProviderBuilder;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TracerProviderBuilder AddDiginsight(this TracerProviderBuilder tracerProviderBuilder, LogLevel? defaultActivityLogLevel = null)
    {
        tracerProviderBuilder.AddProcessor<DiginsightLogProcessor>();

        tracerProviderBuilder.ConfigureServices(
            services =>
            {
                services.AddLogStrings();

                if (defaultActivityLogLevel is not null)
                {
                    services.Configure<DiginsightActivitiesOptions>(o => { o.ActivityLogLevel = defaultActivityLogLevel.Value; });
                }
            }
        );

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

    public static void EnsureDiginsight(this IServiceProvider serviceProvider)
    {
        _ = serviceProvider.GetService<TracerProvider>();
        _ = serviceProvider.GetService<MeterProvider>();
    }
}
