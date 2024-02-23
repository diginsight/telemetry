namespace Diginsight.SmartCache;

// TODO Validate
public sealed class SmartCacheKubernetesOptions : ISmartCacheKubernetesOptions
{
    public TimeSpan CompanionRequestTimeout { get; set; }
    public string? CompanionsDnsName { get; set; }
    public string PodIpEnvVariableName { get; set; }
}
