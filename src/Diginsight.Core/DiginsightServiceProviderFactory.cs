using Microsoft.Extensions.DependencyInjection;

namespace Diginsight;

/// <summary>
/// A <see cref="IServiceProviderFactory{IServiceCollection}" /> extended with additional functionality provided by the Diginsight library.
/// </summary>
public sealed class DiginsightServiceProviderFactory : IServiceProviderFactory<IServiceCollection>
{
    private readonly IServiceProviderFactory<IServiceCollection> decoratee;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiginsightServiceProviderFactory" /> class.
    /// </summary>
    /// <param name="options">The options to configure the service provider.</param>
    public DiginsightServiceProviderFactory(ServiceProviderOptions options)
    {
        decoratee = new DefaultServiceProviderFactory(options);
    }

    /// <inheritdoc />
    public IServiceCollection CreateBuilder(IServiceCollection services) => decoratee.CreateBuilder(services);

    /// <inheritdoc />
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
