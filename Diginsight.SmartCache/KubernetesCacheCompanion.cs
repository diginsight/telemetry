using Microsoft.Extensions.Logging;

namespace Diginsight.SmartCache;

public class KubernetesCacheCompanion : CacheCompanion
{
    private readonly ILogger<KubernetesCacheCompanion> logger;
    private readonly IHttpClientFactory httpClientFactory;

    public KubernetesCacheCompanion(
        string podIp,
        ILogger<KubernetesCacheCompanion> logger,
        IHttpClientFactory httpClientFactory
    )
        : base(podIp)
    {
        this.logger = logger;
        this.httpClientFactory = httpClientFactory;
    }

    public override async Task<CacheLocationOutput<TValue>?> GetAsync<TValue>(
        CacheKeyHolder keyHolder, DateTime minimumCreationDate, Action markInvalid, CancellationToken cancellationToken
    )
    {
        throw new NotImplementedException();
    }

    protected override Task PublishCacheMissAndForgetAsync(CachePayloadHolder<CacheMissDescriptor> descriptorHolder)
    {
        // TODO Use SmartCacheMiddlewareOptions.CacheMissPathSegment
        return PublishAndForgetAsync(descriptorHolder, "cacheMiss");
    }

    protected override Task PublishInvalidationAndForgetAsync(CachePayloadHolder<InvalidationDescriptor> descriptorHolder)
    {
        // TODO Use SmartCacheMiddlewareOptions.InvalidatePathSegment
        return PublishAndForgetAsync(descriptorHolder, "invalidate");
    }

    private async Task PublishAndForgetAsync<T>(CachePayloadHolder<T> descriptorHolder, string uriSuffix)
        where T : notnull
    {
        // TODO Use SmartCacheMiddlewareOptions.RootPath
        HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, $"http://{Id}/api/v1/clusterCache/{uriSuffix}")
        {
            Content = new StringContent(descriptorHolder.GetAsString(), SmartCacheSerialization.Encoding),
        };
        using HttpResponseMessage responseMessage = await MakeHttpClient().SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
    }

    private HttpClient MakeHttpClient() => httpClientFactory.CreateClient(nameof(KubernetesCacheCompanion));
}
