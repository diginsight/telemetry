using Diginsight.Diagnostics;
using Diginsight.SmartCache.Externalization.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace Diginsight.SmartCache.Externalization.AspNetCore;

internal sealed class SmartCacheMiddleware : IMiddleware
{
#if !NET
    private static string? tempDirectory;
#endif

    private readonly ISmartCache smartCache;
    private readonly ISmartCacheHttpOptions httpOptions;

    public SmartCacheMiddleware(
        ISmartCache smartCache,
        IOptions<SmartCacheHttpOptions> httpOptions
    )
    {
        this.smartCache = smartCache;
        this.httpOptions = httpOptions.Value;
    }

    public async Task InvokeAsync(HttpContext httpContext, RequestDelegate next)
    {
        HttpRequest request = httpContext.Request;
        if (request.Method != HttpMethod.Post.Method || !request.Path.StartsWithSegments(httpOptions.RootPath, out PathString remPath0))
        {
            await next(httpContext);
            return;
        }

        string? remPath = remPath0.Value;
        IActionResult actionResult;
        if (string.Equals(remPath, httpOptions.GetPathSegment, StringComparison.OrdinalIgnoreCase))
        {
            actionResult = await GetAsync(httpContext);
        }
        else if (string.Equals(remPath, httpOptions.CacheMissPathSegment, StringComparison.OrdinalIgnoreCase))
        {
            actionResult = await CacheMissAsync(httpContext);
        }
        else if (string.Equals(remPath, httpOptions.InvalidatePathSegment, StringComparison.OrdinalIgnoreCase))
        {
            actionResult = await InvalidateAsync(httpContext);
        }
        else
        {
            await next(httpContext);
            return;
        }

        ActionContext actionContext = new (httpContext, httpContext.GetRouteData(), new ActionDescriptor());
        await actionResult.ExecuteResultAsync(actionContext);
    }

    private async Task<IActionResult> GetAsync(HttpContext httpContext)
    {
        byte[] rawValue;
        using (TimerLap lap = SmartCacheObservability.Instruments.FetchDuration.StartLap(SmartCacheObservability.Tags.Type.Direct))
        {
            ICacheKey key = await DeserializeBodyAsync<ICacheKey>(httpContext);
            if (!smartCache.TryGetDirectFromMemory(key, out Type? type, out object? value))
            {
                lap.AddTags(SmartCacheObservability.Tags.Found.False);
                return new NotFoundResult();
            }

            lap.AddTags(SmartCacheObservability.Tags.Found.True);
            rawValue = SmartCacheSerialization.SerializeToBytes(value, type);
        }

        return new FileContentResult(rawValue, $"application/json; charset={SmartCacheSerialization.Encoding.WebName}");
    }

    private async Task<IActionResult> CacheMissAsync(HttpContext httpContext)
    {
        CacheMissDescriptor descriptor = await DeserializeBodyAsync<CacheMissDescriptor>(httpContext);

        smartCache.AddExternalMiss(descriptor);

        return new OkResult();
    }

    private async Task<IActionResult> InvalidateAsync(HttpContext httpContext)
    {
        InvalidationDescriptor descriptor = await DeserializeBodyAsync<InvalidationDescriptor>(httpContext);

        smartCache.Invalidate(descriptor);

        return new OkResult();
    }

    private static async Task<T> DeserializeBodyAsync<T>(HttpContext httpContext)
        where T : notnull
    {
#if NET
        await using FileBufferingReadStream stream = new (httpContext.Request.Body, 100 * 1024);
#else
#if NETSTANDARD2_1_OR_GREATER
        await using FileBufferingReadStream stream = new (
#else
        using FileBufferingReadStream stream = new (
#endif
            httpContext.Request.Body,
            100 * 1024,
            null,
            static () =>
            {
                if (tempDirectory == null)
                {
                    string str = Environment.GetEnvironmentVariable("ASPNETCORE_TEMP") ?? Path.GetTempPath();
                    tempDirectory = Directory.Exists(str) ? str : throw new DirectoryNotFoundException(str);
                }

                return tempDirectory;
            }
        );
#endif
        await stream.DrainAsync(CancellationToken.None);
        stream.Position = 0;

        return SmartCacheSerialization.Deserialize<T>(stream);
    }
}
