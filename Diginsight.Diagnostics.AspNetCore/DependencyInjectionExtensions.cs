using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics.AspNetCore;

public static class DependencyInjectionExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IWebHostBuilder UseObservabilityServiceProvider(
        this IWebHostBuilder hostBuilder,
        Action<WebHostBuilderContext, ObservabilityServiceProviderOptions>? configureOptions = null
    )
    {
        return hostBuilder.ConfigureServices(
            (context, services) =>
            {
                ObservabilityServiceProviderOptions options = new ();
                configureOptions?.Invoke(context, options);
                services.Replace(ServiceDescriptor.Singleton<IServiceProviderFactory<IServiceCollection>>(new ObservabilityServiceProviderFactory(options)));
            }
        );
    }
}
