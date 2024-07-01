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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IServiceCollection DynamicallyConfigureFromHttpRequestHeaders<TOptions>(this IServiceCollection services)
        where TOptions: class, IDynamicallyConfigurable
    {
        return services.DynamicallyConfigureFromHttpRequestHeaders<TOptions>(Options.DefaultName);
    }

    public static IServiceCollection DynamicallyConfigureFromHttpRequestHeaders<TOptions>(this IServiceCollection services, string name)
        where TOptions: class, IDynamicallyConfigurable
    {
        services
            .AddOptions()
            .AddHttpContextAccessor();

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IConfigureOptions<TOptions>, DynamicallyConfigureOptionsFromHttpRequestHeaders<TOptions>>(
                sp => ActivatorUtilities.CreateInstance<DynamicallyConfigureOptionsFromHttpRequestHeaders<TOptions>>(sp, name)
            )
        );

        services.Configure<DiginsightDistributedContextOptions>(
            static x => { x.NonBaggageKeys.Add(DynamicallyConfigureOptionsFromHttpRequestHeaders<TOptions>.HeaderName); }
        );

        services.FlagAsDynamic<TOptions>(name);

        return services;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IServiceCollection DynamicallyConfigureClassAwareFromHttpRequestHeaders<TOptions>(this IServiceCollection services)
        where TOptions: class, IDynamicallyConfigurable
    {
        return services.DynamicallyConfigureClassAwareFromHttpRequestHeaders<TOptions>(Options.DefaultName);
    }

    public static IServiceCollection DynamicallyConfigureClassAwareFromHttpRequestHeaders<TOptions>(this IServiceCollection services, string name)
        where TOptions: class, IDynamicallyConfigurable
    {
        services.AddClassAwareOptions();
        services.DynamicallyConfigureFromHttpRequestHeaders<TOptions>(name);

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IConfigureClassAwareOptions<TOptions>, DynamicallyConfigureClassAwareOptionsFromHttpRequestHeaders<TOptions>>(
                sp => ActivatorUtilities.CreateInstance<DynamicallyConfigureClassAwareOptionsFromHttpRequestHeaders<TOptions>>(sp, name)
            )
        );

        return services;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IServiceCollection DynamicallyPostConfigureFromHttpRequestHeaders<TOptions>(this IServiceCollection services)
        where TOptions: class, IDynamicallyConfigurable
    {
        return services.DynamicallyPostConfigureFromHttpRequestHeaders<TOptions>(Options.DefaultName);
    }

    public static IServiceCollection DynamicallyPostConfigureFromHttpRequestHeaders<TOptions>(this IServiceCollection services, string name)
        where TOptions: class, IDynamicallyConfigurable
    {
        services
            .AddOptions()
            .AddHttpContextAccessor();

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IPostConfigureOptions<TOptions>, DynamicallyConfigureOptionsFromHttpRequestHeaders<TOptions>>(
                sp => ActivatorUtilities.CreateInstance<DynamicallyConfigureOptionsFromHttpRequestHeaders<TOptions>>(sp, name)
            )
        );

        services.Configure<DiginsightDistributedContextOptions>(
            static x => { x.NonBaggageKeys.Add(DynamicallyConfigureOptionsFromHttpRequestHeaders<TOptions>.HeaderName); }
        );

        services.FlagAsDynamic<TOptions>(name);

        return services;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IServiceCollection DynamicallyPostConfigureClassAwareFromHttpRequestHeaders<TOptions>(this IServiceCollection services)
        where TOptions: class, IDynamicallyConfigurable
    {
        return services.DynamicallyPostConfigureClassAwareFromHttpRequestHeaders<TOptions>(Options.DefaultName);
    }

    public static IServiceCollection DynamicallyPostConfigureClassAwareFromHttpRequestHeaders<TOptions>(this IServiceCollection services, string name)
        where TOptions: class, IDynamicallyConfigurable
    {
        services.AddClassAwareOptions();
        services.DynamicallyPostConfigureFromHttpRequestHeaders<TOptions>(name);

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IPostConfigureClassAwareOptions<TOptions>, DynamicallyConfigureClassAwareOptionsFromHttpRequestHeaders<TOptions>>(
                sp => ActivatorUtilities.CreateInstance<DynamicallyConfigureClassAwareOptionsFromHttpRequestHeaders<TOptions>>(sp, name)
            )
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
