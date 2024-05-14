using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;

namespace Diginsight.Diagnostics.AspNetCore;

public static class DependencyInjectionExtensions
{
    public static TracerProviderBuilder SetHttpHeadersSampler(
        this TracerProviderBuilder builder,
        Func<IServiceProvider, Sampler> makeInitial,
        Func<IServiceProvider, Sampler, Sampler>? makeFinal = null
    )
    {
        return builder
            .ConfigureServices(
                static services => services.Configure<DiginsightDistributedContextOptions>(
                    static x => { x.NonBaggageKeys.Add(HttpHeadersSampler.HeaderName); }
                )
            )
            .SetSampler(
                sp =>
                {
                    Sampler initial = makeInitial(sp);
                    Sampler candidate = ActivatorUtilities.CreateInstance<HttpHeadersSampler>(sp, initial);
                    return makeFinal?.Invoke(sp, candidate) ?? candidate;
                }
            );
    }
}
