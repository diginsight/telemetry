using log4net.Appender;
using log4net.Config;
using log4net.Layout;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics.Log4Net;

public static class DependencyInjectionExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ILoggingBuilder AddObservabilityLog4Net(
        this ILoggingBuilder loggingBuilder, IEnumerable<Func<AppenderSkeleton>> appenderFactories
    )
    {
        loggingBuilder.AddObservability();

        IServiceCollection services = loggingBuilder.Services;
        if (!services.Any(static x => x.ServiceType == typeof(AddObservabilityLog4NetCalled)))
        {
            services
                .AddSingleton<AddObservabilityLog4NetCalled>()
                .AddSingleton<ILoggerProvider>(
                    sp =>
                    {
                        IOptionsMonitor<ObservabilityConsoleFormatterOptions> formatterOptionsMonitor =
                            sp.GetRequiredService<IOptionsMonitor<ObservabilityConsoleFormatterOptions>>();
                        ILayout layout = new ObservabilityLayoutSkeleton(formatterOptionsMonitor);

                        BasicConfigurator.Configure(
                            appenderFactories
                                .Select(
                                    appenderFactory =>
                                    {
                                        AppenderSkeleton appender = appenderFactory();
                                        appender.Layout = layout;
                                        return appender;
                                    }
                                )
                                .ToArray<IAppender>()
                        );

                        Log4NetProviderOptions providerOptions = new ()
                        {
                            LoggingEventFactory = new ObservabilityLoggingEventFactory(),
                            UseWebOrAppConfig = false,
                            ExternalConfigurationSetup = true,
                        };
                        return new Log4NetProvider(providerOptions);
                    }
                );
        }

        return loggingBuilder;
    }

    // ReSharper disable once ConvertToStaticClass
    private sealed class AddObservabilityLog4NetCalled
    {
        private AddObservabilityLog4NetCalled() => throw new NotSupportedException();
    }
}
