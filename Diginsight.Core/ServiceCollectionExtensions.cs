using Diginsight.CAOptions;
using Diginsight.Strings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

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

    public static IServiceCollection AddClassAwareOptions(this IServiceCollection services)
    {
        services.TryAddSingleton(typeof(IClassAwareOptions<,>), typeof(UnnamedClassAwareOptionsManager<,>));
        services.TryAddSingleton(typeof(IClassAwareOptionsProvider<>), typeof(ClassAwareOptionsProvider<>));

        services.TryAddScoped(typeof(IClassAwareOptionsSnapshot<,>), typeof(ClassAwareOptionsManager<,>));
        services.TryAddScoped(typeof(IClassAwareOptionsSnapshotProvider<>), typeof(ClassAwareOptionsSnapshotProvider<>));

        services.TryAddSingleton(typeof(IClassAwareOptionsMonitor<,>), typeof(ClassAwareOptionsMonitor<,>));
        services.TryAddSingleton(typeof(IClassAwareOptionsMonitorProvider<>), typeof(ClassAwareOptionsMonitorProvider<>));

        services.TryAddTransient(typeof(IClassAwareOptionsFactory<>), typeof(ClassAwareOptionsFactory<>));

        services.TryAddSingleton(typeof(IClassAwareOptionsCache<>), typeof(ClassAwareOptionsCache<>));

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

    public static IServiceCollection AddLoggerFactorySetter(this IServiceCollection services)
    {
        if (services.Any(static x => x.ServiceType == typeof(ILoggerFactorySetter)))
        {
            return services;
        }

        return services
            .AddLogging()
            .Decorate<ILoggerFactory, LoggerFactorySetter>()
            .AddSingleton(static p => (ILoggerFactorySetter)p.GetRequiredService<ILoggerFactory>());
    }
}
