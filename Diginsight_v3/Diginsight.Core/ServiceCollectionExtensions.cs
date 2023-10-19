using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Diginsight;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddClassConfigurationGetter(this IServiceCollection services)
    {
        services.TryAddSingleton(typeof(IClassConfigurationGetter<>), typeof(ClassConfigurationGetter<>));
        services.TryAddSingleton<IClassConfigurationGetterProvider, ClassConfigurationGetterProvider>();
        return services;
    }
}
