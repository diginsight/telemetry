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

/// <summary>
/// Provides extension methods for registering Diginsight ASP.NET Core services.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ServiceCollectionExtensions
{
    /// <param name="services">The service collection.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds dynamic log level services using the specified injector type.
        /// </summary>
        /// <typeparam name="T">The dynamic log level injector type.</typeparam>
        /// <returns>The service collection, for chaining.</returns>
        public IServiceCollection AddDynamicLogLevel<T>()
            where T : class, IDynamicLogLevelInjector
        {
            services.AddDynamicLogLevelCore();
            services.TryAddTransient<IDynamicLogLevelInjector, T>();
            return services;
        }

        /// <summary>
        /// Adds dynamic log level services using the specified injector factory.
        /// </summary>
        /// <param name="implementationFactory">The dynamic log level injector factory.</param>
        /// <returns>The service collection, for chaining.</returns>
        public IServiceCollection AddDynamicLogLevel(
            Func<IServiceProvider, IDynamicLogLevelInjector> implementationFactory
        )
        {
            services.AddDynamicLogLevelCore();
            services.TryAddTransient(implementationFactory);
            return services;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IServiceCollection AddDynamicLogLevelCore()
        {
            return services
                .AddLoggerFactorySetter()
                .AddHttpContextAccessor()
                .Decorate<IHttpContextFactory, DynamicLogLevelHttpContextFactory>();
        }

        /// <summary>
        /// Adds the ASP.NET Core propagator to the specified service collection.
        /// </summary>
        /// <returns>The service collection, for chaining.</returns>
        public IServiceCollection AddAspNetCorePropagator()
        {
            services.AddHttpContextAccessor();
            services.TryAddSingleton<DistributedContextPropagator>(
                static sp => ActivatorUtilities.CreateInstance<AspNetCorePropagator>(sp, DistributedContextPropagator.Current)
            );
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IOnCreateServiceProvider, SetCurrentPropagator>());

            return services;
        }
    }

    /// <summary>
    /// Sets the current distributed context propagator when the service provider is created.
    /// </summary>
    public sealed class SetCurrentPropagator : IOnCreateServiceProvider
    {
        private readonly DistributedContextPropagator propagator;

        /// <summary>
        /// Initializes a new instance of the <see cref="SetCurrentPropagator" /> class.
        /// </summary>
        /// <param name="propagator">The distributed context propagator.</param>
        public SetCurrentPropagator(DistributedContextPropagator propagator)
        {
            this.propagator = propagator;
        }

        /// <inheritdoc />
        public void Run()
        {
            DistributedContextPropagator.Current = propagator;
        }
    }

#if NET
    /// <summary>
    /// Maps the volatile configuration endpoint to the specified endpoint route builder.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <returns>The endpoint convention builder.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the volatile configuration storage provider is not registered.</exception>
    public static IEndpointConventionBuilder MapVolatileConfiguration(this IEndpointRouteBuilder endpoints, string pattern = ".volatile-configuration")
#else
    /// <summary>
    /// Maps the volatile configuration endpoint to the specified route builder.
    /// </summary>
    /// <param name="routes">The route builder.</param>
    /// <param name="template">The route template.</param>
    /// <returns>The route builder.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the volatile configuration storage provider is not registered.</exception>
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
            endpoints
#else
            routes
#endif
                .ServiceProvider;
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
