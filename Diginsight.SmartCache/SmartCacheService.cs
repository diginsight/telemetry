using Diginsight.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using Activity = System.Diagnostics.Activity;
using NotImplementedException = System.NotImplementedException;

namespace Diginsight.SmartCache;

public class SmartCacheService : ISmartCacheService
{
    private static class Metrics
    {
        public static readonly TimerHistogram FetchDuration;
        public static readonly TimerHistogram SerializationDuration;
        public static readonly TimerHistogram SizeComputationDuration;
        public static readonly TimerHistogram CompanionFetchDuration;
        public static readonly Histogram<double> CompanionFetchRelativeDuration;
        public static readonly Counter<int> Sources;
        public static readonly Counter<int> Calls;
        public static readonly Counter<int> Evictions;
        public static readonly Histogram<long> KeyObjectSize;
        public static readonly Histogram<long> ValueObjectSize;
        public static readonly Histogram<long> KeySerializedSize;
        public static readonly Histogram<long> ValueSerializedSize;
        public static readonly UpDownCounter<long> TotalSize;

        static Metrics()
        {
            Meter meter = AutoObservabilityUtils.Meter;
            FetchDuration = meter.CreateTimer("cache.origin_fetch.duration");
            SizeComputationDuration = meter.CreateTimer("cache.size_computation.duration");
            CompanionFetchDuration = meter.CreateTimer("cache.companion_fetch.duration");
            CompanionFetchRelativeDuration = meter.CreateHistogram<double>("cache.companion_fetch.relative_duration", "ms_per_kbyte");
            Sources = meter.CreateCounter<int>("cache.source.count");
            Calls = meter.CreateCounter<int>("cache.call.count");
            Evictions = meter.CreateCounter<int>("cache.eviction.count");
            KeyObjectSize = meter.CreateHistogram<long>("cache.key.object_size", "vbytes");
            ValueObjectSize = meter.CreateHistogram<long>("cache.value.object_size", "vbytes");
            KeySerializedSize = meter.CreateHistogram<long>("cache.key.serialized_size", "bytes");
            ValueSerializedSize = meter.CreateHistogram<long>("cache.value.serialized_size", "bytes");
            TotalSize = meter.CreateUpDownCounter<long>("cache.total_size", "vbytes");
            SerializationDuration = meter.CreateTimer("cache.serialization.duration");
        }

        public static class Tags
        {
            public static class Found
            {
                public static readonly KeyValuePair<string, object?> True = new ("found", true);
                public static readonly KeyValuePair<string, object?> False = new ("found", false);
            }

            public static class SourceType
            {
                public static readonly KeyValuePair<string, object?> Memory = new ("source_type", "memory");
                public static readonly KeyValuePair<string, object?> Distributed = new ("source_type", "distributed");
                public static readonly KeyValuePair<string, object?> Redis = new ("source_type", "redis");
                public static readonly KeyValuePair<string, object?> Miss = new ("source_type", "miss");
                public static readonly KeyValuePair<string, object?> Disabled = new ("source_type", "disabled");
            }

            public static class Eviction
            {
                public static readonly KeyValuePair<string, object?> Expired = new ("eviction_reason", "expired");
                public static readonly KeyValuePair<string, object?> Capacity = new ("eviction_reason", "capacity");
                public static readonly KeyValuePair<string, object?> Removed = new ("eviction_reason", "removed");
                public static readonly KeyValuePair<string, object?> Replaced = new ("eviction_reason", "replaced");
            }

            public static class Subject
            {
                public static readonly KeyValuePair<string, object?> Key = new ("subject", "cache_key");
                public static readonly KeyValuePair<string, object?> Value = new ("subject", "cache_value");
            }

            public static class Operation
            {
                public static readonly KeyValuePair<string, object?> Serialization = new ("operation", "serialization");
                public static readonly KeyValuePair<string, object?> Deserialization = new ("operation", "deserialization");
            }
        }
    }

    private readonly ILogger<SmartCacheService> logger;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly IClassConfigurationGetter classConfigurationGetter;
    private readonly IClassConfigurationGetterProvider classConfigurationGetterProvider;
    private readonly ICacheCompanionProvider companionProvider;
    private readonly ISmartCacheServiceOptions smartCacheServiceOptions;
    private readonly TimeProvider timeProvider;
    private readonly IMemoryCache memoryCache;

    private readonly IDictionary<ICacheKey, ValueTuple> keys = new ConcurrentDictionary<ICacheKey, ValueTuple>();
    private readonly ExternalMissDictionary externalMissDictionary = new ();
    private readonly ConcurrentDictionary<string, Latency> locationLatencies = new ();

    private long memoryCacheSize = 0;

