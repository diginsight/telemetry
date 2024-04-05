using Diginsight.CAOptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Diginsight.AspNetCore;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection PostConfigureFromHttpRequestHeaders<TOptions>(this IServiceCollection services)
        where TOptions: class, IDynamicallyPostConfigurable
    {
        return services.PostConfigureFromHttpRequestHeaders<TOptions>(Options.DefaultName);
    }

    public static IServiceCollection PostConfigureFromHttpRequestHeaders<TOptions>(this IServiceCollection services, string name)
        where TOptions: class, IDynamicallyPostConfigurable
    {
        services.AddOptions();

        services.AddHttpContextAccessor();

        services.TryAddSingleton(
            sp => new PostConfigureOptionsFromHttpRequestHeaders<TOptions>(name, sp.GetRequiredService<IHttpContextAccessor>())
        );
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<TOptions>, PostConfigureOptionsFromHttpRequestHeaders<TOptions>>(
            static sp => sp.GetRequiredService<PostConfigureOptionsFromHttpRequestHeaders<TOptions>>())
        );
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IOptionsChangeTokenSource<TOptions>, PostConfigureOptionsFromHttpRequestHeaders<TOptions>>(
            static sp => sp.GetRequiredService<PostConfigureOptionsFromHttpRequestHeaders<TOptions>>())
        );

        return services;
    }

    public static IServiceCollection PostConfigureClassAwareFromHttpRequestHeaders<TOptions>(this IServiceCollection services)
        where TOptions: class, IDynamicallyPostConfigurable
    {
        return services.PostConfigureClassAwareFromHttpRequestHeaders<TOptions>(Options.DefaultName);
    }

    public static IServiceCollection PostConfigureClassAwareFromHttpRequestHeaders<TOptions>(this IServiceCollection services, string name)
        where TOptions: class, IDynamicallyPostConfigurable
    {
        services.PostConfigureFromHttpRequestHeaders<TOptions>(name);

        services.AddClassAwareOptions();

        services.AddHttpContextAccessor();

        services.TryAddSingleton(
            sp => new PostConfigureClassAwareOptionsFromHttpRequestHeaders<TOptions>(name, sp.GetRequiredService<IHttpContextAccessor>())
        );
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureClassAwareOptions<TOptions>, PostConfigureClassAwareOptionsFromHttpRequestHeaders<TOptions>>(
            static sp => sp.GetRequiredService<PostConfigureClassAwareOptionsFromHttpRequestHeaders<TOptions>>())
        );
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IClassAwareOptionsChangeTokenSource<TOptions>, PostConfigureClassAwareOptionsFromHttpRequestHeaders<TOptions>>(
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
