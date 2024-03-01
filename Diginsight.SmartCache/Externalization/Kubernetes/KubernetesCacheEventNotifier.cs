using Diginsight.SmartCache.Externalization.Middleware;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;

namespace Diginsight.SmartCache.Externalization.Kubernetes;

internal sealed class KubernetesCacheEventNotifier : CacheEventNotifier
{
    private readonly string podIp;
    private readonly KubernetesCacheCompanionHelper helper;
    private readonly ISmartCacheMiddlewareOptions middlewareOptions;

    public KubernetesCacheEventNotifier(
        string podIp,
        KubernetesCacheCompanionHelper helper,
        IOptions<SmartCacheMiddlewareOptions> middlewareOptions
    )
    {
        this.podIp = podIp;
        this.helper = helper;
        this.middlewareOptions = middlewareOptions.Value;
    }

    protected override Task NotifyCacheMissAsync(CachePayloadHolder<CacheMissDescriptor> descriptorHolder)
    {
        return NotifyAsync(descriptorHolder, middlewareOptions.CacheMissPathSegment);
    }

    protected override Task NotifyInvalidationAsync(CachePayloadHolder<InvalidationDescriptor> descriptorHolder)
    {
        return NotifyAsync(descriptorHolder, middlewareOptions.InvalidatePathSegment);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private async Task NotifyAsync<T>(CachePayloadHolder<T> descriptorHolder, string pathSegment)
        where T : notnull
    {
        HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, helper.MakeRequestUri(podIp, pathSegment))
        {
            Content = new StringContent(descriptorHolder.GetAsString(), SmartCacheSerialization.Encoding),
        };
        using HttpResponseMessage responseMessage = await helper.MakeHttpClient().SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
    }
}
