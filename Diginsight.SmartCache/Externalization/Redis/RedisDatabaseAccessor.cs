using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Diginsight.SmartCache.Externalization.Redis;

internal sealed class RedisDatabaseAccessor : IRedisDatabaseAccessor
{
    private readonly string? redisConfiguration;
    private IConnectionMultiplexer? connectionMultiplexer;

    public RedisDatabaseAccessor(IOptions<SmartCacheRedisOptions> smartCacheRedisOptions)
    {
        ISmartCacheRedisOptions options = smartCacheRedisOptions.Value;
        redisConfiguration = options.Configuration;
    }

    public IDatabase? Database
    {
        get
        {
            if (redisConfiguration is null)
            {
                return null;
            }

            if (connectionMultiplexer is not null)
            {
                return connectionMultiplexer.GetDatabase();
            }

            lock (this)
            {
                if (connectionMultiplexer is not null)
                {
                    return connectionMultiplexer.GetDatabase();
                }

                try
                {
                    connectionMultiplexer = ConnectionMultiplexer.Connect(redisConfiguration);
                }
                catch (RedisConnectionException) { }

                return connectionMultiplexer?.GetDatabase();
            }
        }
    }
}
