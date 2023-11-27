using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Diginsight.AspNetCore;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDynamicLogLevel<T>(this IServiceCollection services)
        where T : DynamicLogLevelMiddleware
    {
        AddDynamicLogLevelCore(services);
        services.TryAddTransient<DynamicLogLevelMiddleware, T>();
        return services;
    }

    public static IServiceCollection AddDynamicLogLevel(
        this IServiceCollection services, Func<IServiceProvider, DynamicLogLevelMiddleware> implementationFactory
    )
    {
        AddDynamicLogLevelCore(services);
        services.TryAddTransient(implementationFactory);
        return services;
    }

    private static void AddDynamicLogLevelCore(IServiceCollection services)
    {
        services
            .AddLoggerFactorySetter()
            .TryAddEnumerable(ServiceDescriptor.Singleton<IStartupFilter, DynamicLogLevelStartupFilter>());
    }

    private sealed class DynamicLogLevelStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                app.UseMiddleware<DynamicLogLevelMiddleware>();
                next(app);
            };
        }
    }
}
