using Diginsight.CAOptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;

namespace Diginsight.AspNetCore;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection PostConfigureClassAwareFromHttpRequestHeaders<TOptions>(this IServiceCollection services)
        where TOptions: class
    {
        return services.PostConfigureClassAwareFromHttpRequestHeaders<TOptions>(Options.DefaultName);
    }

    public static IServiceCollection PostConfigureClassAwareFromHttpRequestHeaders<TOptions>(this IServiceCollection services, string name)
        where TOptions: class
    {
        services.AddClassAwareOptions();

        services.AddHttpContextAccessor();
        services.TryAddSingleton(
            sp => ActivatorUtilities.CreateInstance<PostConfigureClassAwareOptionsFromHttpRequestHeaders<TOptions>>(sp, name)
        );
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureClassAwareOptions<TOptions>, PostConfigureClassAwareOptionsFromHttpRequestHeaders<TOptions>>(
            static sp => sp.GetRequiredService<PostConfigureClassAwareOptionsFromHttpRequestHeaders<TOptions>>())
        );
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IOptionsChangeTokenSource<TOptions>, PostConfigureClassAwareOptionsFromHttpRequestHeaders<TOptions>>(
            static sp => sp.GetRequiredService<PostConfigureClassAwareOptionsFromHttpRequestHeaders<TOptions>>())
        );
        return services;
    }

    public static IServiceCollection AddDynamicLogLevel<T>(this IServiceCollection services)
        where T : class, IDynamicLogLevelInjector
    {
        AddDynamicLogLevelCore(services);
        services.TryAddTransient<IDynamicLogLevelInjector, T>();
        return services;
    }

    public static IServiceCollection AddDynamicLogLevel(
        this IServiceCollection services, Func<IServiceProvider, IDynamicLogLevelInjector> implementationFactory
    )
    {
        AddDynamicLogLevelCore(services);
        services.TryAddTransient(implementationFactory);
        return services;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AddDynamicLogLevelCore(IServiceCollection services)
    {
        services.AddLoggerFactorySetter();
        services.AddHttpContextAccessor();
        services.Decorate<IHttpContextFactory, DynamicLogLevelHttpContextFactory>();
    }
}
