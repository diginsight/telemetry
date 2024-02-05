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
    public static ILoggingBuilder AddDiginsightLog4Net(
        this ILoggingBuilder loggingBuilder,
        Func<AppenderSkeleton> appenderFactory,
        ILoggerRepository? loggerRepository = null,
        Action<DiginsightLayoutSkeletonOptions>? configureLayoutSkeletonOptions = null
    )
    {
        return loggingBuilder.AddDiginsightLog4Net([ appenderFactory ], loggerRepository, configureLayoutSkeletonOptions);
    }

    public static ILoggingBuilder AddDiginsightLog4Net(
        this ILoggingBuilder loggingBuilder,
        IEnumerable<Func<AppenderSkeleton>> appenderFactories,
        ILoggerRepository? loggerRepository = null,
        Action<DiginsightLayoutSkeletonOptions>? configureLayoutSkeletonOptions = null
    )
    {
        loggingBuilder.AddDiginsight();

        IServiceCollection services = loggingBuilder.Services;

        services.TryAddSingleton<ILog4NetLineDescriptorProvider, Log4NetLineDescriptorProvider>();

        if (configureLayoutSkeletonOptions is not null)
        {
            services.Configure(configureLayoutSkeletonOptions);
        }

        ServiceDescriptor descriptor = ServiceDescriptor.Singleton<ILoggerProvider>(
            sp =>
            {
                ILayout layout = ActivatorUtilities.CreateInstance<DiginsightLayoutSkeleton>(sp);

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
                    LoggingEventFactory = ActivatorUtilities.CreateInstance<DiginsightLoggingEventFactory>(sp),
                    UseWebOrAppConfig = false,
                    ExternalConfigurationSetup = true,
                };
                return new Log4NetProvider(providerOptions);
            }
        );

        AddDiginsightLog4NetMarker marker;
        if (services.FirstOrDefault(static x => x.ServiceType == typeof(AddDiginsightLog4NetMarker)) is { } markerDescriptor)
        {
            marker = (AddDiginsightLog4NetMarker)markerDescriptor.ImplementationInstance!;
            services.Remove(marker.Descriptor);
            marker.Descriptor = descriptor;
        }
        else
        {
            marker = new AddDiginsightLog4NetMarker(descriptor);
            services.AddSingleton(marker);
        }

        services.Add(descriptor);

        return loggingBuilder;
    }

    private sealed class AddDiginsightLog4NetMarker
    {
        public ServiceDescriptor Descriptor { get; set; }

        public AddDiginsightLog4NetMarker(ServiceDescriptor descriptor)
        {
            Descriptor = descriptor;
        }
    }
}
