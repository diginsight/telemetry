using Diginsight.CAOptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
#if NET
using Microsoft.AspNetCore.Builder;
#endif

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
    private static IServiceCollection AddDynamicLogLevelCore(IServiceCollection services)
    {
        return services
            .AddLoggerFactorySetter()
            .AddHttpContextAccessor()
            .Decorate<IHttpContextFactory, DynamicLogLevelHttpContextFactory>();
    }

    public static IServiceCollection AddAspNetCorePropagator(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.TryAddSingleton<DistributedContextPropagator>(
            static sp => ActivatorUtilities.CreateInstance<AspNetCorePropagator>(sp, DistributedContextPropagator.Current)
        );
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IOnCreateServiceProvider, SetCurrentPropagator>());

        return services;
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

#if NET
    public static IEndpointConventionBuilder MapVolatileConfiguration(this IEndpointRouteBuilder endpoints, string pattern = ".volatile-configuration")
#else
    public static IRouteBuilder MapVolatileConfiguration(this IRouteBuilder routes, string template = ".volatile-configuration")
#endif
    {
        static Task ApplyVolatileConfigurationAsync(HttpContext httpContext)
        {
            IServiceProvider serviceProvider = httpContext.RequestServices;
            IVolatileConfigurationStorageProvider storageProvider = serviceProvider.GetRequiredService<IVolatileConfigurationStorageProvider>();

            string method = httpContext.Request.Method;
            bool delete = method == HttpMethods.Delete;
            bool overwrite = method != HttpMethods.Patch;

            foreach (IAspNetCoreVolatileConfigurationLoader loader in serviceProvider.GetServices<IAspNetCoreVolatileConfigurationLoader>())
            {
                IVolatileConfigurationStorage storage = storageProvider.Get(loader.StorageName);
                IEnumerable<KeyValuePair<string, string?>> entries = delete ? [ ] : loader.Load(httpContext);
                storage.Apply(entries, overwrite);
            }

            httpContext.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        }

        IServiceProvider serviceProvider =
#if NET
            endpoints.ServiceProvider;
#else
            routes.ServiceProvider;
#endif
        if (serviceProvider.GetService<IVolatileConfigurationStorageProvider>() is null)
        {
            throw new InvalidOperationException($"Required service {nameof(IVolatileConfigurationStorageProvider)} not registered");
        }

#if NET
        return endpoints.MapMethods(pattern, [ HttpMethods.Put, HttpMethods.Patch, HttpMethods.Delete ], ApplyVolatileConfigurationAsync);
#else
        return routes
            .MapPut(template, ApplyVolatileConfigurationAsync)
            .MapVerb(HttpMethods.Patch, template, ApplyVolatileConfigurationAsync)
            .MapDelete(template, ApplyVolatileConfigurationAsync);
#endif
    }
}
