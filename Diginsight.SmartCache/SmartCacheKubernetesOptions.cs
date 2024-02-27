namespace Diginsight.SmartCache;

public sealed class SmartCacheKubernetesOptions : ISmartCacheKubernetesOptions
{
    public bool UseHttps { get; set; }

    // TODO Validate
    public TimeSpan CompanionRequestTimeout { get; set; }

    public string? CompanionsDnsName { get; set; }

    string ISmartCacheKubernetesOptions.CompanionsDnsName =>
        CompanionsDnsName ?? throw new InvalidOperationException($"{nameof(CompanionsDnsName)} is null");

    public string? PodIpEnvVariableName { get; set; }

    string ISmartCacheKubernetesOptions.PodIpEnvVariableName =>
        PodIpEnvVariableName ?? throw new InvalidOperationException($"{nameof(PodIpEnvVariableName)} is not null");
}
