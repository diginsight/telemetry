using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenTelemetry.Trace;

namespace Diginsight.Diagnostics.AspNetCore;

public static class DependencyInjectionExtensions
{
    public static TracerProviderBuilder SetHttpHeadersDynamicSampler<T>(this TracerProviderBuilder tracerProviderBuilder)
        where T : Sampler
    {
        tracerProviderBuilder.ConfigureServices(static s => s.TryAddSingleton<T>());
        return tracerProviderBuilder.SetSampler(static sp => ActivatorUtilities.CreateInstance<HttpHeadersSampler>(sp, sp.GetRequiredService<T>()));
    }

    public static TracerProviderBuilder SetHttpHeadersDynamicSampler(this TracerProviderBuilder tracerProviderBuilder, Sampler sampler)
    {
        return tracerProviderBuilder.SetSampler(sp => ActivatorUtilities.CreateInstance<HttpHeadersSampler>(sp, sampler));
    }

    public static TracerProviderBuilder SetHttpHeadersDynamicSampler(this TracerProviderBuilder tracerProviderBuilder, Func<IServiceProvider, Sampler> makeSampler)
    {
        return tracerProviderBuilder.SetSampler(sp => ActivatorUtilities.CreateInstance<HttpHeadersSampler>(sp, makeSampler(sp)));
    }
}
