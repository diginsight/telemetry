using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Diagnostics;

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

    public static ILoggingBuilder AddDiginsightCore(this ILoggingBuilder loggingBuilder, Func<ActivitySource, bool>? shouldListenToActivitySource = null)
    {
        loggingBuilder.Services.AddLogStrings();
        loggingBuilder.Services.TryAddSingleton<ActivityLifecycleLogEmitter>();

        loggingBuilder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IOnCreateServiceProvider, RegisterLifecycleActivityListener>(
                sp => ActivatorUtilities.CreateInstance<RegisterLifecycleActivityListener>(sp, shouldListenToActivitySource ?? (static _ => true))
            )
        );

        loggingBuilder.Configure(
            static loggerFactoryOptions => { loggerFactoryOptions.ActivityTrackingOptions = ActivityTrackingOptions.SpanId | ActivityTrackingOptions.TraceId | ActivityTrackingOptions.TraceFlags; }
        );

        return loggingBuilder;
    }

    private sealed class RegisterLifecycleActivityListener : IOnCreateServiceProvider
    {
        private readonly ActivityLifecycleLogEmitter emitter;
        private readonly Func<ActivitySource, bool> shouldListenTo;

        public RegisterLifecycleActivityListener(ActivityLifecycleLogEmitter emitter, Func<ActivitySource, bool> shouldListenTo)
        {
            this.emitter = emitter;
            this.shouldListenTo = shouldListenTo;
        }

        public void Run()
        {
            ActivitySource.AddActivityListener(
                new ActivityListener()
                {
                    ActivityStarted = emitter.OnStart,
                    ActivityStopped = emitter.OnEnd,
                    Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                    ShouldListenTo = shouldListenTo,
                }
            );
        }
    }

    public static ILoggingBuilder AddDiginsightConsole(
        this ILoggingBuilder loggingBuilder,
        Action<DiginsightConsoleFormatterOptions>? configureFormatterOptions = null,
        Func<ActivitySource, bool>? shouldListenToActivitySource = null
    )
    {
        loggingBuilder.AddDiginsightCore(shouldListenToActivitySource);

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
