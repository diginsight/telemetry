namespace Diginsight.SmartCache.Externalization.Kubernetes;

public interface ISmartCacheKubernetesOptions
{
    string PodsDnsName { get; }
    string PodIpEnvVariableName { get; }
    TimeSpan PodRequestTimeout { get; }
}