    public SmartCacheService(
        ILogger<SmartCacheService> logger,
        IHttpContextAccessor httpContextAccessor,
        IHttpClientFactory httpClientFactory,
        IClassConfigurationGetter classConfigurationGetter,
        IClassConfigurationGetterProvider classConfigurationGetterProvider,
        ICacheCompanionProvider companionProvider,
        IOptions<SmartCacheServiceOptions> smartCacheServiceOptions,
        IOptions<MemoryCacheOptions> memoryCacheOptions,
        ILoggerFactory loggerFactory,
        TimeProvider? timeProvider = null
    )
    {
        this.logger = logger;
        this.httpContextAccessor = httpContextAccessor;
        this.httpClientFactory = httpClientFactory;
        this.classConfigurationGetter = classConfigurationGetter;
        this.classConfigurationGetterProvider = classConfigurationGetterProvider;
        this.companionProvider = companionProvider;
        this.smartCacheServiceOptions = smartCacheServiceOptions.Value;
        this.timeProvider = timeProvider ?? TimeProvider.System;

        MemoryCacheOptions initialMemoryCacheOptions = memoryCacheOptions.Value;
        MemoryCacheOptions finalMemoryCacheOptions = new ()
        {
            Clock = initialMemoryCacheOptions.Clock ?? new TimeProviderClock(this.timeProvider),
            CompactionPercentage = initialMemoryCacheOptions.CompactionPercentage,
            ExpirationScanFrequency = initialMemoryCacheOptions.ExpirationScanFrequency,
            SizeLimit = this.smartCacheServiceOptions.SizeLimit,
            TrackLinkedCacheEntries = initialMemoryCacheOptions.TrackLinkedCacheEntries,
            TrackStatistics = initialMemoryCacheOptions.TrackStatistics,
        };
        memoryCache = new MemoryCache(finalMemoryCacheOptions, loggerFactory);
    }

    private sealed class TimeProviderClock : ISystemClock
    {
        private readonly TimeProvider timeProvider;

        public TimeProviderClock(TimeProvider timeProvider)
        {
            this.timeProvider = timeProvider;
        }

        public DateTimeOffset UtcNow => timeProvider.GetUtcNow();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<T> GetAsync<T>(
        ICacheKey key,
        Func<Task<T>> fetchAsync,
        ISmartCacheOperationOptions? operationOptions,
        Type? callerType
    )
    {
        callerType ??= RuntimeUtils.GetCaller().DeclaringType;

        using Activity? activity = AutoObservabilityUtils.ActivitySource.StartMethodActivity(logger, new { key, operationOptions });
        activity?.SetTag("cache.key", key.ToLogString());

        Metrics.Calls.Add(1);

        if (operationOptions?.Enabled != true)
        {
            Metrics.Sources.Add(1, Metrics.Tags.SourceType.Disabled);

            using (Metrics.FetchDuration.StartLap(Metrics.Tags.SourceType.Disabled))
            {
                activity?.SetTag("cache.disabled", 1);
                return await fetchAsync();
            }
        }

        return await GetAsync(key, fetchAsync, operationOptions.MaxAge, callerType, operationOptions.AbsoluteExpiration, operationOptions.SlidingExpiration);
    }

