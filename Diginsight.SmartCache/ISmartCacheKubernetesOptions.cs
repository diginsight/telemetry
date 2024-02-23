namespace Diginsight.SmartCache;

public interface ISmartCacheKubernetesOptions
{
    TimeSpan CompanionRequestTimeout { get; }
    string? CompanionsDnsName { get; }
    string PodIpEnvVariableName { get; }
}
