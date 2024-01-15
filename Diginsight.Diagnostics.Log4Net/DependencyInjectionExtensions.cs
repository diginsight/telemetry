using log4net.Appender;
using log4net.Config;
using log4net.Layout;
using log4net.Repository;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics.Log4Net;

public static class DependencyInjectionExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ILoggingBuilder AddObservabilityLog4Net(
        this ILoggingBuilder loggingBuilder,
        Func<AppenderSkeleton> appenderFactory,
        ILoggerRepository? loggerRepository = null,
        Action<ObservabilityLayoutSkeletonOptions>? configureLayoutSkeletonOptions = null
    )
    {
        return loggingBuilder.AddObservabilityLog4Net([ appenderFactory ], loggerRepository, configureLayoutSkeletonOptions);
    }

    public static ILoggingBuilder AddObservabilityLog4Net(
        this ILoggingBuilder loggingBuilder,
        IEnumerable<Func<AppenderSkeleton>> appenderFactories,
        ILoggerRepository? loggerRepository = null,
        Action<ObservabilityLayoutSkeletonOptions>? configureLayoutSkeletonOptions = null
    )
    {
        loggingBuilder.AddObservability();

        IServiceCollection services = loggingBuilder.Services;

        services.TryAddSingleton<ILog4NetLineDescriptorProvider, Log4NetLineDescriptorProvider>();

        if (configureLayoutSkeletonOptions is not null)
        {
            services.Configure(configureLayoutSkeletonOptions);
        }

        ServiceDescriptor descriptor = ServiceDescriptor.Singleton<ILoggerProvider>(
            sp =>
            {
                ILayout layout = ActivatorUtilities.CreateInstance<ObservabilityLayoutSkeleton>(sp);

                IAppender[] appenders = appenderFactories
                    .Select(
                        appenderFactory =>
                        {
                            AppenderSkeleton appender = appenderFactory();
                            appender.Layout = layout;
                            return appender;
                        }
                    )
                    .ToArray<IAppender>();

                if (loggerRepository is not null)
                {
                    BasicConfigurator.Configure(loggerRepository, appenders);
                }
                else
                {
                    BasicConfigurator.Configure(appenders);
                }

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
