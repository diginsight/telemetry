using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.ComponentModel;

namespace Diginsight.Diagnostics;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class DependencyInjectionExtensions
{
    public static IServiceCollection FlushOnCreateServiceProvider(this IServiceCollection services, IDeferredLoggerFactory deferredLoggerFactory)
    {
        services.AddSingleton<IOnCreateServiceProvider>(sp => ActivatorUtilities.CreateInstance<FlushDeferredLoggerFactory>(sp, deferredLoggerFactory));

        return services;
    }

    private sealed class FlushDeferredLoggerFactory : IOnCreateServiceProvider
    {
        private readonly IDeferredLoggerFactory deferredLoggerFactory;
        private readonly ILoggerFactory? loggerFactory;

        public FlushDeferredLoggerFactory(IDeferredLoggerFactory deferredLoggerFactory, ILoggerFactory? loggerFactory = null)
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

    public static ILoggingBuilder AddDiginsightCore(this ILoggingBuilder loggingBuilder)
    {
        loggingBuilder.Services.AddLogStrings();
        loggingBuilder.Services.TryAddSingleton<ActivityLifecycleLogEmitter>();
        loggingBuilder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IOnCreateServiceProvider, RegisterLifecycleActivityListener>());

        loggingBuilder.Configure(
            static loggerFactoryOptions => { loggerFactoryOptions.ActivityTrackingOptions = ActivityTrackingOptions.SpanId | ActivityTrackingOptions.TraceId | ActivityTrackingOptions.TraceFlags; }
        );

        return loggingBuilder;
    }

    private sealed class RegisterLifecycleActivityListener : IOnCreateServiceProvider
    {
        private readonly ActivityLifecycleLogEmitter emitter;
        private readonly IDiginsightActivitiesOptions activitiesOptions;

        public RegisterLifecycleActivityListener(
            ActivityLifecycleLogEmitter emitter,
            IOptions<DiginsightActivitiesOptions> activitiesOptions
        )
        {
            this.emitter = emitter;
            this.activitiesOptions = activitiesOptions.Value.Freeze();
        }

        public void Run()
        {
            ActivityUtils.AddActivityListeners(
                emitter,
                activitySource =>
                {
                    string name = activitySource.Name;
                    return activitiesOptions.ActivitySources.Any(x => ActivityUtils.NameMatchesPattern(name, x));
                }
            );
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
