using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Sockets;

namespace Diginsight.SmartCache;

internal sealed class KubernetesCacheCompanionProvider : ICacheCompanionProvider
{
    private readonly IServiceProvider serviceProvider;
    private readonly ISmartCacheKubernetesOptions smartCacheKubernetesOptions;

    public string SelfLocationId { get; }

    public IEnumerable<PassiveCacheLocation> PassiveLocations { get; }

    public KubernetesCacheCompanionProvider(
        IServiceProvider serviceProvider,
        IOptions<SmartCacheKubernetesOptions> smartCacheKubernetesOptionsOptions,
        RedisCacheLocation? redisLocation = null
    )
    {
        this.serviceProvider = serviceProvider;
        smartCacheKubernetesOptions = smartCacheKubernetesOptionsOptions.Value;

        SelfLocationId = Environment.GetEnvironmentVariable(smartCacheKubernetesOptions.PodIpEnvVariableName!) ?? "";

        PassiveLocations = redisLocation is null
            ? Enumerable.Empty<PassiveCacheLocation>()
            : new[] { redisLocation };
    }

    public async Task<IEnumerable<CacheCompanion>> GetCompanionsAsync()
    {
#if NET6_0_OR_GREATER
        return (await Dns.GetHostAddressesAsync(smartCacheKubernetesOptions.CompanionsDnsName!, AddressFamily.InterNetwork))
#else
        return (await Dns.GetHostAddressesAsync(smartCacheKubernetesOptions.CompanionsDnsName!))
            .Where(static x => x.AddressFamily == AddressFamily.InterNetwork)
#endif
            .Select(static x => x.ToString())
            .Where(ip => ip != SelfLocationId)
            .Select(x => ActivatorUtilities.CreateInstance<KubernetesCacheCompanion>(serviceProvider, x))
            .ToArray();
    }
}
