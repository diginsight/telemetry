using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Diginsight.SmartCache;

public sealed class KubernetesCacheCompanionProviderInstaller : ICacheCompanionProviderInstaller
{
    public void Install(IServiceCollection services, out Action uninstall)
    {
        ServiceDescriptor sd0 = ServiceDescriptor.Singleton<ICacheCompanionProvider, KubernetesCacheCompanionProvider>();
        services.TryAdd(sd0);

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<SmartCacheKubernetesOptions>, ValidateSmartCacheKubernetesOptions>());
        services.AddMiddlewareOptions();

        uninstall = Uninstall;

        void Uninstall()
        {
            services.Remove(sd0);
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

            return failureMessages.Count > 0 ? ValidateOptionsResult.Fail(failureMessages) : ValidateOptionsResult.Success;
        }
    }
}
