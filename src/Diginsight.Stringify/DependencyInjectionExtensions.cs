using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.ComponentModel;

namespace Diginsight.Stringify;

/// <summary>
/// Provides extension methods for registering stringification services.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Registers stringification services in the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddStringify(this IServiceCollection services)
    {
        services.AddOptions();
        services.TryAddSingleton<IStringifyContextFactory, StringifyContextFactory>();
        services.TryAddSingleton<IMemberInfoStringifier, MemberInfoStringifier>();
        services.TryAddSingleton<IReflectionStringifyHelper, ReflectionStringifyHelper>();

        return services;
    }
}
