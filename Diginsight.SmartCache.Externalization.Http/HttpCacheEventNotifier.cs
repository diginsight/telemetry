using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;

namespace Diginsight.SmartCache.Externalization.Http;

internal sealed class HttpCacheEventNotifier : CacheEventNotifier
{
    private readonly string host;
    private readonly Func<HttpClient> createHttpClient;
    private readonly ISmartCacheHttpOptions httpOptions;

    public HttpCacheEventNotifier(
        string host,
        Func<HttpClient> createHttpClient,
        IOptions<SmartCacheHttpOptions> httpOptions
    )
    {
        this.host = host;
        this.createHttpClient = createHttpClient;
        this.httpOptions = httpOptions.Value;
    }

    protected override Task NotifyCacheMissAsync(CachePayloadHolder<CacheMissDescriptor> descriptorHolder)
    {
        return NotifyAsync(descriptorHolder, httpOptions.CacheMissPathSegment);
    }

    protected override Task NotifyInvalidationAsync(CachePayloadHolder<InvalidationDescriptor> descriptorHolder)
    {
        return NotifyAsync(descriptorHolder, httpOptions.InvalidatePathSegment);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private async Task NotifyAsync(ICachePayloadHolder descriptorHolder, string pathSegment)
    {
        using (await HttpCacheCompanionHelper.SendAsync(createHttpClient(), httpOptions, host, pathSegment, descriptorHolder, false)) { }
    }
}
