using Diginsight.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
            string method = httpContext.Request.Method;
            bool delete = method == HttpMethods.Delete;
            bool overwrite = method != HttpMethods.Patch;

            AspNetCoreVolatileConfiguration.Apply(httpContext, delete, overwrite);

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
