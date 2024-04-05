using Diginsight.SmartCache.Externalization.Middleware;
using System.Runtime.CompilerServices;

namespace Diginsight.SmartCache.Externalization.Kubernetes;

internal sealed class KubernetesCacheCompanionHelper
{
    private readonly IHttpClientFactory httpClientFactory;
    private readonly ISmartCacheKubernetesOptions kubernetesOptions;
    private readonly ISmartCacheMiddlewareOptions middlewareOptions;

    public KubernetesCacheCompanionHelper(
        IHttpClientFactory httpClientFactory,
        ISmartCacheKubernetesOptions kubernetesOptions,
        ISmartCacheMiddlewareOptions middlewareOptions
    )
    {
        this.httpClientFactory = httpClientFactory;
        this.kubernetesOptions = kubernetesOptions;
        this.middlewareOptions = middlewareOptions;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HttpClient MakeHttpClient() => httpClientFactory.CreateClient(nameof(KubernetesCacheCompanion));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string MakeRequestUri(string podIp, string pathSegment)
    {
        return $"{(kubernetesOptions.UseHttps ? "https" : "http")}://{podIp}{middlewareOptions.RootPath}{pathSegment}";
    }
}
