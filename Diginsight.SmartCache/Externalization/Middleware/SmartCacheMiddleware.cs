using Diginsight.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using System.Text;

namespace Diginsight.SmartCache.Externalization.Middleware;

internal sealed class SmartCacheMiddleware : IMiddleware
{
    private static class Metrics
    {
        public static readonly TimerHistogram FetchDuration = SelfObservabilityUtils.Meter.CreateTimer("fetch_duration");

        public static class Tags
        {
            public static readonly KeyValuePair<string, object?> Found = new ("found", true);
            public static readonly KeyValuePair<string, object?> NotFound = new ("found", false);
        }
    }

#if !NET6_0_OR_GREATER
    private static string? tempDirectory;
#endif

    private readonly ISmartCacheService cacheService;
    private readonly ISmartCacheMiddlewareOptions middlewareOptions;

    public SmartCacheMiddleware(
        ISmartCacheService cacheService,
        IOptions<SmartCacheMiddlewareOptions> middlewareOptions
    )
    {
        this.cacheService = cacheService;
        this.middlewareOptions = middlewareOptions.Value;
    }

    public async Task InvokeAsync(HttpContext httpContext, RequestDelegate next)
    {
        HttpRequest request = httpContext.Request;
        if (request.Method != HttpMethod.Post.Method || !request.Path.StartsWithSegments(middlewareOptions.RootPath, out PathString remPath0))
        {
            await next(httpContext);
            return;
        }

        string? remPath = remPath0.Value;
        IActionResult actionResult;
        if (string.Equals(remPath, middlewareOptions.GetPathSegment, StringComparison.OrdinalIgnoreCase))
        {
            actionResult = await GetAsync(httpContext);
        }
        else if (string.Equals(remPath, middlewareOptions.CacheMissPathSegment, StringComparison.OrdinalIgnoreCase))
        {
            actionResult = await CacheMissAsync(httpContext);
        }
        else if (string.Equals(remPath, middlewareOptions.InvalidatePathSegment, StringComparison.OrdinalIgnoreCase))
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
        using (var lap = Metrics.FetchDuration.StartLap())
        {
            ICacheKey key = await DeserializeBodyAsync<ICacheKey>(httpContext);
            if (!cacheService.TryGetDirectFromMemory(key, out Type? type, out object? value))
            {
                lap.AddTags(Metrics.Tags.NotFound);
                return new NotFoundResult();
            }

            lap.AddTags(Metrics.Tags.Found);
            rawValue = SmartCacheSerialization.SerializeToBytes(value, type);
        }

        Encoding encoding = SmartCacheSerialization.Encoding;

        return new FileContentResult(rawValue, $"application/json; charset={encoding.WebName}");
    }

    private async Task<IActionResult> CacheMissAsync(HttpContext httpContext)
    {
        CacheMissDescriptor descriptor = await DeserializeBodyAsync<CacheMissDescriptor>(httpContext);

        cacheService.AddExternalMiss(descriptor);

        return new OkResult();
    }

    private async Task<IActionResult> InvalidateAsync(HttpContext httpContext)
    {
        InvalidationDescriptor descriptor = await DeserializeBodyAsync<InvalidationDescriptor>(httpContext);

        cacheService.Invalidate(descriptor);

        return new OkResult();
    }

    private static async Task<T> DeserializeBodyAsync<T>(HttpContext httpContext)
        where T : notnull
    {
#if NET6_0_OR_GREATER
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
