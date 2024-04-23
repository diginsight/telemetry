using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.ComponentModel;
using System.Diagnostics;

namespace Diginsight.Diagnostics;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class DependencyInjectionExtensions
{
    public static IServiceCollection FlushOnCreateServiceProvider(this IServiceCollection services, IDeferredLoggerFactory deferredLoggerFactory)
    {
        services.AddSingleton<IOnCreateServiceProvider>(sp => ActivatorUtilities.CreateInstance<DeferredLoggerFactoryFlusher>(sp, deferredLoggerFactory));
        return services;
    }

    private sealed class DeferredLoggerFactoryFlusher : IOnCreateServiceProvider
    {
        private readonly IDeferredLoggerFactory deferredLoggerFactory;
        private readonly ILoggerFactory? loggerFactory;

        public DeferredLoggerFactoryFlusher(IDeferredLoggerFactory deferredLoggerFactory, ILoggerFactory? loggerFactory = null)
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
            .AddLogStrings()
            .AddActivityListenersAdder();
        services.TryAddSingleton<ActivityLifecycleLogEmitter>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IActivityListenerRegistration, ActivityLifecycleLogEmitterRegistration>());

        loggingBuilder.Configure(
            static loggerFactoryOptions =>
            {
                loggerFactoryOptions.ActivityTrackingOptions = ActivityTrackingOptions.SpanId | ActivityTrackingOptions.TraceId | ActivityTrackingOptions.TraceFlags;
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
            string name = activitySource.Name;
            return activitiesOptions.ActivitySources.Any(x => ActivityUtils.NameMatchesPattern(name, x));
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
}
