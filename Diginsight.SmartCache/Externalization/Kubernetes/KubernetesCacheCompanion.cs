using Diginsight.SmartCache.Externalization.Redis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Sockets;

namespace Diginsight.SmartCache.Externalization.Kubernetes;

internal sealed class KubernetesCacheCompanion : ICacheCompanion
{
    private readonly IServiceProvider serviceProvider;
    private readonly ISmartCacheKubernetesOptions smartCacheKubernetesOptions;

    private readonly ObjectFactory<KubernetesCacheLocation> makeLocation =
        ActivatorUtilities.CreateFactory<KubernetesCacheLocation>([typeof(string)]);

    private readonly ObjectFactory<KubernetesCacheEventNotifier> makeEventNotifier =
        ActivatorUtilities.CreateFactory<KubernetesCacheEventNotifier>([typeof(string)]);

    public string SelfLocationId { get; }

    public IEnumerable<PassiveCacheLocation> PassiveLocations { get; }

    public KubernetesCacheCompanion(
        IServiceProvider serviceProvider,
        IOptions<SmartCacheKubernetesOptions> smartCacheKubernetesOptionsOptions,
        RedisCacheLocation? redisLocation = null
    )
    {
        this.serviceProvider = serviceProvider;
        smartCacheKubernetesOptions = smartCacheKubernetesOptionsOptions.Value;

        SelfLocationId = Environment.GetEnvironmentVariable(smartCacheKubernetesOptions.PodIpEnvVariableName) ?? "";

        PassiveLocations = redisLocation is null
            ? Enumerable.Empty<PassiveCacheLocation>()
            : new[] { redisLocation };
    }

    public async Task<IEnumerable<ActiveCacheLocation>> GetActiveLocationsAsync(IEnumerable<string> locationIds)
    {
        return (await GetAllPodIpsAsync())
            .Intersect(locationIds)
            .Select(x => makeLocation(serviceProvider, [x]))
            .ToArray();
    }

    public async Task<IEnumerable<CacheEventNotifier>> GetAllEventNotifiersAsync()
    {
        return (await GetAllPodIpsAsync())
            .Select(x => makeEventNotifier(serviceProvider, [x]))
            .ToArray();
    }

    private async Task<IEnumerable<string>> GetAllPodIpsAsync()
    {
#if NET6_0_OR_GREATER
        return (await Dns.GetHostAddressesAsync(smartCacheKubernetesOptions.CompanionsDnsName, AddressFamily.InterNetwork))
#else
        return (await Dns.GetHostAddressesAsync(smartCacheKubernetesOptions.CompanionsDnsName))
            .Where(static x => x.AddressFamily == AddressFamily.InterNetwork)
#endif
            .Select(static x => x.ToString())
            .Where(ip => ip != SelfLocationId);
    }
}
