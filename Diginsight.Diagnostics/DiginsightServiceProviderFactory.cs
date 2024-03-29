﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Diginsight.Diagnostics;

internal sealed class DiginsightServiceProviderFactory : IServiceProviderFactory<IServiceCollection>
{
    private readonly DiginsightServiceProviderOptions options;
    private readonly IServiceProviderFactory<IServiceCollection> decoratee;

    public DiginsightServiceProviderFactory(DiginsightServiceProviderOptions options)
    {
        this.options = options;
        decoratee = new DefaultServiceProviderFactory(options);
    }

    public IServiceCollection CreateBuilder(IServiceCollection services) => decoratee.CreateBuilder(services);

    public IServiceProvider CreateServiceProvider(IServiceCollection services)
    {
        IServiceProvider serviceProvider = decoratee.CreateServiceProvider(services);

        serviceProvider.EnsureDiginsight();

        if (options.DeferredLoggerFactory is { } deferredLoggerFactory && serviceProvider.GetService<ILoggerFactory>() is { } targetLoggerFactory)
        {
            deferredLoggerFactory.FlushTo(targetLoggerFactory);
        }

        return serviceProvider;
    }
}
