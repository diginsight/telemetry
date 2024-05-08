using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System.ComponentModel;

namespace Diginsight.SmartCache.Externalization.Redis;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class SmartCacheRedisExtensions
{
    public static SmartCacheBuilder AddRedis(
        this SmartCacheBuilder builder, Action<SmartCacheRedisOptions>? configureOptions = null
    )
    {
        builder.Services.TryAddSingleton<IRedisDatabaseAccessor, RedisDatabaseAccessor>();
        builder.Services.TryAddSingleton<PassiveCacheLocation, RedisCacheLocation>();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<SmartCacheRedisOptions>, ValidateSmartCacheRedisOptions>());

        if (configureOptions is not null)
        {
            builder.Services.Configure(configureOptions);
        }

        return builder;
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
}
