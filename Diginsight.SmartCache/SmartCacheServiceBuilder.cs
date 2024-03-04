using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Diginsight.SmartCache;

public sealed class SmartCacheServiceBuilder
{
    public IServiceCollection Services { get; }

    internal SmartCacheServiceBuilder(IServiceCollection services)
    {
        services.AddMemoryCache();
        services.TryAddSingleton<ISmartCacheService, SmartCacheService>();
        services.TryAddSingleton(static sp => new Lazy<ISmartCacheService>(sp.GetRequiredService<ISmartCacheService>));
        services.TryAddSingleton<ICacheKeyService, CacheKeyService>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<SmartCacheServiceOptions>, ValidateSmartCacheServiceOptions>());

        Services = services;
    }

    private sealed class ValidateSmartCacheServiceOptions : IValidateOptions<SmartCacheServiceOptions>
    {
        public ValidateOptionsResult Validate(string? name, SmartCacheServiceOptions options)
        {
            if (name != Options.DefaultName)
            {
                return ValidateOptionsResult.Skip;
            }

            ICollection<string> messages = new List<string>();
            if (options.LowPrioritySizeThreshold < options.MidPrioritySizeThreshold)
            {
                messages.Add($"{nameof(SmartCacheServiceOptions.LowPrioritySizeThreshold)} must be greater than or equal to {nameof(SmartCacheServiceOptions.MidPrioritySizeThreshold)}");
            }

            int companionPrefetchCount = options.CompanionPrefetchCount;
            int companionMaxParallelism = options.CompanionMaxParallelism;

            if (companionPrefetchCount <= 0)
            {
                messages.Add($"{nameof(SmartCacheServiceOptions.CompanionPrefetchCount)} must be positive");
            }

            if (companionMaxParallelism <= 0)
            {
                messages.Add($"{nameof(SmartCacheServiceOptions.CompanionMaxParallelism)} must be positive");
            }

            if (companionPrefetchCount > 0 && companionMaxParallelism > 0 && companionPrefetchCount < companionMaxParallelism)
            {
                messages.Add($"{nameof(SmartCacheServiceOptions.CompanionMaxParallelism)} must be less than or equal to {nameof(SmartCacheServiceOptions.CompanionPrefetchCount)}");
            }

            return messages.Any() ? ValidateOptionsResult.Fail(messages) : ValidateOptionsResult.Success;
        }
    }
}
