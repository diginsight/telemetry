using Diginsight.CAOptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System.ComponentModel;
using System.Diagnostics;
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
        services.AddHttpContextAccessor();

        services.TryAddSingleton(
            sp => ActivatorUtilities.CreateInstance<PostConfigureOptionsFromHttpRequestHeaders<TOptions>>(sp, name)
        );
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<TOptions>, PostConfigureOptionsFromHttpRequestHeaders<TOptions>>(
            static sp => sp.GetRequiredService<PostConfigureOptionsFromHttpRequestHeaders<TOptions>>())
        );
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IOptionsChangeTokenSource<TOptions>, PostConfigureOptionsFromHttpRequestHeaders<TOptions>>(
            static sp => sp.GetRequiredService<PostConfigureOptionsFromHttpRequestHeaders<TOptions>>())
        );

        services.Configure<DiginsightDistributedContextOptions>(
            static x => { x.NonBaggageKeys.Add(PostConfigureOptionsFromHttpRequestHeaders<TOptions>.HeaderName); }
        );

        services.FlagAsDynamic<TOptions>(name);

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
        services.AddClassAwareOptions();
        services.PostConfigureFromHttpRequestHeaders<TOptions>(name);

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

    public static void AddAspNetCorePropagator(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.TryAddSingleton<DistributedContextPropagator>(
            static sp => ActivatorUtilities.CreateInstance<AspNetCorePropagator>(sp, DistributedContextPropagator.Current)
        );
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IOnCreateServiceProvider, SetCurrentPropagator>());
    }

    public sealed class SetCurrentPropagator : IOnCreateServiceProvider
    {
        private readonly DistributedContextPropagator propagator;

        public SetCurrentPropagator(DistributedContextPropagator propagator)
        {
            this.propagator = propagator;
        }

        public void Run()
        {
            DistributedContextPropagator.Current = propagator;
        }
    }
}
