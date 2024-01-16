using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Diginsight.Diagnostics;

internal sealed class ObservabilityServiceProviderFactory : IServiceProviderFactory<IServiceCollection>
{
    private readonly ObservabilityServiceProviderOptions options;

    public ObservabilityServiceProviderFactory(ObservabilityServiceProviderOptions options)
    {
        this.options = options;
    }

    public IServiceCollection CreateBuilder(IServiceCollection services) => services;

    public IServiceProvider CreateServiceProvider(IServiceCollection services)
    {
        IServiceProvider serviceProvider = services.BuildServiceProvider(options);

        serviceProvider.EnsureObservability();

        if (options.DeferredLoggerFactory is { } deferredLoggerFactory && serviceProvider.GetService<ILoggerFactory>() is { } targetLoggerFactory)
        {
            deferredLoggerFactory.FlushTo(targetLoggerFactory);
        }

        return serviceProvider;
    }
}
