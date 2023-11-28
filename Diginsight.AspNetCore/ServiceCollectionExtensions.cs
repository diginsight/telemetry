using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Runtime.CompilerServices;

namespace Diginsight.AspNetCore;

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
    private static void AddDynamicLogLevelCore(IServiceCollection services)
    {
        services.AddLoggerFactorySetter();
        services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.Decorate<IHttpContextFactory, DynamicLogLevelHttpContextFactory>();
    }
}
