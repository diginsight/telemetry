using Diginsight.Strings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Diginsight;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddClassConfigurationGetter(this IServiceCollection services, IConfiguration? rootConfiguration = null)
    {
        services.TryAddSingleton(typeof(IClassConfigurationGetter<>), typeof(ClassConfigurationGetter<>));
        services.TryAddSingleton<IClassConfigurationGetterProvider, ClassConfigurationGetterProvider>();
        if (rootConfiguration is not null)
        {
            services.TryAddSingleton(new ClassConfigurationGetter.ConfigurationWrapper(rootConfiguration));
        }

        return services;
    }

    public static IServiceCollection AddLogStrings(this IServiceCollection services)
    {
        if (services.Any(static x => x.ServiceType == typeof(IAppendingContextFactory)))
            return services;

        return services
            .AddOptions()
            .AddSingleton<IAppendingContextFactory, AppendingContextFactory>()
            .AddSingleton<IMemberInfoLogStringProvider, MemberInfoLogStringProvider>()
            .AddSingleton<IReflectionLogStringHelper, ReflectionLogStringHelper>();
    }
}
