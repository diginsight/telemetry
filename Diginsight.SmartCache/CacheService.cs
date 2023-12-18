using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using IDisposable = System.IDisposable;

namespace Diginsight.SmartCache;

public class CacheService : ICacheService
{
    private static readonly CacheMetrics CacheMetrics = CacheMetrics.Instance;
    public static readonly ActivitySource ActivitySource = new(CacheMetrics.ObservabilityName);

    // public const string ObservabilityName = "cache-service";
    private const string RedisLocation = "<redis>";

    private readonly ICacheServiceOptions cacheServiceOptions;
    private readonly ILogger<CacheService> logger;
    private readonly IMemoryCache memoryCache;
    private readonly IRedisDatabaseAccessor redisDatabaseAccessor;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly IClassConfigurationGetter classConfigurationGetter;
    private readonly ICacheCompanionProvider companionProvider;
    private readonly IDictionary<ICacheKey, ValueTuple> keys = new ConcurrentDictionary<ICacheKey, ValueTuple>();
    private readonly ExternalMissDictionary externalMissDictionary = new();
    private readonly ConcurrentDictionary<string, Latency> locationLatencies = new();

    private long memoryCacheSize = 0;

    public CacheService(
        IOptions<MemoryCacheOptions> memoryCacheOptionsOptions,
        ILoggerFactory loggerFactory,
        IOptions<CacheServiceOptions> cacheServiceOptionsOptions,
        ILogger<CacheService> logger,
        IRedisDatabaseAccessor redisDatabaseAccessor,
        IHttpContextAccessor httpContextAccessor,
        IHttpClientFactory httpClientFactory,
        IClassConfigurationGetter<CacheService> classConfigurationGetter,
        ICacheCompanionProvider companionProvider)
    {
        cacheServiceOptions = cacheServiceOptionsOptions.Value;
        this.logger = logger;

        MemoryCacheOptions initalMemoryCacheOptions = memoryCacheOptionsOptions.Value;
        MemoryCacheOptions memoryCacheOptions = new()
        {
            Clock = initalMemoryCacheOptions.Clock,
            CompactionPercentage = initalMemoryCacheOptions.CompactionPercentage,
            ExpirationScanFrequency = initalMemoryCacheOptions.ExpirationScanFrequency,
            SizeLimit = cacheServiceOptions.SizeLimit,
        };
        memoryCache = new MemoryCache(memoryCacheOptions, loggerFactory);

        this.redisDatabaseAccessor = redisDatabaseAccessor;
        this.httpContextAccessor = httpContextAccessor;
        this.httpClientFactory = httpClientFactory;
        this.classConfigurationGetter = classConfigurationGetter;
        this.companionProvider = companionProvider;
    }

    public async Task<T> GetAsync<T>(ICacheKey key, Func<Task<T>> fetchAsync, ICacheContext? cacheContext = null)
    {
        using var scope = logger.BeginMethodScope(() => new { key = key.ToLogString(), cacheContext });

        using Activity? activity = ActivitySource.StartActivity("CacheService.GetAsync");
        activity?.SetTag("cache.key", key.ToLogString());

        CacheMetrics.Calls.Add(1);

        if (cacheContext?.Enabled != true)
        {
            CacheMetrics.Sources.Add(1, MetricTags.Disabled);

            using (CacheMetrics.FetchDuration.StartMark(MetricTags.Disabled))
            {
                activity?.SetTag("cache.disabled", 1);
                return await fetchAsync();
            }
        }

        return await GetAsync(
            key,
            fetchAsync,
            cacheContext.MaxAge,
            cacheContext.InterfaceType,
            cacheContext.AbsoluteExpiration,
            cacheContext.SlidingExpiration);
    }

    // TODO Try get from redis unconditionally, after reading memory cache
    private async Task<TValue> GetAsync<TValue>(
        ICacheKey key,
        Func<Task<TValue>> fetchAsync,
        int? maxAge,
        Type? callerType,
        int? absExpiration = null,
        int? sldExpiration = null)
    {
        string keyLogString = key.ToLogString();

        using var scope = logger.BeginMethodScope(() => new { key = keyLogString });

        DateTime utcNow = DateTime.UtcNow;
        DateTime minimumCreationDate = GetMinimumCreationDate(scope, ref maxAge, callerType, utcNow);
        bool forceFetch = maxAge.Value <= 0 || minimumCreationDate >= utcNow;

        using TimerMark memoryMark = CacheMetrics.FetchDuration.CreateMark(MetricTags.Memory);
        memoryMark.DisableCommit = true;

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
            using (memoryMark.Start())
            using (ActivitySource.StartActivity("CacheService.GetFromMemory"))
            {
                valueEntry = Microsoft.Extensions.Caching.Memory.CacheExtensions.Get<ValueEntry<TValue>?>(memoryCache, key);
                externalEntry = discardExternalMiss ? null : externalMissDictionary.Get(key);
            }

            if (valueEntry is not null)
            {
                scope.LogDebug($"Cache entry found for key: {keyLogString}.");
            }
        }

