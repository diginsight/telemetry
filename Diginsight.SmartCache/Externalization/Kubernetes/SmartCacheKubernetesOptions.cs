namespace Diginsight.SmartCache.Externalization.Kubernetes;

public sealed class SmartCacheKubernetesOptions : ISmartCacheKubernetesOptions
{
    public bool UseHttps { get; set; }

    public TimeSpan CompanionRequestTimeout { get; set; } = TimeSpan.FromSeconds(5);

    public string? CompanionsDnsName { get; set; }

    string ISmartCacheKubernetesOptions.CompanionsDnsName =>
        CompanionsDnsName ?? throw new InvalidOperationException($"{nameof(CompanionsDnsName)} is null");

    public string? PodIpEnvVariableName { get; set; }

    string ISmartCacheKubernetesOptions.PodIpEnvVariableName =>
        PodIpEnvVariableName ?? throw new InvalidOperationException($"{nameof(PodIpEnvVariableName)} is not null");
}
