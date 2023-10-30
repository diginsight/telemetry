using Microsoft.Extensions.DependencyInjection;

namespace Diginsight;

internal sealed class ClassConfigurationGetterProvider : IClassConfigurationGetterProvider
{
    private readonly IServiceProvider serviceProvider;

    public ClassConfigurationGetterProvider(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public IClassConfigurationGetter GetFor(Type @class)
    {
        return (IClassConfigurationGetter)serviceProvider.GetRequiredService(typeof(IClassConfigurationGetter<>).MakeGenericType(@class));
    }
}
