using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics.AspNetCore;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class DependencyInjectionExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IWebHostBuilder UseDiginsightServiceProvider(
        this IWebHostBuilder hostBuilder,
        Action<WebHostBuilderContext, ServiceProviderOptions>? configureOptions = null
    )
    {
        return hostBuilder.ConfigureServices(
            (context, services) =>
            {
                ServiceProviderOptions options = new ();
                configureOptions?.Invoke(context, options);
                services.Replace(ServiceDescriptor.Singleton<IServiceProviderFactory<IServiceCollection>>(new DiginsightServiceProviderFactory(options)));
            }
        );
    }
}
