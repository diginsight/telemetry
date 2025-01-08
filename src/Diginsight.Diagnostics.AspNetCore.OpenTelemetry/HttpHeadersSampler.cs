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
        bool? shouldInclude = HttpHeadersHelper.ShouldInclude(null, samplingParameters.Name, HeaderName, httpContextAccessor);

        if (shouldInclude is not null)
        {
            var samplingResult = new SamplingResult(shouldInclude.Value);
            return samplingResult;
        }
        else
        {
            var shouldSample = decoratee.ShouldSample(in samplingParameters);
            return shouldSample;
        }
    }
}
