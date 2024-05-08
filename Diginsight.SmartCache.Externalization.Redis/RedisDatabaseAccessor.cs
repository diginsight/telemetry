using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Diginsight.SmartCache.Externalization.Redis;

internal sealed class RedisDatabaseAccessor : IRedisDatabaseAccessor, IDisposable
{
    private readonly ILogger logger;
    private readonly string? redisConfiguration;

    private readonly object lockObj = new ();
    private IConnectionMultiplexer? connectionMultiplexer;

    public RedisDatabaseAccessor(
        ILogger<RedisDatabaseAccessor> logger,
        IOptions<SmartCacheRedisOptions> smartCacheRedisOptions
    )
    {
        this.logger = logger;
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

            logger.LogDebug("Accessing Redis database");

            IConnectionMultiplexer? cm;
            if ((cm = connectionMultiplexer) is not null)
            {
                return cm.GetDatabase();
            }

            lock (lockObj)
            {
                if ((cm = connectionMultiplexer) is not null)
                {
                    return cm.GetDatabase();
                }

                logger.LogDebug("Connecting to Redis");

                try
                {
                    connectionMultiplexer = cm = ConnectionMultiplexer.Connect(redisConfiguration, static o => { o.AbortOnConnectFail = true; });
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Failed to connect to Redis");
                }

                return cm?.GetDatabase();
            }
        }
    }

    void IDisposable.Dispose()
    {
        lock (lockObj)
        {
            if (connectionMultiplexer is { } cm)
            {
                cm.Dispose();
                connectionMultiplexer = null;
            }
        }
    }
}
