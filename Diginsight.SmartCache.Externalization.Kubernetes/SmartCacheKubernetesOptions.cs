namespace Diginsight.SmartCache.Externalization.Kubernetes;

public sealed class SmartCacheKubernetesOptions : ISmartCacheKubernetesOptions
{
    public string? PodsDnsName { get; set; }

    string ISmartCacheKubernetesOptions.PodsDnsName =>
        PodsDnsName ?? throw new InvalidOperationException($"{nameof(PodsDnsName)} is null");

    public string? PodIpEnvVariableName { get; set; }

    string ISmartCacheKubernetesOptions.PodIpEnvVariableName =>
        PodIpEnvVariableName ?? throw new InvalidOperationException($"{nameof(PodIpEnvVariableName)} is not null");

    public TimeSpan PodRequestTimeout { get; set; } = TimeSpan.FromSeconds(5);
}
