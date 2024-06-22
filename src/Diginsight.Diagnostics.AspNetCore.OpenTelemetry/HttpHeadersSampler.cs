using Microsoft.AspNetCore.Http;
using OpenTelemetry.Trace;

namespace Diginsight.Diagnostics.AspNetCore;

internal sealed class HttpHeadersSampler : Sampler
{
    internal const string HeaderName = "Activity-Sampling";

    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly Sampler decoratee;

    public HttpHeadersSampler(IHttpContextAccessor httpContextAccessor, Sampler decoratee)
    {
        this.httpContextAccessor = httpContextAccessor;
        this.decoratee = decoratee;
    }

    public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
    {
        return HttpHeadersHelper.ShouldInclude(null, samplingParameters.Name, HeaderName, httpContextAccessor) is { } shouldInclude
            ? new SamplingResult(shouldInclude)
            : decoratee.ShouldSample(in samplingParameters);
    }
}