    private async Task<TValue> GetAsync<TValue>(
        ICacheKey key,
        Func<Task<TValue>> fetchAsync,
        TimeSpan? maxAge,
        Type callerType,
        TimeSpan? absExpiration = null,
        TimeSpan? sldExpiration = null
    )
    {
        using Activity? activity = AutoObservabilityUtils.ActivitySource.StartMethodActivity(logger, new { key, maxAge, absExpiration, sldExpiration });

        DateTime utcNow = timeProvider.GetUtcNow().UtcDateTime;
        DateTime minimumCreationDate = GetMinimumCreationDate(ref maxAge, callerType, utcNow);
        bool forceFetch = maxAge.Value <= TimeSpan.Zero || minimumCreationDate >= utcNow;

        using TimerLap memoryLap = Metrics.FetchDuration.CreateLap(Metrics.Tags.SourceType.Memory);
        memoryLap.DisableCommit = true;

        ValueEntry<TValue>? valueEntry;
        ExternalMissDictionary.Entry? externalEntry;

        bool discardExternalMiss = classConfigurationGetter.Get("DiscardExternalMiss", false);

        if (forceFetch)
        {
            valueEntry = null;
            externalEntry = null;
        }
        else
        {
            using (memoryLap.Start())
            using (AutoObservabilityUtils.ActivitySource.StartActivity(logger, $"{nameof(SmartCacheService)}.GetFromMemory"))
            {
                valueEntry = memoryCache.Get<ValueEntry<TValue>?>(key);
                externalEntry = discardExternalMiss ? null : externalMissDictionary.Get(key);
            }

            if (valueEntry is not null)
            {
                logger.LogDebug("Cache entry found");
            }
        }

        [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
        async Task<TValue> FetchAndSetValueAsync()
        {
            Metrics.Sources.Add(1, Metrics.Tags.SourceType.Miss);
            Activity.Current?.SetTag("cache.hit", 0);

            TValue value;
            TimerLap fetchLap = Metrics.FetchDuration.CreateLap(Metrics.Tags.SourceType.Miss);
            using (fetchLap.Start())
            {
                value = await fetchAsync();
            }

            logger.LogDebug("Fetched in {LatencyMsec} ms", (long)fetchLap.ElapsedMilliseconds);

            using (AutoObservabilityUtils.ActivitySource.StartActivity(logger, $"{nameof(SmartCacheService)}.SetValue"))
            {
                SetValue(key, value, absExpiration, sldExpiration, skipPublish: discardExternalMiss);
                return value;
            }
        }

        DateTime? localCreationDate = valueEntry?.CreationDate;

        if (externalEntry is var (othersCreationDate, locations) && !(othersCreationDate - smartCacheServiceOptions.LocalEntryTolerance <= localCreationDate))
        {
            if (othersCreationDate >= minimumCreationDate)
            {
                logger.LogDebug("Key is also available and up-to-date in other locations: {Locations}", locations);

                HttpClient httpClient = httpClientFactory.CreateClient(nameof(SmartCacheService));

                string rawKey;
                using (Activity? serializeActivity = AutoObservabilityUtils.ActivitySource.StartActivity(logger, $"{nameof(SmartCacheService)}.Serialize"))
                {
                    serializeActivity?.RecordDurationMetric(
                        Metrics.SerializationDuration.Underlying, Metrics.Tags.Subject.Key, Metrics.Tags.Operation.Serialization
                    );

                    rawKey = SmartCacheSerialization.SerializeToString(key);
                }

                HttpContent requestContent = new StringContent(rawKey, SmartCacheSerialization.Encoding, "application/json");
                long keySerializedSize = requestContent.Headers.ContentLength!.Value;

                ConcurrentBag<string> invalidLocations = [];

                [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
                async Task<StrongBox<(TValue, double, long)>?> GetFromCompanionAsync(string companionIp, CancellationToken ct)
                {
                    using TimerMark mark = CacheMetrics.FetchDuration.CreateMark(MetricTags.Distributed);
                    try
                    {
                        Stopwatch sw = Stopwatch.StartNew();

                        HttpResponseMessage responseMessage;
                        using (mark.Start())
                        {
                            responseMessage = await httpClient.PostAsync($"http://{companionIp}/api/v1/clusterCache/get", requestContent, ct);
                        }

                        TValue item;
                        long valueSerializedSize;
                        using (responseMessage)
                        {
                            responseMessage.EnsureSuccessStatusCode();
                            HttpContent responseContent = responseMessage.Content;

                            valueSerializedSize = responseContent.Headers.ContentLength!.Value;

                            await using (Stream contentStream = await responseContent.ReadAsStreamAsync(ct))
                            {
                                using (CacheMetrics.SerializationDuration.StartMark(MetricTags.CacheValue, MetricTags.Deserialization))
                                using (ActivitySource.StartActivity("CacheService.Deserialize"))
                                {
                                    item = SmartCacheSerialization.Deserialize<TValue>(contentStream);
                                }
                            }
                        }

                        long latencyMsec = sw.ElapsedMilliseconds;

                        scope.LogDebug($"Cache hit: Returning up-to-date value for {keyLogString} from companion {companionIp}. Latency: {latencyMsec}");

                        long readLatencyThresholdMsec = cacheServiceOptions.ReadLatencyThreshold;
                        if (latencyMsec > readLatencyThresholdMsec)
                        {
                            scope.LogWarning($"Companion retrieval latency {latencyMsec} (size:{valueSerializedSize:#,##0}) exceeded the configured threshold of {readLatencyThresholdMsec}");
                        }

                        CacheMetrics.RelativeFetchDuration.Record(mark.ElapsedMilliseconds / valueSerializedSize * 1000, MetricTags.Distributed, MetricTags.Found);

                        mark.AddTags(MetricTags.Found);
                        return new StrongBox<(TValue, double, long)>((item, (double)latencyMsec / valueSerializedSize, valueSerializedSize));
                    }
                    catch (Exception e) when (e is InvalidOperationException or HttpRequestException || e is TaskCanceledException tce && tce.CancellationToken != ct)
                    {
                        mark.AddTags(MetricTags.NotFound);
                        invalidLocations.Add(companionIp);
                        scope.LogDebug($"Partial cache miss: Failed to retrieve value for {keyLogString} from companion {companionIp}");
                    }

                    return null;
                }

                [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
                async Task<StrongBox<(TValue, double, long)>?> GetFromRedisAsync()
                {
                    if (redisDatabaseAccessor.Database is not { } redisDatabase)
                    {
                        return null;
                    }

                    RedisKey redisKey = cacheServiceOptions.RedisKeyPrefix + rawKey;

                    Stopwatch sw = Stopwatch.StartNew();

                    RedisValue redisEntry;
                    using TimerMark mark = CacheMetrics.FetchDuration.CreateMark(MetricTags.Redis);
                    {
                        using (mark.Start())
                        {
                            redisEntry = await redisDatabase.StringGetAsync(redisKey);
                        }
                    }

                    if (redisEntry.IsNull)
                    {
                        mark.AddTags(MetricTags.NotFound);
                        return null;
                    }

                    mark.AddTags(MetricTags.Found);

                    ValueEntry<TValue> entry;
                    using (CacheMetrics.SerializationDuration.StartMark(MetricTags.CacheValue, MetricTags.Deserialization))
                    {
                        using (ActivitySource.StartActivity("CacheService.Deserialize"))
                        {
                            entry = SmartCacheSerialization.Deserialize<ValueEntry<TValue>>((byte[])redisEntry!);
                        }
                    }

                    long latencyMsec = sw.ElapsedMilliseconds;

                    if (entry.CreationDate < minimumCreationDate)
                    {
                        scope.LogDebug($"Partial cache miss: Value for {keyLogString} found in Redis but CreationDate is invalid. Latency: {latencyMsec}");

                        invalidLocations.Add(RedisLocation);
                        _ = await redisDatabase.KeyDeleteAsync(redisKey);
                        return null;
                    }

                    long valueSerializedSize = redisEntry.Length();
                    scope.LogDebug($"Cache hit (Latency:{latencyMsec}, Size:{valueSerializedSize:#,##0}): Returning up-to-date value for {keyLogString} from Redis.");

                    long readLatencyThresholdMsec = cacheServiceOptions.ReadLatencyThreshold;
                    if (latencyMsec > readLatencyThresholdMsec)
                    {
                        scope.LogWarning($"Redis retrieval latency {latencyMsec} (size:{valueSerializedSize:#,##0}) exceeded the configured threshold of {readLatencyThresholdMsec}");
                    }

                    CacheMetrics.RelativeFetchDuration.Record(mark.ElapsedMilliseconds / valueSerializedSize * 1000, MetricTags.Redis, MetricTags.Found);

                    return new Optional<(TValue, double, long)>((entry.Data, (double)latencyMsec / valueSerializedSize, valueSerializedSize));
                }

                Func<CancellationToken, Task<Optional<(TValue, string, long)>>> UpdatingLatency(
                    string location,
                    Func<CancellationToken, Task<Optional<(TValue, double, long)>>> getFromLocationAsync
                )
                {
                    return async ct =>
                    {
                        Optional<(TValue Item, double RelativeLatency, long ValueSerializedSize)> outputOpt = await getFromLocationAsync(ct);
                        if (!outputOpt.IsUndefined)
                        {
                            Latency latency = locationLatencies.GetOrAdd(location, static _ => new Latency());
                            latency.Add(outputOpt.Value.RelativeLatency);
                        }
                        else if (location != RedisLocation && invalidLocations.Contains(location))
                        {
                            locationLatencies.TryRemove(location, out _);
                        }

                        return outputOpt.Convert(x => (x.Item, location, x.ValueSerializedSize));
                    };
                }

                IEnumerable<Func<CancellationToken, Task<Optional<(TValue, string, long)>>>> taskFactories = locations
                    .GroupJoin(
                        locationLatencies,
                        static l => l,
                        static kv => kv.Key,
                        static (l, kvs) => (Location: l, Latency: kvs.FirstOrDefault().Value ?? new Latency())
                    )
                    .OrderBy(static kv => kv.Latency)
                    .Select(
                        kv =>
                        {
                            string location = kv.Location;
                            Func<CancellationToken, Task<Optional<(TValue, double, long)>>> getFromLocationAsync =
                                location == RedisLocation
                                    ? _ => GetFromRedisAsync()
                                    : ct => GetFromCompanionAsync(location, ct);

                            return UpdatingLatency(location, getFromLocationAsync);
                        }
                    )
                    .ToArray();

                Optional<(TValue Item, string Location, long ValueSerializedSize)> outputOpt;
                try
                {
                    outputOpt = await TaskExtensions.WhenAnyValid(
                        taskFactories.ToArray(),
                        cacheServiceOptions.CompanionPrefetchCount,
                        cacheServiceOptions.CompanionMaxParallelism,
                        // ReSharper disable once AsyncApostle.AsyncWait
                        isValid: static t => new ValueTask<bool>(!t.IsCompletedSuccessfully || !t.Result.IsUndefined)
                    );
                }
                catch (InvalidOperationException)
                {
                    outputOpt = null;
                }
                finally
                {
                    if (invalidLocations.Any())
                    {
                        externalMissDictionary.RemoveSub(key, invalidLocations);
                    }
                }

                if (!outputOpt.IsUndefined)
                {
                    (TValue item, string location, long valueSerializedSize) = outputOpt.Value;

                    KeyValuePair<string, object?> locationTags = location == RedisLocation ? MetricTags.Redis : MetricTags.Distributed;
                    CacheMetrics.KeySerializedSize.Record(keySerializedSize, locationTags);
                    CacheMetrics.ValueSerializedSize.Record(valueSerializedSize, locationTags);
                    CacheMetrics.Sources.Add(1, locationTags);

                    SetValue(key, item, absExpiration, sldExpiration, othersCreationDate, skipPublish: discardExternalMiss);
                    return item!;
                }
            }
            else
            {
                scope.LogDebug($"Cache miss: CreationDate validation failed (minimumCreationDate: '{minimumCreationDate:O}', older entry CreationDate: '{localCreationDate ?? DateTime.MinValue:O}').");
            }

            return await FetchAndSetValueAsync();
        }

        memoryLap.DisableCommit = false;

        if (localCreationDate >= minimumCreationDate && valueEntry!.Data is { } data)
        {
            scope.LogDebug($"Cache hit: valid creation date (minimumCreationDate: '{minimumCreationDate:O}', newer entry CreationDate: '{localCreationDate.Value:O}')");

            memoryLap.AddTags(MetricTags.Found);
            CacheMetrics.Sources.Add(1, MetricTags.Memory);
            Activity.Current?.SetTag("cache.hit", 1);

            return data;
        }
        else
        {
            scope.LogDebug($"Cache miss: CreationDate validation failed (minimumCreationDate: '{minimumCreationDate:O}', older entry CreationDate: '{localCreationDate ?? DateTime.MinValue:O}').");

            memoryLap.AddTags(MetricTags.NotFound);

            return await FetchAndSetValueAsync();
        }
    }

    private void SetValue<TValue>(
        ICacheKey key,
        TValue value,
        int? absExpirationSec = null,
        int? sldExpirationSec = null,
        DateTime? creationDate = null,
        bool skipPublish = false
    )
    {
        SetValue(key, typeof(TValue), value, absExpirationSec, sldExpirationSec, creationDate, /*skipPersist,*/ skipPublish);
    }

    private void SetValue(
        ICacheKey key,
        Type valueType,
        object? value,
        int? absExpirationSec = null,
        int? sldExpirationSec = null,
        DateTime? creationDate = null,
        bool skipPublish = false
    )
    {
        using var scope = logger.BeginMethodScope(() => new { key = key.ToLogString() });

        keys[key] = default;
        RemoveExternalMiss(key);

        IValueEntry entry = IValueEntry.Create(value, valueType, creationDate);
        DateTime finalCreationDate = entry.CreationDate;

        int finalAbsExpirationSecs = absExpirationSec ?? cacheServiceOptions.AbsoluteExpiration;
        TimeSpan finalAbsExpiration = TimeSpan.FromSeconds(finalAbsExpirationSecs);

        if (classConfigurationGetter.Get("RedisOnlyCache", false))
        {
            WriteToRedis(scope, key, entry, finalAbsExpiration, skipPublish);
            return;
        }

        int finalSldExpirationSecs = Math.Min(sldExpirationSec ?? cacheServiceOptions.SlidingExpiration, finalAbsExpirationSecs);

        long keySize;
        try
        {
            using (CacheMetrics.ComputeSizeDuration.StartMark(MetricTags.CacheKey))
            {
                keySize = Size.Get(key);
            }
            CacheMetrics.KeyObjectSize.Record(keySize);
        }
        catch (Exception)
        {
            keySize = 0;
        }

        long valueSize;
        using (CacheMetrics.ComputeSizeDuration.StartMark(MetricTags.CacheValue))
        {
            valueSize = Size.Get(value);
        }
        CacheMetrics.ValueObjectSize.Record(valueSize);

        long size = keySize + valueSize;

        CacheItemPriority priority =
            size >= cacheServiceOptions.LowPrioritySizeThreshold ? CacheItemPriority.Low
            : size >= cacheServiceOptions.MidPrioritySizeThreshold ? CacheItemPriority.Normal
            : CacheItemPriority.High;

        MemoryCacheEntryOptions entryOptions = new ()
        {
            AbsoluteExpirationRelativeToNow = finalAbsExpiration,
            SlidingExpiration = TimeSpan.FromSeconds(finalSldExpirationSecs),
            Size = size,
            Priority = priority,
        };

        entryOptions.RegisterPostEvictionCallback(
            (k, v, r, _) =>
            {
                Interlocked.Add(ref memoryCacheSize, -size);
                CacheMetrics.TotalSize.Add(-size);

                OnEvicted((ICacheKey)k, (IValueEntry)v, r, finalAbsExpiration);
            }
        );

        CacheExtensions.Set(memoryCache, key, entry, entryOptions);

        Interlocked.Add(ref memoryCacheSize, size);
        CacheMetrics.TotalSize.Add(size);

        if (!skipPublish)
        {
            PublishMiss(key, finalCreationDate, (value, valueType), false);
        }
    }

    private void OnEvicted(ICacheKey key, IValueEntry entry, EvictionReason reason, TimeSpan expiration)
    {
        CacheMetrics.Evictions.Add(
            1,
            reason switch
            {
                EvictionReason.Removed => MetricTags.EvictionRemoved,
                EvictionReason.Replaced => MetricTags.EvictionReplaced,
                EvictionReason.Expired or EvictionReason.TokenExpired => MetricTags.EvictionExpired,
                EvictionReason.Capacity => MetricTags.EvictionCapacity,
                EvictionReason.None => throw new InvalidOperationException($"unexpected {nameof(EvictionReason)}"),
                _ => throw new UnreachableException($"unrecognized {nameof(EvictionReason)}"),
            }
        );

        if (reason is EvictionReason.None or EvictionReason.Replaced)
        {
            return;
        }

        using var scope = logger.BeginMethodScope(() => new { reason, expiration, key, entry });

        try
        {
            keys.Remove(key);

            if (reason != EvictionReason.Capacity)
            {
                return;
            }

            WriteToRedis(scope, key, entry, expiration);
        }
        catch (Exception exception)
        {
            scope.LogException(exception);
        }
    }

    private void WriteToRedis(CodeSectionScope scope, ICacheKey key, IValueEntry entry, TimeSpan expiration, bool skipPublish = false)
    {
        _ = Task.Run(WriteToRedisAsync);

        async Task WriteToRedisAsync()
        {
            if (redisDatabaseAccessor.Database is not { } redisDatabase)
            {
                return;
            }

            Stopwatch sw = Stopwatch.StartNew();

            RedisKey redisKey;
            using (CacheMetrics.SerializationDuration.StartMark(MetricTags.CacheKey, MetricTags.Serialization))
            {
                using (ActivitySource.StartActivity("CacheService.Serialize"))
                {
                    redisKey = SmartCacheSerialization.SerializeToBytes(key);
                }
            }

            byte[] rawEntry;
            using (CacheMetrics.SerializationDuration.StartMark(MetricTags.CacheValue, MetricTags.Serialization))
            {
                using (ActivitySource.StartActivity("CacheService.Serialize"))
                {
                    rawEntry = SmartCacheSerialization.SerializeToBytes(entry);
                }
            }

            await redisDatabase.StringSetAsync(redisKey.Prepend(cacheServiceOptions.RedisKeyPrefix), rawEntry, expiration);

            long elapsedMs = sw.ElapsedMilliseconds;
            scope.LogDebug($"redisDatabase.StringSet completed ({elapsedMs} ms, {rawEntry.LongLength} bytes)");

            if (!skipPublish)
            {
                await PublishMissAsync(key, entry.CreationDate, null, true);
            }
        }
    }

    private void PublishMiss(ICacheKey key, DateTime creationDate, (object?, Type)? valueHolder, bool onRedis)
    {
        _ = Task.Run(() => PublishMissAsync(key, creationDate, valueHolder, onRedis));
    }

    private async Task PublishMissAsync(ICacheKey key, DateTime creationDate, (object?, Type)? valueHolder, bool onRedis)
    {
        using var scope = logger.BeginMethodScope(() => new { key = key.ToLogString(), creationDate });

        if (onRedis)
        {
            using (ActivitySource.StartActivity("CacheService.SetMissValue"))
            {
                externalMissDictionary.Add(key, creationDate, RedisLocation);
            }
        }

        await PostAndForgetAsync(
            async () =>
            {
                (Type, object?)? valueTuple;
                if (valueHolder is var (value, valueType) && cacheServiceOptions.MissValueSizeThreshold is > 0 and var size)
                {
                    byte[] valueBytes = new byte[size];
                    await using MemoryStream valueStream = new (valueBytes);

                    using (CacheMetrics.SerializationDuration.StartMark(MetricTags.CacheValue, MetricTags.Serialization))
                    using (ActivitySource.StartActivity("CacheService.Serialize"))
                    {
                        try
                        {
                            SmartCacheSerialization.SerializeToStream(value, valueType, valueStream);
                            valueTuple = (valueType, value);
                        }
                        catch (NotSupportedException) // In case the serialized value is longer than 'size'
                        {
                            valueTuple = null;
                        }
                    }
                }
                else
                {
                    valueTuple = null;
                }

                string selfIp = companionProvider.SelfIp;
                return new CacheMissDescriptor(selfIp, key, creationDate, onRedis ? RedisLocation : selfIp, valueTuple);
            },
            "cacheMiss"
        );
    }

    private DateTime GetMinimumCreationDate([NotNull] ref TimeSpan? maxAge, Type callerType, DateTime utcNow)
    {
        TimeSpan finalMaxAge = maxAge ?? smartCacheServiceOptions.DefaultMaxAge;

        // TODO Use class configuration getter on callerType
        throw new NotImplementedException();

        //if (httpContextAccessor.HttpContext is { } httpContext)
        //{
        //    bool ExtractMaxAgeFromHeader(string headerName)
        //    {
        //        if (!httpContext.Request.Headers.TryGetValue(headerName, out StringValues headerMaxAges)
        //            || !int.TryParse(headerMaxAges.LastOrDefault(), out int headerMaxAge))
        //        {
        //            return false;
        //        }

        //        scope.LogDebug($"From request header: {headerName}={headerMaxAge}");
        //        if (headerMaxAge >= finalMaxAge)
        //        {
        //            return false;
        //        }

        //        finalMaxAge = headerMaxAge;
        //        return true;
        //    }

        //    string? namespaceName = callerType?.Namespace;
        //    string[] maxAgeHeaderNames = namespaceName != null
        //        ? new[] { $"{namespaceName}.{callerType!.Name}.MaxAge", $"{namespaceName}.MaxAge", "MaxAge" }
        //        : new[] { "MaxAge" };

        //    foreach (string maxAgeHeaderName in maxAgeHeaderNames)
        //    {
        //        if (ExtractMaxAgeFromHeader(maxAgeHeaderName))
        //            break;
        //    }
        //}

        //DateTime requestStartedOn =
        //    (httpContextAccessor.HttpContext?.Items.TryGetValue("RequestStartedOn", out var rawRequestStartedOn) == true
        //        ? rawRequestStartedOn as DateTime? : null)
        //    ?? utcNow;

        //DateTime minimumCreationDate = requestStartedOn.Subtract(TimeSpan.FromSeconds(finalMaxAge));
        //if (httpContextAccessor.HttpContext?.Request.Headers.TryGetValue("MinimumCreationDate", out StringValues headerMinimumCreationDates) == true
        //    && DateTime.TryParse(
        //        headerMinimumCreationDates.LastOrDefault(),
        //        null,
        //        DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
        //        out DateTime headerMinimumCreationDate
        //    ))
        //{
        //    scope.LogDebug($"From header: {nameof(minimumCreationDate)}={headerMinimumCreationDate}");

        //    if (headerMinimumCreationDate > minimumCreationDate)
        //    {
        //        minimumCreationDate = headerMinimumCreationDate;
        //    }
        //}

        //maxAge = finalMaxAge;
        //return minimumCreationDate;
    }

    private void RemoveExternalMiss(ICacheKey key)
    {
        if (externalMissDictionary.Remove(key))
        {
            DeleteFromRedis(key);
        }
    }

    private void DeleteFromRedis(ICacheKey key)
    {
        _ = Task.Run(DeleteFromRedisAsync);

        async Task DeleteFromRedisAsync()
        {
            if (redisDatabaseAccessor.Database is not { } redisDatabase)
            {
                return;
            }

            RedisKey redisKey;
            using (CacheMetrics.SerializationDuration.StartMark(MetricTags.CacheKey, MetricTags.Serialization))
            {
                using (ActivitySource.StartActivity("CacheService.Serialize"))
                {
                    redisKey = (RedisKey)SmartCacheSerialization.SerializeToString(key);
                }
            }

            _ = await redisDatabase.KeyDeleteAsync(redisKey.Prepend(cacheServiceOptions.RedisKeyPrefix));
        }
    }

    public bool TryGetDirectFromMemory(ICacheKey key, [NotNullWhen(true)] out Type? type, out object? value)
    {
        using var scope = logger.BeginMethodScope(() => new { key = key.ToLogString() });

        if (CacheExtensions.Get<IValueEntry?>(memoryCache, key) is { } entry)
        {
            type = entry.Type;
            value = entry.Data;
            return true;
        }
        else
        {
            type = null;
            value = null;
            return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Invalidate(IInvalidationRule invalidationRule)
    {
        Invalidate(invalidationRule, true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Invalidate(InvalidationDescriptor descriptor)
    {
        if (descriptor.Emitter != companionProvider.SelfIp)
        {
            Invalidate(descriptor.Rule, false);
        }
    }

    private void Invalidate(IInvalidationRule invalidationRule, bool broadcast)
    {
        using var scope = logger.BeginMethodScope(() => new { invalidationRule = invalidationRule.ToLogString() });

        ICollection<Func<Task>> invalidationCallbacks = new List<Func<Task>>();

        void CoreInvalidate(IEnumerable<ICacheKey> ks, Action<ICacheKey> remove)
        {
            foreach (ICacheKey k in ks.ToArray())
            {
                if (k is not IInvalidatable invalidatable ||
                    !invalidatable.IsInvalidatedBy(invalidationRule, out var invalidationCallback))
                {
                    continue;
                }

                scope.LogDebug($"invalidating cache key {k.ToLogString()}");

                remove(k);
                if (invalidationCallback is not null)
                {
                    invalidationCallbacks.Add(invalidationCallback);
                }
            }
        }

        using (ActivitySource.StartActivity("CacheService.Invalidate"))
        {
            CoreInvalidate(keys.Keys, memoryCache.Remove);
            CoreInvalidate(externalMissDictionary.Keys, RemoveExternalMiss);
        }

        if (broadcast)
        {
            PublishInvalidation(invalidationRule);
        }

        _ = Task.Run(
            async () =>
            {
                foreach (Func<Task> invalidationCallback in invalidationCallbacks)
                {
                    await invalidationCallback();
                }
            }
        );
    }

    private void PublishInvalidation(IInvalidationRule invalidationRule)
    {
        _ = Task.Run(PublishInvalidationAsync);

        async Task PublishInvalidationAsync()
        {
            using var localScope = logger.BeginMethodScope(() => new { invalidationRule = invalidationRule.ToLogString() }, memberName: nameof(PublishInvalidationAsync));

            await PostAndForgetAsync(async () => new InvalidationDescriptor(companionProvider.SelfIp, invalidationRule), "invalidate");
        }
    }

    private async Task PostAndForgetAsync<T>(Func<Task<T?>> makeObjAsync, string uriSuffix)
    {
        IEnumerable<string> companionIps = await companionProvider.GetCompanionIpsAsync();
        if (!companionIps.Any())
        {
            return;
        }

        HttpClient httpClient = httpClientFactory.CreateClient(nameof(SmartCacheService));

        string stringContent;
        using (Activity? serializeActivity = AutoObservabilityUtils.ActivitySource.StartActivity(logger, $"{nameof(SmartCacheService)}.Serialize"))
        {
            serializeActivity?.RecordDurationMetric(
                Metrics.SerializationDuration.Underlying, Metrics.Tags.Subject.Value, Metrics.Tags.Operation.Serialization
            );

            stringContent = SmartCacheSerialization.SerializeToString(await makeObjAsync());
        }

        HttpContent content = new StringContent(stringContent, SmartCacheSerialization.Encoding);

        foreach (string companionIp in companionIps)
        {
            _ = Task.Run(
                async () =>
                {
                    HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, $"http://{companionIp}/api/v1/clusterCache/{uriSuffix}")
                    {
                        Content = content,
                    };
                    using HttpResponseMessage responseMessage = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
                }
            );
        }
    }

    public void AddExternalMiss(CacheMissDescriptor descriptor)
    {
        (string emitter,
            ICacheKey key,
            DateTime timestamp,
            string location,
            Type? valueType) = descriptor;

        if (emitter == companionProvider.SelfIp)
        {
            return;
        }

        if (valueType is not null)
        {
            SetValue(key, valueType, descriptor.Value, creationDate: timestamp, skipPublish: true);
        }
        else
        {
            externalMissDictionary.Add(key, timestamp, location);
        }
    }

    private sealed class ExternalMissDictionary
    {
        private readonly ConcurrentDictionary<ICacheKey, Entry> underlying = new ConcurrentDictionary<ICacheKey, Entry>();

        public IEnumerable<ICacheKey> Keys => underlying.Keys;

        private readonly object lockObject = new object();

        public Entry? Get(ICacheKey key)
        {
            return underlying.GetValueOrDefault(key);
        }

        public bool Remove(ICacheKey key)
        {
            return underlying.TryRemove(key, out Entry? entry)
                && entry.Locations.Contains(RedisLocation);
        }

        public void RemoveSub(ICacheKey key, IEnumerable<string> locations)
        {
            lock (lockObject)
            {
                if (!underlying.TryGetValue(key, out Entry? entry))
                {
                    return;
                }

                foreach (string location in locations)
                {
                    entry = entry with { Locations = entry.Locations.Where(x => x != location).ToArray() };

                    if (!entry.Locations.Any())
                    {
                        underlying.TryRemove(key, out _);
                        return;
                    }
                }

                underlying[key] = entry;
            }
        }

        public void Add(ICacheKey key, DateTime timestamp, string location)
        {
            lock (lockObject)
            {
                if (!underlying.TryGetValue(key, out Entry? entry) || entry.Timestamp < timestamp)
                {
                    underlying[key] = new Entry(timestamp, new[] { location });
                }
                else if (!(entry.Timestamp > timestamp))
                {
                    underlying[key] = entry with { Locations = entry.Locations.Append(location).Distinct().ToArray() };
                }
            }
        }

        public sealed record Entry(DateTime Timestamp, IEnumerable<string> Locations);
    }

    private sealed class Latency : IComparable<Latency>
    {
        private double average = double.PositiveInfinity;
        private int count = 0;

        public void Add(double latency)
        {
            if (count == 0)
            {
                average = latency;
                count = 1;
            }
            else
            {
                average = ((average * count) + latency) / ++count;
            }
        }

        public int CompareTo(Latency? other) => average.CompareTo(other?.average ?? double.PositiveInfinity);
    }

    private static class Size
    {
        private static readonly MethodInfo GetUnmanagedSizeMethod = typeof(Size)
            .GetMethod(nameof(GetUnmanagedSize), BindingFlags.NonPublic | BindingFlags.Static)!;

        private static readonly IDictionary<Type, long> FixedSizes = new ConcurrentDictionary<Type, long>();
        private static readonly IDictionary<Type, ValueTuple> ManagedTypes = new ConcurrentDictionary<Type, ValueTuple>();
        private static readonly IDictionary<Type, (long, IEnumerable<FieldInfo>)> VariableCache = new ConcurrentDictionary<Type, (long, IEnumerable<FieldInfo>)>();

        public static long Get(object? obj)
        {
            ISet<object> seen = new HashSet<object>();
            int depth = 0;

            (long Sz, bool Fxd) CoreGet(object? current)
            {
                depth++;
                try
                {
                    if (current is null)
                    {
                        return (0, false);
                    }

                    if (current is Pointer or Delegate)
                    {
                        throw new ArgumentException("pointers and delegates not supported");
                    }

                    Type type = current.GetType();
                    if (FixedSizes.TryGetValue(type, out long fsz))
                    {
                        return (fsz, true);
                    }

                    if (!ManagedTypes.ContainsKey(type))
                    {
                        if (TryGetUnmanagedSize(type) is { } usz)
                        {
                            return (FixedSizes[type] = usz, true);
                        }
                        else
                        {
                            ManagedTypes.TryAdd(type, default);
                        }
                    }

                    if (current is string str)
                    {
                        return (str.Length * sizeof(char), false);
                    }

                    if (current is IManualSize ms)
                    {
                        return ms.GetSize(CoreGet);
                    }

                    if (current is Type)
                    {
                        return (IntPtr.Size, true);
                    }

                    if (current is JToken jt)
                    {
                        return jt switch
                        {
                            JValue jv => (CoreGet(jv.Value).Sz, false),
                            JArray ja => (CoreGet(ja.Children().ToArray()).Sz, false),
                            JObject jo => (CoreGet(jo.Properties().ToArray()).Sz, false),
                            JProperty jp => (CoreGet(jp.Name).Sz + CoreGet(jp.Value).Sz, false),
                            JConstructor jc => (CoreGet(jc.Name).Sz + CoreGet(jc.Children().ToArray()).Sz, false),
                            _ => throw new UnreachableException($"unsupported {nameof(JToken)} subclass"),
                        };
                    }

                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Lazy<>))
                    {
                        return CoreGet(type.GetProperty(nameof(Lazy<object>.Value))!.GetValue(current));
                    }

                    if (!seen.Add(current))
                    {
                        return (IntPtr.Size, false);
                    }

                    try
                    {
                        if (current is IEnumerable enumerable)
                        {
                            long sz = 0;

                            IEnumerator enumerator = enumerable.GetEnumerator();
                            try
                            {
                                while (enumerator.MoveNext())
                                {
                                    sz += CoreGet(enumerator.Current).Sz;
                                }
                            }
                            finally
                            {
                                (enumerator as IDisposable)?.Dispose();
                            }

                            return (sz, false);
                        }

                        if (VariableCache.TryGetValue(type, out (long, IEnumerable<FieldInfo>) pair))
                        {
                            (long baseSz, IEnumerable<FieldInfo> fields) = pair;
                            return (fields.Aggregate(baseSz, (sz, field) => sz + CoreGet(field.GetValue(current)).Sz), false);
                        }

                        long fixedSz = 0;
                        long variableSz = 0;

                        FieldInfo[] allFields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        ICollection<FieldInfo> variableFields = new List<FieldInfo>();

                        foreach (FieldInfo field in allFields)
                        {
                            (long sz, bool fxd) = CoreGet(field.GetValue(current));
                            if (fxd)
                            {
                                fixedSz += sz;
                            }
                            else
                            {
                                variableSz += sz;
                                variableFields.Add(field);
                            }
                        }

                        if (!variableFields.Any())
                        {
                            return (fixedSz, true);
                        }

                        VariableCache[type] = (fixedSz, variableFields);
                        return (fixedSz + variableSz, false);
                    }
                    finally
                    {
                        seen.Remove(current);
                    }
                }
                finally
                {
                    depth--;
                }
            }

            return CoreGet(obj).Sz;
        }

        private static long? TryGetUnmanagedSize(Type type)
        {
            try
            {
                return (long)GetUnmanagedSizeMethod.MakeGenericMethod(type).Invoke(null, Array.Empty<object>())!;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static unsafe long GetUnmanagedSize<T>()
            where T : unmanaged
        {
            return sizeof(T);
        }
    }
}
