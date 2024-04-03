using Diginsight.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Diagnostics;

namespace Diginsight.SmartCache.Externalization.Redis;

public sealed class RedisCacheLocation : PassiveCacheLocation
{
    private readonly ILogger<RedisCacheLocation> logger;
    private readonly IRedisDatabaseAccessor redisDatabaseAccessor;
    private readonly ISmartCacheRedisOptions smartCacheRedisOptions;

    public override KeyValuePair<string, object?> MetricTag => SmartCacheObservability.Tags.Type.Redis;

    public RedisCacheLocation(
        ILogger<RedisCacheLocation> logger,
        IRedisDatabaseAccessor redisDatabaseAccessor,
        IOptions<SmartCacheRedisOptions> smartCacheRedisOptions,
        string? id = null
    )
        : base(id ?? "<redis>")
    {
        this.logger = logger;
        this.redisDatabaseAccessor = redisDatabaseAccessor;
        this.smartCacheRedisOptions = smartCacheRedisOptions.Value;
    }

    public override async Task<CacheLocationOutput<TValue>?> GetAsync<TValue>(
        CacheKeyHolder keyHolder, DateTime minimumCreationDate, Action markInvalid, CancellationToken cancellationToken
    )
    {
        using Activity? activity = SmartCacheObservability.ActivitySource.StartMethodActivity(logger, new { key = keyHolder.Key, minimumCreationDate });

        if (redisDatabaseAccessor.Database is not { } redisDatabase)
        {
            return null;
        }

        RedisKey redisKey = smartCacheRedisOptions.KeyPrefix + keyHolder.GetAsString();

        RedisValue redisEntry;
        using TimerLap lap = SmartCacheObservability.Instruments.FetchDuration.CreateLap(SmartCacheObservability.Tags.Type.Redis);

        using (lap.Start())
        {
            redisEntry = await redisDatabase.StringGetAsync(redisKey);
        }

        if (redisEntry.IsNull)
        {
            lap.AddTags(SmartCacheObservability.Tags.Found.False);
            return null;
        }

        lap.AddTags(SmartCacheObservability.Tags.Found.True);

        ValueEntry<TValue> entry;
        using (SmartCacheObservability.Instruments.SerializationDuration.StartLap(SmartCacheObservability.Tags.Operation.Deserialization, SmartCacheObservability.Tags.Subject.Value))
        {
            entry = SmartCacheSerialization.Deserialize<ValueEntry<TValue>>((byte[])redisEntry!);
        }

        double latencyMsecD = lap.ElapsedMilliseconds;
        long latencyMsecL = (long)latencyMsecD;

        if (entry.CreationDate < minimumCreationDate)
        {
            logger.LogDebug("Partial cache miss (latency: {LatencyMsec}): value found in Redis but creation date is invalid", latencyMsecL);

            lap.AddTags(SmartCacheObservability.Tags.Found.False);
            markInvalid();
            _ = await redisDatabase.KeyDeleteAsync(redisKey);
            return null;
        }

        long valueSerializedSize = redisEntry.Length();
        logger.LogDebug(
            "Cache hit (latency: {LatencyMsec}, size: {ValueSerializedSize:#,##0}): returning up-to-date value from Redis",
            latencyMsecL,
            valueSerializedSize
        );

        return new CacheLocationOutput<TValue>(entry.Data, valueSerializedSize, latencyMsecD);
    }

    protected override async Task WriteAsync(CacheKeyHolder keyHolder, IValueEntry entry, Expiration expiration, Func<Task> notifyMissAsync)
    {
        using Activity? activity = SmartCacheObservability.ActivitySource.StartMethodActivity(logger, new { key = keyHolder.Key, expiration });

        if (redisDatabaseAccessor.Database is not { } redisDatabase)
        {
            return;
        }

        Stopwatch sw = Stopwatch.StartNew();

        RedisKey redisKey = keyHolder.GetAsBytes();

        byte[] rawEntry;
        using (SmartCacheObservability.Instruments.SerializationDuration.StartLap(SmartCacheObservability.Tags.Operation.Serialization, SmartCacheObservability.Tags.Subject.Value))
        {
            rawEntry = SmartCacheSerialization.SerializeToBytes(entry);
        }

        await redisDatabase.StringSetAsync(
            redisKey.Prepend(smartCacheRedisOptions.KeyPrefix),
            rawEntry,
            expiry: expiration.IsNever ? null : expiration.Value
        );

        sw.Stop();

        logger.LogDebug("redisDatabase.StringSet completed ({ElapsedMsec} ms, {EntryLength} bytes)", sw.ElapsedMilliseconds, rawEntry.LongLength);

        await notifyMissAsync();
    }

    protected override async Task DeleteAsync(CacheKeyHolder keyHolder)
    {
        if (redisDatabaseAccessor.Database is not { } redisDatabase)
        {
            return;
        }

        RedisKey redisKey = keyHolder.GetAsString();
        _ = await redisDatabase.KeyDeleteAsync(redisKey.Prepend(smartCacheRedisOptions.KeyPrefix));
    }
}
