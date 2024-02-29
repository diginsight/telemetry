using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Diginsight.SmartCache.Externalization.ServiceBus;

public sealed class ServiceBusCacheCompanionProviderInstaller : ICacheCompanionProviderInstaller
{
    public void Install(IServiceCollection services, out Action uninstall)
    {
        throw new NotImplementedException();
    }

    private sealed class ValidateSmartCacheServiceBusOptions : IValidateOptions<SmartCacheServiceBusOptions>
    {
        public ValidateOptionsResult Validate(string? name, SmartCacheServiceBusOptions options)
        {
            if (name != Options.DefaultName)
            {
                return ValidateOptionsResult.Skip;
            }

            ICollection<string> failureMessages = new List<string>();
            if (string.IsNullOrEmpty(options.ConnectionString))
            {
                failureMessages.Add($"{nameof(SmartCacheServiceBusOptions.ConnectionString)} must be non-empty");
            }
            if (string.IsNullOrEmpty(options.TopicName))
            {
                failureMessages.Add($"{nameof(SmartCacheServiceBusOptions.TopicName)} must be non-empty");
            }
            if (string.IsNullOrEmpty(options.SubscriptionName))
            {
                failureMessages.Add($"{nameof(SmartCacheServiceBusOptions.SubscriptionName)} must be non-empty");
            }

            return failureMessages.Count > 0 ? ValidateOptionsResult.Fail(failureMessages) : ValidateOptionsResult.Success;
        }
    }
}