        [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
        async Task<TValue> FetchAndSetValueAsync()
        {
            Stopwatch sw = Stopwatch.StartNew();

            CacheMetrics.Sources.Add(1, MetricTags.Miss);
            Activity.Current?.SetTag("cache.hit", 0);

            TValue value;
            using (CacheMetrics.FetchDuration.StartMark(MetricTags.Miss))
            {
                value = await fetchAsync();
            }

            long latencyMsec = sw.ElapsedMilliseconds;

            scope.LogDebug($"Fetched in {latencyMsec} ms");

            using (ActivitySource.StartActivity("CacheService.SetValue"))
            {
                SetValue(key, value, absExpiration, sldExpiration, /*skipPersist: skipPersist,*/ skipPublish: discardExternalMiss);
                return value;
            }
        }

        DateTime? localCreationDate = valueEntry?.CreationDate;

        if (externalEntry is var (othersCreationDate, locations) && !(othersCreationDate - cacheServiceOptions.LocalEntryTolerance <= localCreationDate))
        {
            if (othersCreationDate >= minimumCreationDate)
            {
                scope.LogDebug($"Key {keyLogString} is also available and up-to-date in other locations: {locations.GetLogString()}");

                HttpClient httpClient = httpClientFactory.CreateClient(nameof(CacheService));

                string rawKey;
                using (CacheMetrics.SerializationDuration.StartMark(MetricTags.CacheKey, MetricTags.Serialization))
                using (ActivitySource.StartActivity("CacheService.Serialize"))
                {
                    rawKey = CacheSerialization.SerializeToString(key);
                }

                HttpContent requestContent = new StringContent(rawKey, CacheSerialization.Encoding, "application/json");
                long keySerializedSize = requestContent.Headers.ContentLength!.Value;

                ConcurrentBag<string> invalidLocations = [];

                [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
                async Task<Optional<(TValue, double, long)>> GetFromCompanionAsync(string companionIp, CancellationToken ct)
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
                                    item = CacheSerialization.Deserialize<TValue>(contentStream);
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
                        return new Optional<(TValue, double, long)>((item, (double)latencyMsec / valueSerializedSize, valueSerializedSize));
                    }
                    catch (Exception e) when (e is InvalidOperationException or HttpRequestException || e is TaskCanceledException tce && tce.CancellationToken != ct)
                    {
                        mark.AddTags(MetricTags.NotFound);
                        invalidLocations.Add(companionIp);
                        scope.LogDebug($"Partial cache miss: Failed to retrieve value for {keyLogString} from companion {companionIp}");
                    }

                    return default;
                }

                [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
                async Task<Optional<(TValue, double, long)>> GetFromRedisAsync()
                {
                    if (redisDatabaseAccessor.Database is not { } redisDatabase)
                    {
                        return default;
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
                        return default;
                    }

                    mark.AddTags(MetricTags.Found);

                    ValueEntry<TValue> entry;
                    using (CacheMetrics.SerializationDuration.StartMark(MetricTags.CacheValue, MetricTags.Deserialization))
                    {
                        using (ActivitySource.StartActivity("CacheService.Deserialize"))
                        {
                            entry = CacheSerialization.Deserialize<ValueEntry<TValue>>((byte[])redisEntry!);
                        }
                    }

                    long latencyMsec = sw.ElapsedMilliseconds;

                    if (entry.CreationDate < minimumCreationDate)
                    {
                        scope.LogDebug($"Partial cache miss: Value for {keyLogString} found in Redis but CreationDate is invalid. Latency: {latencyMsec}");

                        invalidLocations.Add(RedisLocation);
                        _ = await redisDatabase.KeyDeleteAsync(redisKey);
                        return default;
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
                    Func<CancellationToken, Task<Optional<(TValue, double, long)>>> getFromLocationAsync)
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
                        static (l, kvs) => (Location: l, Latency: kvs.FirstOrDefault().Value ?? new Latency()))
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
                        })
                    .ToArray();

                Optional<(TValue Item, string Location, long ValueSerializedSize)> outputOpt;
                try
                {
                    outputOpt = await TaskExtensions.WhenAnyValid(
                        taskFactories.ToArray(),
                        cacheServiceOptions.CompanionPrefetchCount,
                        cacheServiceOptions.CompanionMaxParallelism,
                        // ReSharper disable once AsyncApostle.AsyncWait
                        isValid: static t => new ValueTask<bool>(!t.IsCompletedSuccessfully || !t.Result.IsUndefined));
                }
                catch (InvalidOperationException)
                {
                    outputOpt = default;
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

        memoryMark.DisableCommit = false;

        if (localCreationDate >= minimumCreationDate && valueEntry!.Data is { } data)
        {
            scope.LogDebug($"Cache hit: valid creation date (minimumCreationDate: '{minimumCreationDate:O}', newer entry CreationDate: '{localCreationDate.Value:O}')");

            memoryMark.AddTags(MetricTags.Found);
            CacheMetrics.Sources.Add(1, MetricTags.Memory);
            Activity.Current?.SetTag("cache.hit", 1);

            return data;
        }
        else
        {
            scope.LogDebug($"Cache miss: CreationDate validation failed (minimumCreationDate: '{minimumCreationDate:O}', older entry CreationDate: '{localCreationDate ?? DateTime.MinValue:O}').");

            memoryMark.AddTags(MetricTags.NotFound);

            return await FetchAndSetValueAsync();
        }
    }

    private void SetValue<TValue>(
        ICacheKey key,
        TValue value,
        int? absExpirationSec = null,
        int? sldExpirationSec = null,
        DateTime? creationDate = null,
        bool skipPublish = false)
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
        bool skipPublish = false)
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

