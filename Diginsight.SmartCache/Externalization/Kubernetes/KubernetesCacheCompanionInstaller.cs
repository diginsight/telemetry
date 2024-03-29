﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Diginsight.SmartCache.Externalization.Kubernetes;

public sealed class KubernetesCacheCompanionInstaller : ICacheCompanionInstaller
{
    public static readonly ICacheCompanionInstaller Instance = new KubernetesCacheCompanionInstaller();

    private KubernetesCacheCompanionInstaller() { }

    public void Install(IServiceCollection services, out Action uninstall)
    {
        services
            .AddHttpClient(nameof(KubernetesCacheCompanion))
            .ConfigureHttpClient(
                static (sp, client) =>
                {
                    ISmartCacheKubernetesOptions options = sp.GetRequiredService<IOptions<SmartCacheKubernetesOptions>>().Value;
                    client.Timeout = options.CompanionRequestTimeout;
                }
            );

        ServiceDescriptor sd0 = ServiceDescriptor.Singleton<ICacheCompanion, KubernetesCacheCompanion>();
        services.TryAdd(sd0);

        ServiceDescriptor sd1 = ServiceDescriptor.Singleton<KubernetesCacheCompanionHelper, KubernetesCacheCompanionHelper>();
        services.TryAdd(sd1);

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<SmartCacheKubernetesOptions>, ValidateSmartCacheKubernetesOptions>());

        uninstall = Uninstall;

        void Uninstall()
        {
            services.Remove(sd0);
            services.Remove(sd1);
        }
    }

    private sealed class ValidateSmartCacheKubernetesOptions : IValidateOptions<SmartCacheKubernetesOptions>
    {
        public ValidateOptionsResult Validate(string? name, SmartCacheKubernetesOptions options)
        {
            if (name != Options.DefaultName)
            {
                return ValidateOptionsResult.Skip;
            }

            ICollection<string> failureMessages = new List<string>();
            if (string.IsNullOrEmpty(options.CompanionsDnsName))
            {
                failureMessages.Add($"{nameof(SmartCacheKubernetesOptions.CompanionsDnsName)} must be non-empty");
            }
            if (string.IsNullOrEmpty(options.PodIpEnvVariableName))
            {
                failureMessages.Add($"{nameof(SmartCacheKubernetesOptions.PodIpEnvVariableName)} must be non-empty");
            }
            if (options.CompanionRequestTimeout < TimeSpan.FromSeconds(1))
            {
                failureMessages.Add($"{nameof(SmartCacheKubernetesOptions.CompanionRequestTimeout)} must be at least 1 second");
            }

            return failureMessages.Count > 0 ? ValidateOptionsResult.Fail(failureMessages) : ValidateOptionsResult.Success;
        }
    }
}
