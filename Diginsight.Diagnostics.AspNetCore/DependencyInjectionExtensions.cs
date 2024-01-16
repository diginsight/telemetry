using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenTelemetry.Trace;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics.AspNetCore;

public static class DependencyInjectionExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TracerProviderBuilder SetHttpHeadersDynamicSampler<T>(this TracerProviderBuilder tracerProviderBuilder)
        where T : Sampler
    {
        tracerProviderBuilder.ConfigureServices(static s => s.TryAddSingleton<T>());
        return tracerProviderBuilder.SetHttpHeadersDynamicSampler(static sp => sp.GetRequiredService<T>());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TracerProviderBuilder SetHttpHeadersDynamicSampler(this TracerProviderBuilder tracerProviderBuilder, Sampler sampler)
    {
        return tracerProviderBuilder.SetHttpHeadersDynamicSampler(_ => sampler);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TracerProviderBuilder SetHttpHeadersDynamicSampler(this TracerProviderBuilder tracerProviderBuilder, Func<IServiceProvider, Sampler> makeSampler)
    {
        return tracerProviderBuilder
            .ConfigureServices(static s => s.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>())
            .SetSampler(sp => ActivatorUtilities.CreateInstance<HttpHeadersSampler>(sp, makeSampler(sp)));
    }

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
