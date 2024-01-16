using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
#if !NET7_0_OR_GREATER
using System.Reflection;
#endif

namespace Diginsight.Diagnostics;

public static class DependencyInjectionExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IHostBuilder UseObservabilityServiceProvider(
        this IHostBuilder hostBuilder,
        Action<HostBuilderContext, ObservabilityServiceProviderOptions>? configureOptions = null
    )
    {
        return hostBuilder.UseServiceProviderFactory(
            context =>
            {
                ObservabilityServiceProviderOptions options = new ();
                configureOptions?.Invoke(context, options);
                return new ObservabilityServiceProviderFactory(options);
            }
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpenTelemetryBuilder AddObservability(this IServiceCollection services, Action<ObservabilityOptions>? configureObservability = null)
    {
        if (configureObservability is not null)
        {
            services.Configure(configureObservability);
        }

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<ObservabilityOptions>, ValidateObservabilityOptions>());

        return services
            .AddOpenTelemetry()
            .ConfigureResource(
                static resourceBuilder =>
                {
                    resourceBuilder.AddService(Assembly.GetEntryAssembly()!.FullName, serviceInstanceId: Environment.MachineName);
                }
            );
    }

    private sealed class ValidateObservabilityOptions : IValidateOptions<ObservabilityOptions>
    {
        public ValidateOptionsResult Validate(string? name, ObservabilityOptions options)
        {
            if (name != Options.DefaultName)
            {
                return ValidateOptionsResult.Skip;
            }

            IEnumerable<string> recordedActivityNames = options.RecordedActivityNames.Distinct().ToArray();
            options.RecordedActivityNames.Clear();
            options.RecordedActivityNames.AddRange(recordedActivityNames);

            IEnumerable<string> notRecordedActivityNames = options.NotRecordedActivityNames.Distinct().ToArray();
            options.NotRecordedActivityNames.Clear();
            options.NotRecordedActivityNames.AddRange(notRecordedActivityNames);

            return ValidateOptionsResult.Success;
        }
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
    public static ILoggingBuilder AddObservabilityConsole(
        this ILoggingBuilder loggingBuilder, Action<ObservabilityConsoleFormatterOptions>? configureFormatterOptions = null
    )
    {
        loggingBuilder.AddObservability();

        if (configureFormatterOptions is not null)
        {
            loggingBuilder.AddConsoleFormatter<ObservabilityConsoleFormatter, ObservabilityConsoleFormatterOptions>(configureFormatterOptions);
        }
        else
        {
            loggingBuilder.AddConsoleFormatter<ObservabilityConsoleFormatter, ObservabilityConsoleFormatterOptions>();
        }

        loggingBuilder.AddConsole(static consoleLoggerOptions => { consoleLoggerOptions.FormatterName = ObservabilityConsoleFormatter.FormatterName; });

        loggingBuilder.Services.TryAddSingleton<IConsoleLineDescriptorProvider, ConsoleLineDescriptorProvider>();

        return loggingBuilder;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MeterProviderBuilder AddObservability(this MeterProviderBuilder meterProviderBuilder)
    {
        return meterProviderBuilder;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TracerProviderBuilder AddObservability(this TracerProviderBuilder tracerProviderBuilder, LogLevel? defaultActivityLogLevel = null)
    {
        return tracerProviderBuilder
            .AddProcessor<ObservabilityLogProcessor>()
            .ConfigureServices(
                services =>
                {
                    services.AddLogStrings();

                    if (defaultActivityLogLevel is not null)
                    {
                        services.Configure<ObservabilityOptions>(o => { o.DefaultActivityLogLevel = defaultActivityLogLevel.Value; });
                    }
                }
            );
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
        _ = serviceProvider.GetService<MeterProvider>();
    }
}
