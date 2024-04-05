namespace Diginsight.SmartCache.Externalization.Kubernetes;

public interface ISmartCacheKubernetesOptions
{
    bool UseHttps { get; }
    string CompanionsDnsName { get; }
    string PodIpEnvVariableName { get; }
    TimeSpan CompanionRequestTimeout { get; }
}
