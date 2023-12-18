using Microsoft.Extensions.Logging;
using System.Diagnostics.Metrics;
using System.Text;

namespace Diginsight.SmartCache;

internal sealed class ClusterCacheMiddleware : IMiddleware
{
    public const string ObservabilityName = "cluster-cache";

    private static class Metrics
    {
        private static readonly Meter Meter = new(ObservabilityName);
        public static readonly TimerHistogram FetchDuration = Meter.CreateTimer("cluster_cache_fetch_duration");
    }

    private static class MetricTags
    {
        public static readonly KeyValuePair<string, object?> Found = new("found", true);
        public static readonly KeyValuePair<string, object?> NotFound = new("found", false);
    }

    private readonly ILogger<ClusterCacheMiddleware> logger;
    private readonly ICacheService cacheService;

    public ClusterCacheMiddleware(
        ILogger<ClusterCacheMiddleware> logger,
        ICacheService cacheService)
    {
        this.logger = logger;
        this.cacheService = cacheService;
    }

    public async Task InvokeAsync(HttpContext httpContext, RequestDelegate next)
    {
        HttpRequest request = httpContext.Request;
        if (request.Method != HttpMethod.Post.Method || !request.Path.StartsWithSegments("/api/v1/ClusterCache", out PathString remPath))
        {
            await next(httpContext);
            return;
        }

        IActionResult actionResult;
        switch (remPath.Value?.ToLowerInvariant())
        {
            case "/get":
                actionResult = await GetAsync(httpContext);
                break;

            case "/cachemiss":
                actionResult = await CacheMissAsync(httpContext);
                break;

            case "/invalidate":
                actionResult = await InvalidateAsync(httpContext);
                break;

            default:
                await next(httpContext);
                return;
        }

        ActionContext actionContext = new(httpContext, new RouteData(request.RouteValues), new ActionDescriptor());
        await actionResult.ExecuteResultAsync(actionContext);
    }

    private async Task<IActionResult> GetAsync(HttpContext httpContext)
    {
        byte[] rawValue;
        using (var mark = Metrics.FetchDuration.StartMark())
        {
            ICacheKey key = await DeserializeBodyAsync<ICacheKey>(httpContext);
            if (!cacheService.TryGetDirectFromMemory(key, out Type? type, out object? value))
            {
                mark.AddTags(MetricTags.NotFound);
                return new NotFoundResult();
            }

            mark.AddTags(MetricTags.Found);
            rawValue = CacheSerialization.SerializeToBytes(value, type);
        }

        Encoding encoding = CacheSerialization.Encoding;

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
        await using FileBufferingReadStream stream = new(httpContext.Request.Body, 100 * 1024);
        await stream.DrainAsync(CancellationToken.None);
        stream.Position = 0;

        return CacheSerialization.Deserialize<T>(stream);
    }
}
