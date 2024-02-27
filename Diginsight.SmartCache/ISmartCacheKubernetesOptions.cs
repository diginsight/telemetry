namespace Diginsight.SmartCache;

public interface ISmartCacheKubernetesOptions
{
    bool UseHttps { get; }
    TimeSpan CompanionRequestTimeout { get; }
    string CompanionsDnsName { get; }
    string PodIpEnvVariableName { get; }
}
