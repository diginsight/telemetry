using Diginsight.SmartCache.Externalization.Http;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Sockets;

namespace Diginsight.SmartCache.Externalization.Kubernetes;

internal sealed class KubernetesCacheCompanion : HttpCacheCompanion
{
    private readonly ISmartCacheKubernetesOptions smartCacheKubernetesOptions;

    public override string SelfLocationId { get; }

    public override IEnumerable<PassiveCacheLocation> PassiveLocations { get; }

    public KubernetesCacheCompanion(
        IServiceProvider serviceProvider,
        IHttpClientFactory httpClientFactory,
        IOptions<SmartCacheKubernetesOptions> smartCacheKubernetesOptions,
        IEnumerable<PassiveCacheLocation> passiveLocations
    )
        : base(serviceProvider, () => httpClientFactory.CreateClient(nameof(KubernetesCacheCompanion)))
    {
        this.smartCacheKubernetesOptions = smartCacheKubernetesOptions.Value;
        PassiveLocations = passiveLocations;

        SelfLocationId = Environment.GetEnvironmentVariable(this.smartCacheKubernetesOptions.PodIpEnvVariableName) ?? "";
    }

    public override async Task<IEnumerable<ActiveCacheLocation>> GetActiveLocationsAsync(IEnumerable<string> locationIds)
    {
        return (await GetAllPodIpsAsync()).Intersect(locationIds).Select(MakeLocation).ToArray();
    }

    public override async Task<IEnumerable<CacheEventNotifier>> GetAllEventNotifiersAsync()
    {
        return (await GetAllPodIpsAsync()).Select(MakeEventNotifier).ToArray();
    }

    private async Task<IEnumerable<string>> GetAllPodIpsAsync()
    {
#if NET
        return (await Dns.GetHostAddressesAsync(smartCacheKubernetesOptions.PodsDnsName, AddressFamily.InterNetwork))
#else
        return (await Dns.GetHostAddressesAsync(smartCacheKubernetesOptions.PodsDnsName))
            .Where(static x => x.AddressFamily == AddressFamily.InterNetwork)
#endif
            .Select(static x => x.ToString())
            .Where(ip => ip != SelfLocationId);
    }
}
