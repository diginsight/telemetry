using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.ComponentModel;

namespace Diginsight.Stringify;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddStringify(this IServiceCollection services)
    {
        services.AddOptions();
        services.TryAddSingleton<IStringifyContextFactory, StringifyContextFactory>();
        services.TryAddSingleton<IMemberInfoStringifier, MemberInfoStringifier>();
        services.TryAddSingleton<IReflectionStringifyHelper, ReflectionStringifyHelper>();

        return services;
    }
}
