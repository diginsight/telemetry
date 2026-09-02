using log4net.Appender;
using log4net.Core;
using log4net.Repository.Hierarchy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Globalization;

namespace Diginsight.Diagnostics.Log4Net;

/// <summary>
/// Provides extension methods for registering Diginsight Log4Net services.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class DependencyInjectionExtensions
{
    /// <param name="loggingBuilder">The logging builder.</param>
    extension(ILoggingBuilder loggingBuilder)
    {
        /// <summary>
        /// Adds Diginsight Log4Net logging with appenders created from the service provider.
        /// </summary>
        /// <param name="createAppenders">The function used to create Log4Net appenders.</param>
        /// <param name="getLevel">The function used to get the root logger level.</param>
        /// <param name="repositoryName">The Log4Net repository name.</param>
        /// <returns>The logging builder.</returns>
        public ILoggingBuilder AddDiginsightLog4Net(
            Func<IServiceProvider, IEnumerable<IAppender>> createAppenders,
            Func<IServiceProvider, Level?>? getLevel = null,
            string? repositoryName = null
        )
        {
            loggingBuilder.AddDiginsightCore();

            IServiceCollection services = loggingBuilder.Services;

            repositoryName ??= Guid.NewGuid().ToString("N");

            ServiceDescriptor descriptor = ServiceDescriptor.Singleton<ILoggerProvider, Log4NetProvider>(
                sp =>
                {
                    string fullRepositoryName = string.Format(CultureInfo.InvariantCulture, "{0}_{1:X8}", repositoryName, sp.GetHashCode());
                    IRepositorySelector repositorySelector = LoggerManager.RepositorySelector;

                    Hierarchy hierarchy = repositorySelector.ExistsRepository(fullRepositoryName)
                        ? (Hierarchy)repositorySelector.GetRepository(fullRepositoryName)
                        : (Hierarchy)repositorySelector.CreateRepository(fullRepositoryName, typeof(Hierarchy));

                    if (!hierarchy.Configured)
                    {
                        Logger logger = hierarchy.Root;

                        foreach (IAppender appender in createAppenders(sp))
                        {
                            logger.AddAppender(appender.AsActivatedOptionHandler());
                        }

                        if (getLevel?.Invoke(sp) is { } logLevel)
                        {
                            logger.Level = logLevel;
                        }

                        hierarchy.Configured = true;
                    }

                    Log4NetProviderOptions providerOptions = new ()
                    {
                        LoggingEventFactory = ActivatorUtilities.CreateInstance<DiginsightLoggingEventFactory>(sp),
                        ExternalConfigurationSetup = true,
                        LoggerRepository = hierarchy.Name,
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

        /// <summary>
        /// Adds Diginsight Log4Net logging with configuration loaded from a file.
        /// </summary>
        /// <param name="configFileName">The Log4Net configuration file name.</param>
        /// <returns>The logging builder.</returns>
        public ILoggingBuilder AddDiginsightLog4Net(string configFileName)
        {
            loggingBuilder.AddDiginsightCore();

            IServiceCollection services = loggingBuilder.Services;

            ServiceDescriptor descriptor = ServiceDescriptor.Singleton<ILoggerProvider, Log4NetProvider>(
                sp =>
                {
                    Log4NetProviderOptions providerOptions = new (configFileName)
                    {
                        LoggingEventFactory = ActivatorUtilities.CreateInstance<DiginsightLoggingEventFactory>(sp),
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
