using Microsoft.Extensions.DependencyInjection;

namespace Diginsight.SmartCache.Externalization.ServiceBus;

public static class SmartCacheServiceBusExtensions
{
    public static SmartCacheBuilder SetServiceBusCompanion(
        this SmartCacheBuilder builder, Action<SmartCacheServiceBusOptions>? configureOptions = null
    )
    {
        builder.SetCompanion(ServiceBusCacheCompanionInstaller.Instance);

        if (configureOptions is not null)
        {
            builder.Services.Configure(configureOptions);
        }

        return builder;
    }
}
