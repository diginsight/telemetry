using Diginsight.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class DependencyInjectionExtensions
{
    public static IServiceCollection FlushOnCreateServiceProvider(this IServiceCollection services, DeferredLoggerFactory deferredLoggerFactory)
    {
        services.AddSingleton<IOnCreateServiceProvider>(sp => ActivatorUtilities.CreateInstance<DeferredLoggerFactoryFlusher>(sp, deferredLoggerFactory));
        return services;
    }

    private sealed class DeferredLoggerFactoryFlusher : IOnCreateServiceProvider
    {
        private readonly DeferredLoggerFactory deferredLoggerFactory;
        private readonly ILoggerFactory? loggerFactory;

        public DeferredLoggerFactoryFlusher(DeferredLoggerFactory deferredLoggerFactory, ILoggerFactory? loggerFactory = null)
        {
            this.deferredLoggerFactory = deferredLoggerFactory;
            this.loggerFactory = loggerFactory;
        }

        public void Run()
        {
            if (loggerFactory is not null)
            {
                deferredLoggerFactory.FlushTo(loggerFactory);
            }
        }
    }

    public static IServiceCollection FlushOnCreateServiceProvider(this IServiceCollection services, DeferredActivityLifecycleLogEmitter deferredEmitter)
    {
        services.AddSingleton<IOnCreateServiceProvider>(sp => ActivatorUtilities.CreateInstance<DeferredActivityLifecycleLogEmitterFlusher>(sp, deferredEmitter));
        return services;
    }

    private sealed class DeferredActivityLifecycleLogEmitterFlusher : IOnCreateServiceProvider
    {
        private readonly DeferredActivityLifecycleLogEmitter deferredEmitter;
        private readonly ActivityLifecycleLogEmitter? emitter;

        public DeferredActivityLifecycleLogEmitterFlusher(
            DeferredActivityLifecycleLogEmitter deferredEmitter, ActivityLifecycleLogEmitter? emitter = null
        )
        {
            this.deferredEmitter = deferredEmitter;
            this.emitter = emitter;
        }

        public void Run()
        {
            if (emitter is not null)
            {
                deferredEmitter.FlushTo(emitter);
            }
        }
    }

    public static IServiceCollection AddActivityListenersAdder(this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IOnCreateServiceProvider, ActivityListenersAdder>());
        return services;
    }

    private sealed class ActivityListenersAdder : IOnCreateServiceProvider
    {
        private readonly IEnumerable<IActivityListenerRegistration> registrations;

        public ActivityListenersAdder(IEnumerable<IActivityListenerRegistration> registrations)
        {
            this.registrations = registrations;
        }

        public void Run()
        {
            ActivitySource.AddActivityListener(ActivityUtils.DepthSetterActivityListener);

            foreach (IActivityListenerRegistration registration in registrations)
            {
                ActivitySource.AddActivityListener(registration.ToActivityListener());
            }
        }
    }

    public static ILoggingBuilder AddDiginsightCore(this ILoggingBuilder loggingBuilder)
    {
        IServiceCollection services = loggingBuilder.Services;

        services
            .AddClassAwareOptions()
            .AddActivityListenersAdder();

        services.TryAddSingleton<ActivityLifecycleLogEmitter>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IActivityListenerRegistration, ActivityLifecycleLogEmitterRegistration>());

        loggingBuilder.Configure(
            static loggerFactoryOptions =>
            {
                loggerFactoryOptions.ActivityTrackingOptions =
                    ActivityTrackingOptions.SpanId | ActivityTrackingOptions.TraceId | ActivityTrackingOptions.TraceFlags;
            }
        );

        return loggingBuilder;
    }

    private sealed class ActivityLifecycleLogEmitterRegistration : IActivityListenerRegistration
    {
        private readonly IDiginsightActivitiesOptions activitiesOptions;

        public IActivityListenerLogic Logic { get; }

        public ActivityLifecycleLogEmitterRegistration(
            ActivityLifecycleLogEmitter emitter,
            IOptions<DiginsightActivitiesOptions> activitiesOptions
        )
        {
            Logic = emitter;
            this.activitiesOptions = activitiesOptions.Value.Freeze();
        }

        public bool ShouldListenTo(ActivitySource activitySource)
        {
            string activitySourceName = activitySource.Name;
            return activitiesOptions.ActivitySources
                .Where(x => ActivityUtils.NameMatchesPattern(activitySourceName, x.Key))
                .All(static x => x.Value);
        }
    }

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

    public static ILoggingBuilder AddDiginsightDebug(
        this ILoggingBuilder loggingBuilder, Action<DiginsightDebugLoggerOptions>? configureOptions = null
    )
    {
        loggingBuilder.AddDiginsightCore();

        loggingBuilder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, DiginsightDebugLoggerProvider>());

        if (configureOptions is not null)
        {
            loggingBuilder.Services.Configure(configureOptions);
        }

        return loggingBuilder;
    }

    public static IServiceCollection AddSpanDurationMetricRecorder<TRegistration>(this IServiceCollection services)
        where TRegistration : SpanDurationMetricRecorderRegistration
    {
        services
            .AddClassAwareOptions()
            .AddActivityListenersAdder()
            .AddMetrics();

        services.TryAddSingleton<SpanDurationMetricRecorder>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IActivityListenerRegistration, TRegistration>());

        return services;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IServiceCollection AddSpanDurationMetricRecorder(this IServiceCollection services)
    {
        return services.AddSpanDurationMetricRecorder<SpanDurationMetricRecorderRegistration>();
    }

    public static ILoggingBuilder AddVolatileConfiguration(this ILoggingBuilder loggingBuilder)
    {
        IServiceCollection services = loggingBuilder.Services;

        if (services.Any(static sd => sd.ImplementationType == typeof(VolatileLogLevelOptionsChangeTokenSource)))
        {
            return loggingBuilder;
        }

        services.AddSingleton<IOptionsChangeTokenSource<LoggerFilterOptions>, VolatileLogLevelOptionsChangeTokenSource>();

        Assembly assembly = typeof(ILoggerProviderConfigurationFactory).Assembly;

        services.AddSingleton(
            sp => (IConfigureOptions<LoggerFilterOptions>)Activator.CreateInstance(
                assembly.GetType("Microsoft.Extensions.Logging.LoggerFilterConfigureOptions")!,
                sp.GetRequiredService<IVolatileConfigurationStorageProvider>().Get(KnownVolatileConfigurationStorageNames.LogLevel).Configuration
            )!
        );

        Type loggingConfigurationType = assembly.GetType("Microsoft.Extensions.Logging.Configuration.LoggingConfiguration")!;
        services.AddSingleton(
            loggingConfigurationType,
            sp => Activator.CreateInstance(
                loggingConfigurationType,
                sp.GetRequiredService<IVolatileConfigurationStorageProvider>().Get(KnownVolatileConfigurationStorageNames.LogLevel).Configuration
            )!
        );

        return loggingBuilder;
    }

    private sealed class VolatileLogLevelOptionsChangeTokenSource : ConfigurationChangeTokenSource<LoggerFilterOptions>
    {
        public VolatileLogLevelOptionsChangeTokenSource(IVolatileConfigurationStorageProvider storageProvider)
            : base(storageProvider.Get(KnownVolatileConfigurationStorageNames.LogLevel).Configuration) { }
    }
}
