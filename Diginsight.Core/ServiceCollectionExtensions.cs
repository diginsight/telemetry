using Diginsight.CAOptions;
using Diginsight.Strings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Diginsight;

public static class ServiceCollectionExtensions
{
#if EXPERIMENT_CLASS_CONFIGURATION_GETTER
    public static IServiceCollection AddClassConfigurationGetter(this IServiceCollection services, IConfiguration? rootConfiguration = null)
    {
        services.TryAddSingleton(typeof(IClassConfigurationGetter), typeof(ClassConfigurationGetter));
        services.TryAddSingleton(typeof(IClassConfigurationGetter<>), typeof(ClassConfigurationGetter<>));
        services.TryAddSingleton<IClassConfigurationGetterProvider, ClassConfigurationGetterProvider>();
        if (rootConfiguration is not null)
        {
            services.TryAddSingleton(new ClassConfigurationGetter.ConfigurationWrapper(rootConfiguration));
        }

        return services;
    }
#endif

    public static IServiceCollection AddClassAwareOptions(this IServiceCollection services)
    {
        services.TryAddSingleton(typeof(IClassAwareOptions<>), typeof(UnnamedClassAwareOptionsManager<>));
        services.TryAddScoped(typeof(IClassAwareOptionsSnapshot<>), typeof(ClassAwareOptionsManager<>));
        services.TryAddSingleton(typeof(IClassAwareOptionsMonitor<>), typeof(ClassAwareOptionsMonitor<>));
        services.TryAddTransient(typeof(IClassAwareOptionsFactory<>), typeof(ClassAwareOptionsFactory<>));
        services.TryAddSingleton(typeof(IClassAwareOptionsCache<>), typeof(ClassAwareOptionsCache<>));

        return services;
    }

    public static IServiceCollection ConfigureClassAware<TOptions>(
        this IServiceCollection services, IConfiguration configuration, string? sectionKey = null, Action<BinderOptions>? configureBinder = null
    )
        where TOptions : class
    {
        return services.ConfigureClassAware<TOptions>(Options.DefaultName, configuration, sectionKey, configureBinder);
    }

    public static IServiceCollection ConfigureClassAware<TOptions>(
        this IServiceCollection services, string name, IConfiguration configuration, string? sectionKey = null, Action<BinderOptions>? configureBinder = null
    )
        where TOptions : class
    {
        services.AddClassAwareOptions();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IConfigureClassAwareOptions<TOptions>>(
                new ConfigureClassAwareOptionsFromConfiguration<TOptions>(name, configuration, sectionKey, configureBinder)
            )
        );

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