        MemoryCacheEntryOptions entryOptions = new()
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
            });

        Microsoft.Extensions.Caching.Memory.CacheExtensions.Set(memoryCache, key, entry, entryOptions);

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
            });

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
                    redisKey = CacheSerialization.SerializeToBytes(key);
                }
            }

            byte[] rawEntry;
            using (CacheMetrics.SerializationDuration.StartMark(MetricTags.CacheValue, MetricTags.Serialization))
            {
                using (ActivitySource.StartActivity("CacheService.Serialize"))
                {
                    rawEntry = CacheSerialization.SerializeToBytes(entry);
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
                    await using MemoryStream valueStream = new(valueBytes);

                    using (CacheMetrics.SerializationDuration.StartMark(MetricTags.CacheValue, MetricTags.Serialization))
                    using (ActivitySource.StartActivity("CacheService.Serialize"))
                    {
                        try
                        {
                            CacheSerialization.SerializeToStream(value, valueType, valueStream);
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
            "cacheMiss");
    }

    private DateTime GetMinimumCreationDate(CodeSectionScope scope, [NotNull] ref int? maxAge, Type? callerType, DateTime utcNow)
    {
        int finalMaxAge = maxAge ?? cacheServiceOptions.DefaultMaxAge;

        if (httpContextAccessor.HttpContext is { } httpContext)
        {
            bool ExtractMaxAgeFromHeader(string headerName)
            {
                if (!httpContext.Request.Headers.TryGetValue(headerName, out StringValues headerMaxAges)
                    || !int.TryParse(headerMaxAges.LastOrDefault(), out int headerMaxAge))
                {
                    return false;
                }

                scope.LogDebug($"From request header: {headerName}={headerMaxAge}");
                if (headerMaxAge >= finalMaxAge)
                {
                    return false;
                }

                finalMaxAge = headerMaxAge;
                return true;
            }

            string? namespaceName = callerType?.Namespace;
            string[] maxAgeHeaderNames = namespaceName != null
                ? new[] { $"{namespaceName}.{callerType!.Name}.MaxAge", $"{namespaceName}.MaxAge", "MaxAge" }
                : new[] { "MaxAge" };

            foreach (string maxAgeHeaderName in maxAgeHeaderNames)
            {
                if (ExtractMaxAgeFromHeader(maxAgeHeaderName))
                    break;
            }
        }

        DateTime requestStartedOn =
            (httpContextAccessor.HttpContext?.Items.TryGetValue("RequestStartedOn", out var rawRequestStartedOn) == true
                ? rawRequestStartedOn as DateTime? : null)
            ?? utcNow;

        DateTime minimumCreationDate = requestStartedOn.Subtract(TimeSpan.FromSeconds(finalMaxAge));
        if (httpContextAccessor.HttpContext?.Request.Headers.TryGetValue("MinimumCreationDate", out StringValues headerMinimumCreationDates) == true
            && DateTime.TryParse(
                headerMinimumCreationDates.LastOrDefault(),
                null,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                out DateTime headerMinimumCreationDate))
        {
            scope.LogDebug($"From header: {nameof(minimumCreationDate)}={headerMinimumCreationDate}");

            if (headerMinimumCreationDate > minimumCreationDate)
            {
                minimumCreationDate = headerMinimumCreationDate;
            }
        }

        maxAge = finalMaxAge;
        return minimumCreationDate;
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
                    redisKey = (RedisKey)CacheSerialization.SerializeToString(key);
                }
            }

            _ = await redisDatabase.KeyDeleteAsync(redisKey.Prepend(cacheServiceOptions.RedisKeyPrefix));
        }
    }

    public bool TryGetDirectFromMemory(ICacheKey key, [NotNullWhen(true)] out Type? type, out object? value)
    {
        using var scope = logger.BeginMethodScope(() => new { key = key.ToLogString() });

        if (Microsoft.Extensions.Caching.Memory.CacheExtensions.Get<IValueEntry?>(memoryCache, key) is { } entry)
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
            });
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

        HttpClient httpClient = httpClientFactory.CreateClient(nameof(CacheService));

        string stringContent;
        using (CacheMetrics.SerializationDuration.StartMark(MetricTags.CacheValue, MetricTags.Serialization))
        using (ActivitySource.StartActivity("CacheService.Serialize"))
        {
            stringContent = CacheSerialization.SerializeToString(await makeObjAsync());
        }

        HttpContent content = new StringContent(stringContent, CacheSerialization.Encoding);

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
                });
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
            return underlying.TryGetValue(key, out Entry? entry) ? entry : null;
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
