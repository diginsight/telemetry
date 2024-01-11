using Diginsight.Diagnostics.TextWriting;
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

        ServiceDescriptor descriptor = ServiceDescriptor.Singleton<ILoggerProvider>(
            sp =>
            {
                IOptionsMonitor<ObservabilityTextWriterOptions> writerOptionsMonitor =
                    sp.GetRequiredService<IOptionsMonitor<ObservabilityTextWriterOptions>>();
                ILayout layout = new ObservabilityLayoutSkeleton(writerOptionsMonitor);

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
                    LoggingEventFactory = ObservabilityLoggingEventFactory.Instance,
                    UseWebOrAppConfig = false,
                    ExternalConfigurationSetup = true,
                };
                return new Log4NetProvider(providerOptions);
            }
        );

        AddObservabilityLog4NetMarker marker;
        if (services.FirstOrDefault(static x => x.ServiceType == typeof(AddObservabilityLog4NetMarker)) is { } markerDescriptor)
        {
            marker = (AddObservabilityLog4NetMarker)markerDescriptor.ImplementationInstance!;
            services.Remove(marker.Descriptor);
            marker.Descriptor = descriptor;
        }
        else
        {
            marker = new AddObservabilityLog4NetMarker(descriptor);
            services.AddSingleton(marker);
        }

        services.Add(descriptor);

        return loggingBuilder;
    }

    private sealed class AddObservabilityLog4NetMarker
    {
        public ServiceDescriptor Descriptor { get; set; }

        public AddObservabilityLog4NetMarker(ServiceDescriptor descriptor)
        {
            Descriptor = descriptor;
        }
    }
}
