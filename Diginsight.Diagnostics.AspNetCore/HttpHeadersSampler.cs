using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using OpenTelemetry.Trace;
using System.Text.RegularExpressions;

namespace Diginsight.Diagnostics.AspNetCore;

public sealed class HttpHeadersSampler : Sampler
{
    private static readonly Regex SpecRegex = new (@"^([^+\-]+?)([+\-])$", RegexOptions.IgnoreCase);

    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly Sampler decoratee;

    public HttpHeadersSampler(IHttpContextAccessor httpContextAccessor, Sampler decoratee)
    {
        this.httpContextAccessor = httpContextAccessor;
        this.decoratee = decoratee;
    }

    public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
    {
        string name = samplingParameters.Name;

        StringValues rawSpecs = httpContextAccessor.HttpContext?.Request.Headers["Activity-Sampling"] ?? default;
        foreach (string? rawSpec in rawSpecs.Reverse())
        {
            if (SpecRegex.Match(rawSpec!) is not { Success: true } match)
            {
                continue;
            }

            if (ActivityExtensions.MatchesActivityNamePattern(name, match.Groups[1].Value))
            {
                return new SamplingResult(match.Groups[2].Value == "+");
            }
        }

        return decoratee.ShouldSample(in samplingParameters);
    }
}
