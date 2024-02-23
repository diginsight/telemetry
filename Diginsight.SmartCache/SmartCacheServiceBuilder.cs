using Microsoft.Extensions.Caching.Memory;
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
        services.TryAddSingleton<ICacheKeyService, CacheKeyService>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<SmartCacheServiceOptions>, ValidateSmartCacheServiceOptions>());

        Services = services;
    }

    public SmartCacheServiceBuilder SetCompanionProvider<T>()
        where T : class, ICacheCompanionProvider
    {
        Services.RemoveAll<ICacheCompanionProvider>();
        Services.AddSingleton<ICacheCompanionProvider, T>();

        return this;
    }

    public SmartCacheServiceBuilder SetCompanionProvider(Func<IServiceProvider, ICacheCompanionProvider> implementationFactory)
    {
        Services.RemoveAll<ICacheCompanionProvider>();
        Services.AddSingleton(implementationFactory);

        return this;
    }

    public SmartCacheServiceBuilder SetSizeLimit(long? sizeLimit)
    {
        Services.Configure<MemoryCacheOptions>(nameof(SmartCacheService), x => { x.SizeLimit = sizeLimit; });

        return this;
    }

    public SmartCacheServiceBuilder AddRedis()
    {
        Services.TryAddSingleton<IRedisDatabaseAccessor, RedisDatabaseAccessor>();
        Services.TryAddSingleton<RedisCacheLocation>();
        Services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<SmartCacheRedisOptions>, ValidateSmartCacheRedisOptions>());

        return this;
    }

    public SmartCacheServiceBuilder AddMiddleware()
    {
        Services.TryAddTransient<SmartCacheMiddleware>();
        Services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<SmartCacheMiddlewareOptions>, ValidateSmartCacheMiddlewareOptions>());

        return this;
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
            if (options.LowPrioritySizeThreshold > options.MidPrioritySizeThreshold)
            {
                messages.Add($"{nameof(SmartCacheServiceOptions.LowPrioritySizeThreshold)} must be less than or equal to {nameof(SmartCacheServiceOptions.MidPrioritySizeThreshold)}");
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

    private sealed class ValidateSmartCacheRedisOptions : IValidateOptions<SmartCacheRedisOptions>
    {
        public ValidateOptionsResult Validate(string? name, SmartCacheRedisOptions options)
        {
            if (name != Options.DefaultName)
            {
                return ValidateOptionsResult.Skip;
            }

            if (options.Configuration is not null && string.IsNullOrEmpty(options.KeyPrefix))
            {
                return ValidateOptionsResult.Fail($"{nameof(SmartCacheRedisOptions.KeyPrefix)} must be non-empty");
            }

            return ValidateOptionsResult.Success;
        }
    }

    private sealed class ValidateSmartCacheMiddlewareOptions : IValidateOptions<SmartCacheMiddlewareOptions>
    {
        public ValidateOptionsResult Validate(string? name, SmartCacheMiddlewareOptions options)
        {
            if (name != Options.DefaultName)
            {
                return ValidateOptionsResult.Skip;
            }

            ICollection<string> failureMessages = new List<string>();
            if (options.RootPath?[0] != '/')
            {
                failureMessages.Add($"{nameof(SmartCacheMiddlewareOptions.RootPath)} must be not null and start with '/'");
            }
            if (options.GetPathSegment is { } getPathSegment && getPathSegment[0] != '/')
            {
                failureMessages.Add($"{nameof(SmartCacheMiddlewareOptions.GetPathSegment)} must start with '/'");
            }
            if (options.CacheMissPathSegment is { } cacheMissPathSegment && cacheMissPathSegment[0] != '/')
            {
                failureMessages.Add($"{nameof(SmartCacheMiddlewareOptions.CacheMissPathSegment)} must start with '/'");
            }
            if (options.InvalidatePathSegment is { } invalidatePathSegment && invalidatePathSegment[0] != '/')
            {
                failureMessages.Add($"{nameof(SmartCacheMiddlewareOptions.InvalidatePathSegment)} must start with '/'");
            }

            return failureMessages.Count > 0 ? ValidateOptionsResult.Fail(failureMessages) : ValidateOptionsResult.Success;
        }
    }
}
