using Diginsight.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Diagnostics;

namespace Diginsight.SmartCache;

public sealed class RedisCacheLocation : PassiveCacheLocation
{
    private readonly ILogger<RedisCacheLocation> logger;
    private readonly IRedisDatabaseAccessor redisDatabaseAccessor;
    private readonly ISmartCacheServiceOptions smartCacheServiceOptions;
    private readonly ISmartCacheRedisOptions smartCacheRedisOptions;

    public override KeyValuePair<string, object?> MetricTag => SmartCacheMetrics.Tags.Type.Redis;

    public RedisCacheLocation(
        ILogger<RedisCacheLocation> logger,
        IRedisDatabaseAccessor redisDatabaseAccessor,
        IOptions<SmartCacheServiceOptions> smartCacheServiceOptions,
        IOptions<SmartCacheRedisOptions> smartCacheRedisOptions,
        string? id = null
    )
        : base(id ?? "<redis>")
    {
        this.logger = logger;
        this.redisDatabaseAccessor = redisDatabaseAccessor;
        this.smartCacheServiceOptions = smartCacheServiceOptions.Value;
        this.smartCacheRedisOptions = smartCacheRedisOptions.Value;
    }

    public override async Task<(TValue, long, double)?> GetAsync<TValue>(
        CacheKeyHolder keyHolder, DateTime minimumCreationDate, Action markInvalid, CancellationToken cancellationToken
    )
    {
        using Activity? activity = SmartCacheMetrics.ActivitySource.StartMethodActivity(logger, new { key = keyHolder.Key });

        if (redisDatabaseAccessor.Database is not { } redisDatabase)
        {
            return null;
        }

        RedisKey redisKey = smartCacheRedisOptions.KeyPrefix + keyHolder.GetAsString();

        RedisValue redisEntry;
        using TimerLap lap = SmartCacheMetrics.Instruments.FetchDuration.CreateLap(SmartCacheMetrics.Tags.Type.Redis);

        using (lap.Start())
        {
            redisEntry = await redisDatabase.StringGetAsync(redisKey);
        }

        if (redisEntry.IsNull)
        {
            lap.AddTags(SmartCacheMetrics.Tags.Found.False);
            return null;
        }

        lap.AddTags(SmartCacheMetrics.Tags.Found.True);

        ValueEntry<TValue> entry;
        using (Activity? deserializeActivity = SmartCacheMetrics.ActivitySource.StartActivity($"{nameof(SmartCacheService)}.Deserialize"))
        {
            deserializeActivity?.WithDurationMetric(
                SmartCacheMetrics.Instruments.SerializationDuration.Underlying,
                SmartCacheMetrics.Tags.Operation.Deserialization,
                SmartCacheMetrics.Tags.Subject.Value
            );

            entry = SmartCacheSerialization.Deserialize<ValueEntry<TValue>>((byte[])redisEntry!);
        }

        double latencyMsecD = lap.ElapsedMilliseconds;
        long latencyMsecL = (long)latencyMsecD;

        if (entry.CreationDate < minimumCreationDate)
        {
            logger.LogDebug("Partial cache miss (latency: {LatencyMsec}): value found in Redis but creation date is invalid", latencyMsecL);

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

        long readLatencyThresholdMsec = smartCacheServiceOptions.ReadLatencyThreshold;
        if (latencyMsecL > readLatencyThresholdMsec)
        {
            logger.LogWarning(
                "Redis retrieval latency {LatencyMsec} (size: {ValueSerializedSize:#,##0}) exceeded the configured threshold of {ReadLatencyThresholdMsec}",
                latencyMsecL,
                valueSerializedSize,
                readLatencyThresholdMsec
            );
        }

        SmartCacheMetrics.Instruments.CompanionFetchRelativeDuration.Record(
            latencyMsecD / valueSerializedSize * 1000, SmartCacheMetrics.Tags.Type.Redis, SmartCacheMetrics.Tags.Found.True
        );

        return (entry.Data, valueSerializedSize, latencyMsecD / valueSerializedSize);
    }

    protected override async Task WriteAsync(CacheKeyHolder keyHolder, IValueEntry entry, TimeSpan expiration, Func<Task> publishMissAsync)
    {
        using Activity? activity = SmartCacheMetrics.ActivitySource.StartMethodActivity(logger, new { key = keyHolder.Key, expiration });

        if (redisDatabaseAccessor.Database is not { } redisDatabase)
        {
            return;
        }

        Stopwatch sw = Stopwatch.StartNew();

        RedisKey redisKey = keyHolder.GetAsBytes();

        byte[] rawEntry;
        using (Activity? serializeActivity = SmartCacheMetrics.ActivitySource.StartRichActivity(logger, $"{nameof(SmartCacheService)}.Serialize"))
        {
            serializeActivity?.WithDurationMetric(
                SmartCacheMetrics.Instruments.SerializationDuration.Underlying,
                SmartCacheMetrics.Tags.Operation.Serialization,
                SmartCacheMetrics.Tags.Subject.Value
            );

            rawEntry = SmartCacheSerialization.SerializeToBytes(entry);
        }

        await redisDatabase.StringSetAsync(redisKey.Prepend(smartCacheRedisOptions.KeyPrefix), rawEntry, expiration);

        sw.Stop();

        logger.LogDebug("redisDatabase.StringSet completed ({ElapsedMsec} ms, {EntryLength} bytes)", sw.ElapsedMilliseconds, rawEntry.LongLength);

        await publishMissAsync();
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
