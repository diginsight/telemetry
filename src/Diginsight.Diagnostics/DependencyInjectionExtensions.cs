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

/// <summary>
/// Provides extension methods for registering Diginsight diagnostics services.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class DependencyInjectionExtensions
{
    /// <param name="services">The service collection to configure.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers a deferred logger factory to be flushed when the service provider is created.
        /// </summary>
        /// <param name="deferredLoggerFactory">The deferred logger factory to flush.</param>
        /// <returns>The configured service collection.</returns>
        public IServiceCollection FlushOnCreateServiceProvider(DeferredLoggerFactory deferredLoggerFactory)
        {
            services.AddSingleton<IOnCreateServiceProvider>(sp => ActivatorUtilities.CreateInstance<DeferredLoggerFactoryFlusher>(sp, deferredLoggerFactory));
            return services;
        }

        /// <summary>
        /// Registers a deferred activity lifecycle log emitter to be flushed when the service provider is created.
        /// </summary>
        /// <param name="deferredEmitter">The deferred activity lifecycle log emitter to flush.</param>
        /// <returns>The configured service collection.</returns>
        public IServiceCollection FlushOnCreateServiceProvider(DeferredActivityLifecycleLogEmitter deferredEmitter)
        {
            services.AddSingleton<IOnCreateServiceProvider>(sp => ActivatorUtilities.CreateInstance<DeferredActivityLifecycleLogEmitterFlusher>(sp, deferredEmitter));
            return services;
        }

        /// <summary>
        /// Adds the service that registers configured activity listeners when the service provider is created.
        /// </summary>
        /// <returns>The configured service collection.</returns>
        public IServiceCollection AddActivityListenersAdder()
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IOnCreateServiceProvider, ActivityListenersAdder>());
            return services;
        }

        /// <summary>
        /// Adds span duration metric recording with the specified activity listener registration type.
        /// </summary>
        /// <typeparam name="TRegistration">The activity listener registration type.</typeparam>
        /// <returns>The configured service collection.</returns>
        public IServiceCollection AddSpanDurationMetricRecorder<TRegistration>()
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

        /// <summary>
        /// Adds span duration metric recording with the default activity listener registration.
        /// </summary>
        /// <returns>The configured service collection.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IServiceCollection AddSpanDurationMetricRecorder()
        {
            return services.AddSpanDurationMetricRecorder<SpanDurationMetricRecorderRegistration>();
        }
    }

    /// <param name="loggingBuilder">The logging builder to configure.</param>
    extension(ILoggingBuilder loggingBuilder)
    {
        /// <summary>
        /// Adds core Diginsight activity lifecycle logging services.
        /// </summary>
        /// <returns>The configured logging builder.</returns>
        public ILoggingBuilder AddDiginsightCore()
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

        /// <summary>
        /// Adds Diginsight console logging with the Diginsight console formatter.
        /// </summary>
        /// <param name="configureFormatterOptions">The action used to configure formatter options.</param>
        /// <returns>The configured logging builder.</returns>
        public ILoggingBuilder AddDiginsightConsole(
            Action<DiginsightConsoleFormatterOptions>? configureFormatterOptions = null
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

        /// <summary>
        /// Adds Diginsight debug logging.
        /// </summary>
        /// <param name="configureOptions">The action used to configure debug logger options.</param>
        /// <returns>The configured logging builder.</returns>
        public ILoggingBuilder AddDiginsightDebug(
            Action<DiginsightDebugLoggerOptions>? configureOptions = null
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

        /// <summary>
        /// Adds volatile configuration support for logger filter options.
        /// </summary>
        /// <returns>The configured logging builder.</returns>
        public ILoggingBuilder AddVolatileConfiguration()
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

    private sealed class ActivityListenersAdder : IOnCreateServiceProvider
    {
        private readonly IReadOnlyCollection<IActivityListenerRegistration> registrations;

        public ActivityListenersAdder(IEnumerable<IActivityListenerRegistration> registrations)
        {
            this.registrations = [ ..registrations ];
        }

        public void Run()
        {
            ActivitySource.AddActivityListener(
                ActivityUtils.CreateDepthSetterActivityListener(activitySource => registrations.Any(x => x.ShouldListenTo(activitySource)))
            );

            foreach (IActivityListenerRegistration registration in registrations)
            {
                ActivitySource.AddActivityListener(registration.ToActivityListener());
            }
        }
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
            IEnumerable<bool> matches = activitiesOptions.ActivitySources
                .Where(x => ActivityUtils.NameMatchesPattern(activitySourceName, x.Key))
                .Select(static x => x.Value);

            bool result = false;
            foreach (bool match in matches)
            {
                if (!match)
                    return false;
                result = true;
            }
            return result;
        }
    }

    private sealed class VolatileLogLevelOptionsChangeTokenSource : ConfigurationChangeTokenSource<LoggerFilterOptions>
    {
        public VolatileLogLevelOptionsChangeTokenSource(IVolatileConfigurationStorageProvider storageProvider)
            : base(storageProvider.Get(KnownVolatileConfigurationStorageNames.LogLevel).Configuration) { }
    }
}
