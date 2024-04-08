using Microsoft.Extensions.DependencyInjection;

namespace Diginsight;

public sealed class DiginsightServiceProviderFactory : IServiceProviderFactory<IServiceCollection>
{
    private readonly IServiceProviderFactory<IServiceCollection> decoratee;

    public DiginsightServiceProviderFactory(ServiceProviderOptions options)
    {
        decoratee = new DefaultServiceProviderFactory(options);
    }

    public IServiceCollection CreateBuilder(IServiceCollection services) => decoratee.CreateBuilder(services);

    public IServiceProvider CreateServiceProvider(IServiceCollection services)
    {
        IServiceProvider serviceProvider = decoratee.CreateServiceProvider(services);

        foreach (IOnCreateServiceProvider onCreateServiceProvider in serviceProvider.GetServices<IOnCreateServiceProvider>())
        {
            onCreateServiceProvider.Run();
        }

        return serviceProvider;
    }
}
