using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Diginsight.SmartCache;

public sealed class SmartCacheBuilder
{
    public IServiceCollection Services { get; }

    internal SmartCacheBuilder(IServiceCollection services)
    {
        services.AddMemoryCache();
        services.TryAddSingleton<ISmartCache, SmartCache>();
        services.TryAddSingleton(static sp => new Lazy<ISmartCache>(sp.GetRequiredService<ISmartCache>));
        services.TryAddSingleton<ICacheKeyService, CacheKeyService>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<SmartCacheCoreOptions>, ValidateSmartCacheCoreOptions>());

        Services = services;
    }

    private sealed class ValidateSmartCacheCoreOptions : IValidateOptions<SmartCacheCoreOptions>
    {
        public ValidateOptionsResult Validate(string? name, SmartCacheCoreOptions options)
        {
            if (name != Options.DefaultName)
            {
                return ValidateOptionsResult.Skip;
            }

            ICollection<string> messages = new List<string>();
            if (options.LowPrioritySizeThreshold < options.MidPrioritySizeThreshold)
            {
                messages.Add($"{nameof(SmartCacheCoreOptions.LowPrioritySizeThreshold)} must be greater than or equal to {nameof(SmartCacheCoreOptions.MidPrioritySizeThreshold)}");
            }

            int companionPrefetchCount = options.CompanionPrefetchCount;
            int companionMaxParallelism = options.CompanionMaxParallelism;

            if (companionPrefetchCount <= 0)
            {
                messages.Add($"{nameof(SmartCacheCoreOptions.CompanionPrefetchCount)} must be positive");
            }

            if (companionMaxParallelism <= 0)
            {
                messages.Add($"{nameof(SmartCacheCoreOptions.CompanionMaxParallelism)} must be positive");
            }

            if (companionPrefetchCount > 0 && companionMaxParallelism > 0 && companionPrefetchCount < companionMaxParallelism)
            {
                messages.Add($"{nameof(SmartCacheCoreOptions.CompanionMaxParallelism)} must be less than or equal to {nameof(SmartCacheCoreOptions.CompanionPrefetchCount)}");
            }

            return messages.Any() ? ValidateOptionsResult.Fail(messages) : ValidateOptionsResult.Success;
        }
    }
}
