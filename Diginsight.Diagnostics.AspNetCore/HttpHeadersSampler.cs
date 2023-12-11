using Microsoft.AspNetCore.Http;
using OpenTelemetry.Trace;

namespace Diginsight.Diagnostics.AspNetCore;

public sealed class HttpHeadersSampler : Sampler
{
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly Sampler decoratee;

    public HttpHeadersSampler(IHttpContextAccessor httpContextAccessor, Sampler decoratee)
    {
        this.httpContextAccessor = httpContextAccessor;
        this.decoratee = decoratee;
    }

    public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
    {
        return HttpHeadersHelper.ShouldInclude(samplingParameters.Name, "Activity-Sampling", httpContextAccessor) is { } shouldInclude
            ? new SamplingResult(shouldInclude)
            : decoratee.ShouldSample(in samplingParameters);
    }
}
