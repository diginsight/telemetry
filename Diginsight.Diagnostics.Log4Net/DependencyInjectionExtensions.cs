using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Diagnostics;

namespace Diginsight.Diagnostics.Log4Net;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class DependencyInjectionExtensions
{
    public static ILoggingBuilder AddDiginsightLog4Net(
        this ILoggingBuilder loggingBuilder,
        string configFileName,
        Func<ActivitySource, bool>? shouldListenToActivitySource = null
    )
    {
        loggingBuilder.AddDiginsightCore(shouldListenToActivitySource);

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

    private sealed class AddDiginsightLog4NetMarker
    {
        public ServiceDescriptor Descriptor { get; set; }

        public AddDiginsightLog4NetMarker(ServiceDescriptor descriptor)
        {
            Descriptor = descriptor;
        }
    }
}
