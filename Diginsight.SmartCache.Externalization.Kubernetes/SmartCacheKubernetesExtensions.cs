using Diginsight.SmartCache.Externalization.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Diginsight.SmartCache.Externalization.Kubernetes;

public static class SmartCacheKubernetesExtensions
{
    public static SmartCacheBuilder SetKubernetesCompanion(
        this SmartCacheBuilder builder,
        Action<SmartCacheKubernetesOptions>? configureKubernetesOptions = null,
        Action<SmartCacheHttpOptions>? configureHttpOptions = null
    )
    {
        builder
            .AddHttp(configureHttpOptions)
            .SetCompanion(KubernetesCacheCompanionInstaller.Instance);

        if (configureKubernetesOptions is not null)
        {
            builder.Services.Configure(configureKubernetesOptions);
        }

        return builder;
    }
}
