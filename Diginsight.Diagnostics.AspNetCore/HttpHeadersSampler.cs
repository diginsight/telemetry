using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using OpenTelemetry.Trace;
using System.Text.RegularExpressions;

namespace Diginsight.Diagnostics.AspNetCore;

public sealed class HttpHeadersSampler : Sampler
{
    private static readonly Regex SpecRegex = new (@"^([^+\-]+?)([+\-])$", RegexOptions.IgnoreCase);
#if !(NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
    private static readonly char[] StarSeparators = { '*' };
#endif

    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly Sampler decoratee;

    public HttpHeadersSampler(IHttpContextAccessor httpContextAccessor, Sampler decoratee)
    {
        this.httpContextAccessor = httpContextAccessor;
        this.decoratee = decoratee;
    }

    public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
    {
        StringValues rawSpecs = httpContextAccessor.HttpContext?.Request.Headers["Activity-Sampling"] ?? default;
        foreach (string? rawSpec in rawSpecs.Reverse())
        {
            if (SpecRegex.Match(rawSpec!) is not { Success: true } match)
            {
                continue;
            }

            string namePattern = match.Groups[1].Value;

#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            bool isMatch = namePattern.Split('*', 3) switch
#else
            string[] tokens = namePattern.Split(StarSeparators, 3);
            string startToken;
            string endToken;

            bool isMatch = tokens.Length switch
#endif
            {
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                [_] => samplingParameters.Name == namePattern,
                [var startToken, var endToken] => (startToken, endToken) switch
#else
                1 => samplingParameters.Name == namePattern,
                2 => (startToken = tokens[0], endToken = tokens[1]) switch
#endif
                {
                    ("", "") => true,
                    ("", _) => samplingParameters.Name.EndsWith(endToken, StringComparison.OrdinalIgnoreCase),
                    (_, "") => samplingParameters.Name.StartsWith(startToken, StringComparison.OrdinalIgnoreCase),
                    (_, _) => samplingParameters.Name.StartsWith(startToken, StringComparison.OrdinalIgnoreCase) &&
                        samplingParameters.Name.EndsWith(endToken, StringComparison.OrdinalIgnoreCase),
                },
                _ => throw new ArgumentException("Invalid activity name"),
            };

            if (isMatch)
            {
                return new SamplingResult(match.Groups[2].Value == "+");
            }
        }

        return decoratee.ShouldSample(in samplingParameters);
    }
}
