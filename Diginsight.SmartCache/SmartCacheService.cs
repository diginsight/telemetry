﻿using Diginsight.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Diginsight.SmartCache;

public sealed class SmartCacheService : ISmartCacheService
{
    private readonly ILogger logger;
    private readonly IClassConfigurationGetter classConfigurationGetter;
    private readonly IClassConfigurationGetterProvider classConfigurationGetterProvider;
    private readonly ICacheCompanionProvider companionProvider;
    private readonly ISmartCacheServiceOptions smartCacheServiceOptions;
    private readonly TimeProvider timeProvider;
    private readonly IHttpContextAccessor? httpContextAccessor;

    private readonly IMemoryCache memoryCache;

    private readonly IReadOnlyDictionary<string, PassiveCacheLocation> passiveLocations;
    private readonly PassiveCacheLocation redisLocation;

    private readonly IDictionary<ICacheKey, ValueTuple> keys = new ConcurrentDictionary<ICacheKey, ValueTuple>();
    private readonly ExternalMissDictionary externalMissDictionary = new ();
    private readonly ConcurrentDictionary<string, Latency> locationLatencies = new ();

    private long memoryCacheSize = 0;

    public SmartCacheService(
        ILogger<SmartCacheService> logger,
        IClassConfigurationGetter classConfigurationGetter,
        IClassConfigurationGetterProvider classConfigurationGetterProvider,
        ICacheCompanionProvider companionProvider,
        IOptions<SmartCacheServiceOptions> smartCacheServiceOptions,
        IOptionsMonitor<MemoryCacheOptions> memoryCacheOptionsMonitor,
        ILoggerFactory loggerFactory,
        TimeProvider? timeProvider = null,
        IHttpContextAccessor? httpContextAccessor = null
    )
    {
        this.logger = logger;
        this.classConfigurationGetter = classConfigurationGetter;
        this.classConfigurationGetterProvider = classConfigurationGetterProvider;
        this.companionProvider = companionProvider;
        this.smartCacheServiceOptions = smartCacheServiceOptions.Value;
        this.timeProvider = timeProvider ?? TimeProvider.System;
        this.httpContextAccessor = httpContextAccessor;

        memoryCache = new MemoryCache(memoryCacheOptionsMonitor.Get(nameof(SmartCacheService)), loggerFactory);

        passiveLocations = companionProvider.PassiveLocations.ToDictionary(static x => x.Id);
        redisLocation = companionProvider.PassiveLocations.OfType<RedisCacheLocation>().Single();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static DateTime Truncate(DateTime timestamp)
    {
        return new DateTime(timestamp.Year, timestamp.Month, timestamp.Day, timestamp.Hour, timestamp.Minute, timestamp.Second);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [return: NotNullIfNotNull(nameof(timestamp))]
    private static DateTime? Truncate(DateTime? timestamp)
    {
        return timestamp is { } ts ? Truncate(ts) : null;
    }

    public async Task<T> GetAsync<T>(ICacheKey key, Func<Task<T>> fetchAsync, SmartCacheOperationOptions? operationOptions, Type? callerType)
    {
        callerType ??= RuntimeUtils.GetCaller().DeclaringType;
        operationOptions ??= new ();

        using Activity? activity = SmartCacheMetrics.ActivitySource.StartMethodActivity(logger, new { key, operationOptions, callerType });

        CacheKeyHolder keyHolder = new CacheKeyHolder(key);

        SmartCacheMetrics.Instruments.Calls.Add(1);

        if (operationOptions.Disabled)
        {
            SmartCacheMetrics.Instruments.Sources.Add(1, SmartCacheMetrics.Tags.Type.Disabled);

            using (SmartCacheMetrics.Instruments.FetchDuration.StartLap(SmartCacheMetrics.Tags.Type.Disabled))
            {
                activity?.SetTag("cache.disabled", 1);
                return await fetchAsync();
            }
        }

        TimeSpan? maxAge = operationOptions.MaxAge;
        DateTime timestamp = Truncate(timeProvider.GetUtcNow().UtcDateTime);
        DateTime minimumCreationDate = GetMinimumCreationDate(ref maxAge, callerType, timestamp);
        bool forceFetch = maxAge.Value <= TimeSpan.Zero || minimumCreationDate >= timestamp;

        return await GetAsync(
            keyHolder,
            fetchAsync,
            timestamp,
            forceFetch ? null : minimumCreationDate,
            operationOptions.AbsoluteExpiration,
            operationOptions.SlidingExpiration
        );
    }

    private async Task<TValue> GetAsync<TValue>(
        CacheKeyHolder keyHolder,
        Func<Task<TValue>> fetchAsync,
        DateTime timestamp,
        DateTime? maybeMinimumCreationDate,
        TimeSpan? absExpiration = null,
        TimeSpan? sldExpiration = null
    )
    {
        using Activity? activity = SmartCacheMetrics.ActivitySource.StartMethodActivity(
            logger, new { keyHolder.Key, timestamp, maybeMinimumCreationDate, absExpiration, sldExpiration }
        );

        using TimerLap memoryLap = SmartCacheMetrics.Instruments.FetchDuration.CreateLap(SmartCacheMetrics.Tags.Type.Memory);
        memoryLap.DisableCommit = true;

        ValueEntry<TValue>? localEntry;
        ExternalMissDictionary.Entry? externalEntry;

        bool discardExternalMiss = classConfigurationGetter.Get("DiscardExternalMiss", false);

        if (maybeMinimumCreationDate is null)
        {
            localEntry = null;
            externalEntry = null;
        }
        else
        {
            using (memoryLap.Start())
            using (SmartCacheMetrics.ActivitySource.StartActivity(logger, $"{nameof(SmartCacheService)}.GetFromMemory"))
            {
                localEntry = memoryCache.Get<ValueEntry<TValue>?>(keyHolder.Key);
                externalEntry = discardExternalMiss ? null : externalMissDictionary.Get(keyHolder.Key);
            }

            if (localEntry is not null)
            {
                logger.LogDebug("Cache entry found");
            }
        }

        async Task<TValue> FetchAndSetValueAsync([SuppressMessage("ReSharper", "VariableHidesOuterVariable")] Activity? activity)
        {
            SmartCacheMetrics.Instruments.Sources.Add(1, SmartCacheMetrics.Tags.Type.Miss);
            activity?.SetTag("cache.hit", 0);

            TValue value;
            StrongBox<double> latencyMsecBox = new ();
            using (SmartCacheMetrics.Instruments.FetchDuration.StartLap(latencyMsecBox, SmartCacheMetrics.Tags.Type.Miss))
            {
                value = await fetchAsync();
            }

            long latencyMsec = (long)latencyMsecBox.Value;

            logger.LogDebug("Fetched in {LatencyMsec} ms", latencyMsec);

            using (SmartCacheMetrics.ActivitySource.StartActivity(logger, $"{nameof(SmartCacheService)}.SetValue"))
            {
                SetValue(keyHolder, value, timestamp, absExpiration, sldExpiration, discardExternalMiss);
                return value;
            }
        }

        DateTime? localCreationDate = localEntry?.CreationDate;

        if (externalEntry is var (othersCreationDate, locationIds) && !(othersCreationDate - smartCacheServiceOptions.LocalEntryTolerance <= localCreationDate))
        {
            DateTime minimumCreationDate = maybeMinimumCreationDate!.Value;
            if (othersCreationDate >= minimumCreationDate)
            {
                logger.LogDebug("Key is also available and up-to-date in other locations: {LocationIds}", locationIds);

                ConcurrentBag<string> invalidLocations = [ ];

                IReadOnlyDictionary<string, CacheLocation> locations = (await companionProvider.GetCompanionsAsync())
                    .Concat<CacheLocation>(passiveLocations.Values)
                    .ToDictionary(static x => x.Id);

                IEnumerable<Func<CancellationToken, Task<(TValue, long, KeyValuePair<string, object?>)?>>> taskFactories = locationIds
                    .GroupJoin(
                        locationLatencies,
                        static l => l,
                        static kv => kv.Key,
                        static (l, kvs) => (LocationId: l, Latency: kvs.FirstOrDefault().Value ?? new Latency())
                    )
                    .OrderBy(static kv => kv.Latency)
                    .Select(static kv => kv.LocationId)
                    .Select(
                        Func<CancellationToken, Task<(TValue, long, KeyValuePair<string, object?>)?>> (locationId) =>
                        {
                            if (!locations.TryGetValue(locationId, out CacheLocation? location))
                            {
                                return static _ => Task.FromResult<(TValue, long, KeyValuePair<string, object?>)?>(null);
                            }

                            return async ct =>
                            {
                                (TValue Item, long ValueSerializedSize, double RelativeLatency)? maybeOutput =
                                    await location.GetAsync<TValue>(keyHolder, minimumCreationDate, () => invalidLocations.Add(locationId), ct);

                                if (maybeOutput is { } output)
                                {
                                    Latency latency = locationLatencies.GetOrAdd(locationId, static _ => new Latency());
                                    latency.Add(output.RelativeLatency);

                                    return (output.Item, output.ValueSerializedSize, location.MetricTag);
                                }
                                else
                                {
                                    if (invalidLocations.Contains(locationId) && location is CacheCompanion)
                                    {
                                        locationLatencies.TryRemove(locationId, out _);
                                    }

                                    return null;
                                }
                            };
                        }
                    )
                    .ToArray();

                (TValue, long, KeyValuePair<string, object?>)? maybeOutput;
                try
                {
                    maybeOutput = await TaskUtils.WhenAnyValid(
                        taskFactories.ToArray(),
                        smartCacheServiceOptions.CompanionPrefetchCount,
                        smartCacheServiceOptions.CompanionMaxParallelism,
                        // ReSharper disable once AsyncApostle.AsyncWait
                        isValid: static t => new ValueTask<bool>(t.Status != TaskStatus.RanToCompletion || t.Result is not null)
                    );
                }
                catch (InvalidOperationException)
                {
                    maybeOutput = default;
                }
                finally
                {
                    if (invalidLocations.Any())
                    {
                        externalMissDictionary.RemoveSub(keyHolder.Key, invalidLocations);
                    }
                }

                if (maybeOutput is var (item, valueSerializedSize, metricTag))
                {
                    SmartCacheMetrics.Instruments.KeySerializedSize.Record(keyHolder.GetAsBytes().LongLength, metricTag);
                    SmartCacheMetrics.Instruments.ValueSerializedSize.Record(valueSerializedSize, metricTag);
                    SmartCacheMetrics.Instruments.Sources.Add(1, metricTag);

                    SetValue(keyHolder, item, othersCreationDate, absExpiration, sldExpiration, discardExternalMiss);
                    return item!;
                }
            }
            else
            {
                logger.LogDebug(
                    "Cache miss: creation date validation failed (minimum: {MinimumCreationDate:O}, older: {LocalCreationDate:O})",
                    minimumCreationDate,
                    localCreationDate ?? DateTime.MinValue
                );
            }

            return await FetchAndSetValueAsync(activity);
        }

        memoryLap.DisableCommit = false;

        if (localCreationDate >= maybeMinimumCreationDate && localEntry!.Data is { } data)
        {
            logger.LogDebug(
                "Cache hit: valid creation date (minimum: {MaybeMinimumCreationDate:O}, newer: {LocalCreationDate:O})",
                maybeMinimumCreationDate,
                localCreationDate.Value
            );

            memoryLap.AddTags(SmartCacheMetrics.Tags.Found.True);
            SmartCacheMetrics.Instruments.Sources.Add(1, SmartCacheMetrics.Tags.Type.Memory);
            activity?.SetTag("cache.hit", 1);

            return data;
        }
        else
        {
            logger.LogDebug(
                "Cache miss: creation date validation failed (minimum: {MaybeMinimumCreationDate:O}, older: {LocalCreationDate})",
                maybeMinimumCreationDate,
                localCreationDate ?? DateTime.MinValue
            );

            memoryLap.AddTags(SmartCacheMetrics.Tags.Found.False);

            return await FetchAndSetValueAsync(activity);
        }
    }

    private void SetValue<TValue>(
        CacheKeyHolder keyHolder,
        TValue value,
        DateTime creationDate,
        TimeSpan? absExpiration = null,
        TimeSpan? sldExpiration = null,
        bool skipPublish = false
    )
    {
        SetValue(keyHolder, typeof(TValue), value, creationDate, absExpiration, sldExpiration, skipPublish);
    }

    private void SetValue(
        CacheKeyHolder keyHolder,
        Type valueType,
        object? value,
        DateTime creationDate,
        TimeSpan? absExpiration = null,
        TimeSpan? sldExpiration = null,
        bool skipPublish = false
    )
    {
        using Activity? activity = SmartCacheMetrics.ActivitySource.StartMethodActivity(
            logger, new { key = keyHolder.Key, valueType, creationDate, absExpiration, sldExpiration, skipPublish }
        );

        ICacheKey key = keyHolder.Key;

        keys[key] = default;
        RemoveExternalMiss(keyHolder);

        IValueEntry entry = ValueEntry.Create(value, valueType, creationDate);

        TimeSpan finalAbsExpiration = absExpiration ?? smartCacheServiceOptions.AbsoluteExpiration;

        if (classConfigurationGetter.Get("RedisOnlyCache", false))
        {
            WriteToLocation(redisLocation, keyHolder, entry, finalAbsExpiration, skipPublish);
            return;
        }

        TimeSpan finalSldExpiration = new TimeSpan(
            Math.Min((sldExpiration ?? smartCacheServiceOptions.SlidingExpiration).Ticks, finalAbsExpiration.Ticks)
        );

        long keySize;
        try
        {
            using (SmartCacheMetrics.Instruments.SizeComputationDuration.StartLap(SmartCacheMetrics.Tags.Subject.Key))
            {
                keySize = Size.Get(key);
            }
            SmartCacheMetrics.Instruments.KeyObjectSize.Record(keySize);
        }
        catch (Exception)
        {
            keySize = 0;
        }

        long valueSize;
        using (SmartCacheMetrics.Instruments.SizeComputationDuration.StartLap(SmartCacheMetrics.Tags.Subject.Value))
        {
            valueSize = Size.Get(value);
        }
        SmartCacheMetrics.Instruments.ValueObjectSize.Record(valueSize);

        long size = keySize + valueSize;

        CacheItemPriority priority =
            size >= smartCacheServiceOptions.LowPrioritySizeThreshold ? CacheItemPriority.Low
            : size >= smartCacheServiceOptions.MidPrioritySizeThreshold ? CacheItemPriority.Normal
            : CacheItemPriority.High;

        MemoryCacheEntryOptions entryOptions = new ()
        {
            AbsoluteExpirationRelativeToNow = finalAbsExpiration,
            SlidingExpiration = finalSldExpiration,
            Size = size,
            Priority = priority,
        };

        entryOptions.RegisterPostEvictionCallback(
            (k, v, r, _) =>
            {
                Interlocked.Add(ref memoryCacheSize, -size);
                SmartCacheMetrics.Instruments.TotalSize.Add(-size);

                OnEvicted(new CacheKeyHolder((ICacheKey)k), (IValueEntry)v!, r, finalAbsExpiration);
            }
        );

        memoryCache.Set(key, entry, entryOptions);

        Interlocked.Add(ref memoryCacheSize, size);
        SmartCacheMetrics.Instruments.TotalSize.Add(size);

        if (!skipPublish)
        {
            PublishMiss(keyHolder, creationDate, (value, valueType), null);
        }
    }

    private void OnEvicted(CacheKeyHolder keyHolder, IValueEntry entry, EvictionReason reason, TimeSpan expiration)
    {
        SmartCacheMetrics.Instruments.Evictions.Add(
            1,
            reason switch
            {
                EvictionReason.Removed => SmartCacheMetrics.Tags.Eviction.Removed,
                EvictionReason.Replaced => SmartCacheMetrics.Tags.Eviction.Replaced,
                EvictionReason.Expired or EvictionReason.TokenExpired => SmartCacheMetrics.Tags.Eviction.Expired,
                EvictionReason.Capacity => SmartCacheMetrics.Tags.Eviction.Capacity,
                EvictionReason.None => throw new InvalidOperationException($"unexpected {nameof(EvictionReason)}"),
                _ => throw new ArgumentOutOfRangeException($"unrecognized {nameof(EvictionReason)}"),
            }
        );

        if (reason is EvictionReason.None or EvictionReason.Replaced)
        {
            return;
        }

        using Activity? activity = SmartCacheMetrics.ActivitySource.StartMethodActivity(logger, new { key = keyHolder.Key, reason, expiration });

        keys.Remove(keyHolder.Key);

        if (reason != EvictionReason.Capacity)
        {
            return;
        }

        WriteToLocation(redisLocation, keyHolder, entry, expiration);
    }

    private void WriteToLocation(PassiveCacheLocation location, CacheKeyHolder keyHolder, IValueEntry entry, TimeSpan expiration, bool skipPublish = false)
    {
        location.WriteAndForget(
            keyHolder,
            entry,
            expiration,
            skipPublish
                ? () => PublishMissAsync(keyHolder, entry.CreationDate, null, location.Id)
                : static () => Task.CompletedTask
        );
    }

    private void PublishMiss(CacheKeyHolder keyHolder, DateTime creationDate, (object?, Type)? valueHolder, string? locationId)
    {
        _ = Task.Run(() => PublishMissAsync(keyHolder, creationDate, valueHolder, locationId));
    }

    private async Task PublishMissAsync(CacheKeyHolder keyHolder, DateTime creationDate, (object?, Type)? valueHolder, string? locationId)
    {
        using Activity? activity = SmartCacheMetrics.ActivitySource.StartMethodActivity(
            logger, new { key = keyHolder.Key, creationDate, locationId }
        );

        if (locationId is not null)
        {
            using (SmartCacheMetrics.ActivitySource.StartActivity(logger, $"{nameof(SmartCacheService)}.SetMissValue"))
            {
                externalMissDictionary.Add(keyHolder.Key, creationDate, locationId);
            }
        }

        IEnumerable<CacheCompanion> companions = await companionProvider.GetCompanionsAsync();
        if (!companions.Any())
        {
            return;
        }

        (Type, object?)? valueTuple;
        if (valueHolder is var (value, valueType) && smartCacheServiceOptions.MissValueSizeThreshold is > 0 and var size)
        {
            byte[] valueBytes = new byte[size];
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            await using MemoryStream valueStream = new (valueBytes);
#else
            using MemoryStream valueStream = new (valueBytes);
#endif

            using Activity? serializeActivity = SmartCacheMetrics.ActivitySource.StartActivity(logger, $"{nameof(SmartCacheService)}.Serialize");
            serializeActivity?.WithDurationMetric(
                SmartCacheMetrics.Instruments.SerializationDuration.Underlying,
                SmartCacheMetrics.Tags.Subject.Value,
                SmartCacheMetrics.Tags.Operation.Serialization
            );

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
        else
        {
            valueTuple = null;
        }

        string selfLocationId = companionProvider.SelfLocationId;
        CacheMissDescriptor descriptor = new (selfLocationId, keyHolder.Key, creationDate, locationId ?? selfLocationId, valueTuple);
        CachePayloadHolder<CacheMissDescriptor> descriptorHolder = new (descriptor, SmartCacheMetrics.Tags.Subject.Value);

        foreach (CacheCompanion companion in companions)
        {
            companion.PublishCacheMissAndForget(descriptorHolder);
        }
    }

    private DateTime GetMinimumCreationDate([NotNull] ref TimeSpan? maxAge, Type callerType, DateTime timestamp)
    {
        IClassConfigurationGetter callerClassConfigurationGetter = classConfigurationGetterProvider.GetFor(callerType);

        static bool TryConvertMaxAgeSecs(string? str, out int maxAgeSecs)
        {
            if (int.TryParse(str, out maxAgeSecs))
            {
                return true;
            }

            maxAgeSecs = default;
            return false;
        }

        TimeSpan finalMaxAge = maxAge ?? smartCacheServiceOptions.DefaultMaxAge;
        if (callerClassConfigurationGetter.TryGet("MaxAge", out int outerMaxAgeSecs, TryConvertMaxAgeSecs))
        {
            TimeSpan outerMaxAge = TimeSpan.FromSeconds(outerMaxAgeSecs);
            if (outerMaxAge < finalMaxAge)
            {
                finalMaxAge = outerMaxAge;
            }
        }

        DateTime requestStartedOn =
            Truncate(
                httpContextAccessor?.HttpContext?.Items.TryGetValue("RequestStartedOn", out var rawRequestStartedOn) == true
                    ? rawRequestStartedOn as DateTime? : null
            )
            ?? timestamp;

        static bool TryConvertMinimumCreationDate(string? str, out DateTime minimumCreationDate)
        {
            if (DateTime.TryParse(str, null, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out minimumCreationDate))
            {
                return true;
            }

            minimumCreationDate = default;
            return false;
        }

        DateTime minimumCreationDate = requestStartedOn - finalMaxAge;
        if (callerClassConfigurationGetter.TryGet("MinimumCreationDate", out DateTime outerMinimumCreationDate, TryConvertMinimumCreationDate))
        {
            if (outerMinimumCreationDate > minimumCreationDate)
            {
                minimumCreationDate = outerMinimumCreationDate;
            }
        }

        maxAge = finalMaxAge;
        return minimumCreationDate;
    }

    public bool TryGetDirectFromMemory(ICacheKey key, [NotNullWhen(true)] out Type? type, out object? value)
    {
        using Activity? activity = SmartCacheMetrics.ActivitySource.StartMethodActivity(logger, new { key });

        if (memoryCache.Get<IValueEntry?>(key) is { } entry)
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
        if (descriptor.Emitter != companionProvider.SelfLocationId)
        {
            Invalidate(descriptor.Rule, false);
        }
    }

    private void Invalidate(IInvalidationRule invalidationRule, bool broadcast)
    {
        using Activity? activity = SmartCacheMetrics.ActivitySource.StartMethodActivity(logger, () => new { invalidationRule, broadcast });

        ICollection<Func<Task>> invalidationCallbacks = new List<Func<Task>>();

        void CoreInvalidate(IEnumerable<ICacheKey> ks, Action<ICacheKey> remove)
        {
            foreach (ICacheKey k in ks.ToArray())
            {
                // ReSharper disable once SuspiciousTypeConversion.Global
                if (k is not IInvalidatable invalidatable ||
                    !invalidatable.IsInvalidatedBy(invalidationRule, out var invalidationCallback))
                {
                    continue;
                }

                logger.LogDebug("Invalidating cache key");

                remove(k);
                if (invalidationCallback is not null)
                {
                    invalidationCallbacks.Add(invalidationCallback);
                }
            }
        }

        using (SmartCacheMetrics.ActivitySource.StartActivity(logger, $"{nameof(SmartCacheService)}.Invalidate"))
        {
            CoreInvalidate(keys.Keys, memoryCache.Remove);
            CoreInvalidate(externalMissDictionary.Keys, k => RemoveExternalMiss(new CacheKeyHolder(k)));
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
            IEnumerable<CacheCompanion> companions = await companionProvider.GetCompanionsAsync();
            if (!companions.Any())
            {
                return;
            }

            InvalidationDescriptor descriptor = new (companionProvider.SelfLocationId, invalidationRule);
            CachePayloadHolder<InvalidationDescriptor> descriptorHolder = new (descriptor, SmartCacheMetrics.Tags.Subject.Value);
            foreach (CacheCompanion companion in companions)
            {
                companion.PublishInvalidationAndForget(descriptorHolder);
            }
        }
    }

    public void AddExternalMiss(CacheMissDescriptor descriptor)
    {
        (string emitter,
            ICacheKey key,
            DateTime timestamp,
            string location,
            Type? valueType) = descriptor;

        if (emitter == companionProvider.SelfLocationId)
        {
            return;
        }

        if (valueType is not null)
        {
            SetValue(new CacheKeyHolder(key), valueType, descriptor.Value, timestamp, skipPublish: true);
        }
        else
        {
            externalMissDictionary.Add(key, timestamp, location);
        }
    }

    private void RemoveExternalMiss(CacheKeyHolder keyHolder)
    {
        foreach (string locationId in externalMissDictionary.Remove(keyHolder.Key))
        {
            if (passiveLocations.TryGetValue(locationId, out PassiveCacheLocation? passiveLocation))
            {
                passiveLocation.DeleteAndForget(keyHolder);
            }
        }
    }

    private sealed class ExternalMissDictionary
    {
        private readonly ConcurrentDictionary<ICacheKey, Entry> underlying = new ConcurrentDictionary<ICacheKey, Entry>();

        public IEnumerable<ICacheKey> Keys => underlying.Keys;

        private readonly object lockObject = new object();

        public Entry? Get(ICacheKey key)
        {
            // ReSharper disable once CanSimplifyDictionaryTryGetValueWithGetValueOrDefault
            return underlying.TryGetValue(key, out Entry? entry) ? entry : null;
        }

        public IEnumerable<string> Remove(ICacheKey key)
        {
            return underlying.TryRemove(key, out Entry? entry) ? entry.Locations : Enumerable.Empty<string>();
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
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                            ManagedTypes.TryAdd(type, default);
#else
                            if (!ManagedTypes.ContainsKey(type))
                            {
                                ManagedTypes[type] = default;
                            }
#endif
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
                            _ => throw new ArgumentException($"unsupported {nameof(JToken)} subclass"),
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