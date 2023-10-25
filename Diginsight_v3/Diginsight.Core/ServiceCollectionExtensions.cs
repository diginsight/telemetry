using Diginsight.Strings;
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

    public static IServiceCollection AddLogStringComposer(this IServiceCollection services)
    {
        if (services.Any(static x => x.ServiceType == typeof(ILogStringComposer)))
            return services;

        return services
            .AddOptions()
            .AddSingleton<ILogStringComposer, LogStringComposer>()
            .AddSingleton<IMemberLogStringProvider, MemberLogStringProvider>()
            .AddSingleton<IReflectionLogStringHelper, ReflectionLogStringHelper>();
    }
}
