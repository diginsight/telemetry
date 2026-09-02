using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;
using System.ComponentModel;

namespace Diginsight.Diagnostics.AspNetCore;

/// <summary>
/// Provides extension methods for configuring Diginsight ASP.NET Core OpenTelemetry integration.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Sets a sampler that can be controlled by HTTP headers.
    /// </summary>
    /// <param name="builder">The tracer provider builder.</param>
    /// <param name="makeInitial">The function used to create the initial sampler.</param>
    /// <param name="makeFinal">The function used to decorate or replace the HTTP headers sampler.</param>
    /// <returns>The tracer provider builder, for chaining.</returns>
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
