using Diginsight.CAOptions;
using Diginsight.Diagnostics;
using Diginsight.SmartCache.Externalization;
using Diginsight.SmartCache.Externalization.Redis;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Diginsight.SmartCache;

internal sealed class SmartCache : ISmartCache
{
    private readonly ILogger logger;
    private readonly ICacheCompanion companion;
    private readonly IClassAwareOptionsMonitor<SmartCacheCoreOptions> coreOptionsMonitor;
    private readonly IClassAwareOptionsMonitor<OnTheFlySmartCacheCoreOptions> otfCoreOptionsMonitor;
    private readonly TimeProvider timeProvider;

    private readonly IMemoryCache memoryCache;

    private readonly IReadOnlyDictionary<string, PassiveCacheLocation> passiveLocations;
    private readonly PassiveCacheLocation? redisLocation;

    private readonly IDictionary<ICacheKey, ValueTuple> keys = new ConcurrentDictionary<ICacheKey, ValueTuple>();
    private readonly ExternalMissDictionary externalMissDictionary = new ();
    private readonly ConcurrentDictionary<string, Latency> locationLatencies = new ();

    private long memoryCacheSize = 0;

    public SmartCache(
        ILogger<SmartCache> logger,
        ICacheCompanion companion,
        IClassAwareOptionsMonitor<SmartCacheCoreOptions> coreOptionsMonitor,
        IClassAwareOptionsMonitor<OnTheFlySmartCacheCoreOptions> otfCoreOptionsMonitor,
        IOptionsMonitor<MemoryCacheOptions> memoryCacheOptionsMonitor,
        ILoggerFactory loggerFactory,
        TimeProvider? timeProvider = null
    )
    {
        this.logger = logger;
        this.companion = companion;
        this.coreOptionsMonitor = coreOptionsMonitor;
        this.otfCoreOptionsMonitor = otfCoreOptionsMonitor;
        this.timeProvider = timeProvider ?? TimeProvider.System;

        memoryCache = new MemoryCache(memoryCacheOptionsMonitor.Get(nameof(SmartCache)), loggerFactory);

        passiveLocations = companion.PassiveLocations.ToDictionary(static x => x.Id);
        redisLocation = passiveLocations.Values.OfType<RedisCacheLocation>().SingleOrDefault();
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<T> GetAsync<T>(ICacheKey key, Func<Task<T>> fetchAsync, SmartCacheOperationOptions? operationOptions, Type? callerType)
    {
        callerType ??= RuntimeUtils.GetCaller().DeclaringType;
        operationOptions ??= new ();

        using Activity? activity = SmartCacheMetrics.ActivitySource.StartMethodActivity(logger, new { key, operationOptions, callerType });

        CacheKeyHolder keyHolder = new CacheKeyHolder(key, logger);

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

        ISmartCacheCoreOptions coreOptions = coreOptionsMonitor.CurrentValue;

        ValueEntry<TValue>? localEntry;
        ExternalMissDictionary.Entry? externalEntry;

        bool discardExternalMiss = coreOptions.DiscardExternalMiss;

        if (maybeMinimumCreationDate is null)
        {
            localEntry = null;
            externalEntry = null;
        }
        else
        {
            using (memoryLap.Start())
            using (SmartCacheMetrics.ActivitySource.StartRichActivity(logger, $"{nameof(SmartCache)}.GetFromMemory"))
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

            using (SmartCacheMetrics.ActivitySource.StartRichActivity(logger, $"{nameof(SmartCache)}.SetValue"))
            {
                SetValue(keyHolder, value, timestamp, absExpiration, sldExpiration, discardExternalMiss);
                return value;
            }
        }

        DateTime? localCreationDate = localEntry?.CreationDate;

        if (externalEntry is var (othersCreationDate, locationIds) && !(othersCreationDate - coreOptions.LocalEntryTolerance <= localCreationDate))
        {
            DateTime minimumCreationDate = maybeMinimumCreationDate!.Value;
            if (othersCreationDate >= minimumCreationDate)
            {
                logger.LogDebug("Key is also available and up-to-date in other locations: {LocationIds}", locationIds);

                ConcurrentBag<string> invalidLocations = [ ];

                IReadOnlyDictionary<string, CacheLocation> locations = (await companion.GetActiveLocationsAsync(locationIds))
                    .Concat<CacheLocation>(passiveLocations.Values)
                    .ToDictionary(static x => x.Id);

                IEnumerable<Func<CancellationToken, Task<(CacheLocationOutput<TValue>, KeyValuePair<string, object?>)?>>> taskFactories = locationIds
                    .GroupJoin(
                        locationLatencies,
                        static l => l,
                        static kv => kv.Key,
                        static (l, kvs) => (LocationId: l, Latency: kvs.FirstOrDefault().Value ?? new Latency())
                    )
                    .OrderBy(static kv => kv.Latency)
                    .Select(static kv => kv.LocationId)
                    .Select(
                        Func<CancellationToken, Task<(CacheLocationOutput<TValue>, KeyValuePair<string, object?>)?>> (locationId) =>
                        {
                            if (!locations.TryGetValue(locationId, out CacheLocation? location))
                            {
                                return static _ => Task.FromResult<(CacheLocationOutput<TValue>, KeyValuePair<string, object?>)?>(null);
                            }

                            return async ct =>
                            {
                                CacheLocationOutput<TValue>? maybeOutput =
                                    await location.GetAsync<TValue>(keyHolder, minimumCreationDate, () => invalidLocations.Add(locationId), ct);

                                if (maybeOutput is not { } output)
                                {
                                    if (invalidLocations.Contains(locationId) && location is ActiveCacheLocation)
                                    {
                                        locationLatencies.TryRemove(locationId, out _);
                                    }

                                    return null;
                                }

                                Latency latency = locationLatencies.GetOrAdd(locationId, static _ => new Latency());
                                latency.Add(output.LatencyMsec / output.ValueSerializedSize);

                                return (output, location.MetricTag);
                            };
                        }
                    )
                    .ToArray();

                (CacheLocationOutput<TValue> Output, KeyValuePair<string, object?> MetricTag)? maybeOutputTagged;
                try
                {
                    maybeOutputTagged = await TaskUtils.WhenAnyValid(
                        taskFactories.ToArray(),
                        coreOptions.CompanionPrefetchCount,
                        coreOptions.CompanionMaxParallelism,
                        // ReSharper disable once AsyncApostle.AsyncWait
                        isValid: static t => new ValueTask<bool>(t.Status != TaskStatus.RanToCompletion || t.Result is not null)
                    );
                }
                catch (InvalidOperationException)
                {
                    maybeOutputTagged = default;
                }
                finally
                {
                    if (invalidLocations.Any())
                    {
                        externalMissDictionary.RemoveSub(keyHolder.Key, invalidLocations);
                    }
                }

                if (maybeOutputTagged is var ((item, valueSerializedSize, latencyMsec), metricTag))
                {
                    SmartCacheMetrics.Instruments.KeySerializedSize.Record(keyHolder.GetAsBytes().LongLength, metricTag);
                    SmartCacheMetrics.Instruments.ValueSerializedSize.Record(valueSerializedSize, metricTag);
                    SmartCacheMetrics.Instruments.Sources.Add(1, metricTag);
                    SmartCacheMetrics.Instruments.CompanionFetchDuration.Underlying.Record(latencyMsec, metricTag);
                    SmartCacheMetrics.Instruments.CompanionFetchRelativeDuration.Record(latencyMsec / valueSerializedSize * 1000, metricTag);

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
                "Cache miss: creation date validation failed (minimum: {MaybeMinimumCreationDate:O}, older: {LocalCreationDate:O})",
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
        bool skipNotify = false
    )
    {
        SetValue(keyHolder, typeof(TValue), value, creationDate, absExpiration, sldExpiration, skipNotify);
    }

    private void SetValue(
        CacheKeyHolder keyHolder,
        Type valueType,
        object? value,
        DateTime creationDate,
        TimeSpan? absExpiration = null,
        TimeSpan? sldExpiration = null,
        bool skipNotify = false
    )
    {
        using Activity? activity = SmartCacheMetrics.ActivitySource.StartMethodActivity(
            logger, new { key = keyHolder.Key, valueType, creationDate, absExpiration, sldExpiration, skipNotify }
        );

        ISmartCacheCoreOptions coreOptions = coreOptionsMonitor.CurrentValue;

        ICacheKey key = keyHolder.Key;

        keys[key] = default;
        RemoveExternalMiss(keyHolder);

        IValueEntry entry = ValueEntry.Create(value, valueType, creationDate);

        static TimeSpan? ToInfinity(TimeSpan ts) => ts == TimeSpan.MaxValue ? null : ts;

        TimeSpan finalAbsExpiration = absExpiration ?? coreOptions.AbsoluteExpiration;
        TimeSpan? inftyFinalAbsExpiration = ToInfinity(finalAbsExpiration);

        if (coreOptions.RedisOnlyCache && redisLocation is not null)
        {
            WriteToLocation(redisLocation, keyHolder, entry, inftyFinalAbsExpiration, skipNotify);
            return;
        }

        TimeSpan? inftyFinalSldExpiration = ToInfinity(
            new TimeSpan(Math.Min((sldExpiration ?? coreOptions.SlidingExpiration).Ticks, finalAbsExpiration.Ticks))
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
            size >= coreOptions.LowPrioritySizeThreshold ? CacheItemPriority.Low
            : size >= coreOptions.MidPrioritySizeThreshold ? CacheItemPriority.Normal
            : CacheItemPriority.High;

        MemoryCacheEntryOptions entryOptions = new ()
        {
            AbsoluteExpirationRelativeToNow = inftyFinalAbsExpiration,
            SlidingExpiration = inftyFinalSldExpiration,
            Size = size,
            Priority = priority,
        };

        entryOptions.RegisterPostEvictionCallback(
            (k, v, r, _) =>
            {
                Interlocked.Add(ref memoryCacheSize, -size);
                SmartCacheMetrics.Instruments.TotalSize.Add(-size);

                OnEvicted(new CacheKeyHolder((ICacheKey)k, logger), (IValueEntry)v!, r, inftyFinalAbsExpiration);
            }
        );

        memoryCache.Set(key, entry, entryOptions);

        Interlocked.Add(ref memoryCacheSize, size);
        SmartCacheMetrics.Instruments.TotalSize.Add(size);

        if (!skipNotify)
        {
            NotifyMiss(keyHolder, creationDate, (value, valueType), null);
        }
    }

    private void OnEvicted(CacheKeyHolder keyHolder, IValueEntry entry, EvictionReason reason, TimeSpan? expiration)
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

        if (redisLocation is not null)
        {
            WriteToLocation(redisLocation, keyHolder, entry, expiration);
        }
    }

    private void WriteToLocation(PassiveCacheLocation location, CacheKeyHolder keyHolder, IValueEntry entry, TimeSpan? expiration, bool skipNotify = false)
    {
        location.WriteAndForget(
            keyHolder,
            entry,
            expiration,
            skipNotify
                ? () => NotifyMissAsync(keyHolder, entry.CreationDate, null, location.Id)
                : static () => Task.CompletedTask
        );
    }

    private void NotifyMiss(CacheKeyHolder keyHolder, DateTime creationDate, (object?, Type)? valueHolder, string? locationId)
    {
        _ = Task.Run(() => NotifyMissAsync(keyHolder, creationDate, valueHolder, locationId));
    }

    private async Task NotifyMissAsync(CacheKeyHolder keyHolder, DateTime creationDate, (object?, Type)? valueHolder, string? locationId)
    {
        using Activity? activity = SmartCacheMetrics.ActivitySource.StartMethodActivity(
            logger, new { key = keyHolder.Key, creationDate, locationId }
        );

        if (locationId is not null)
        {
            using (SmartCacheMetrics.ActivitySource.StartRichActivity(logger, $"{nameof(SmartCache)}.SetMissValue"))
            {
                externalMissDictionary.Add(keyHolder.Key, creationDate, locationId);
            }
        }

        IEnumerable<CacheEventNotifier> eventNotifiers = await companion.GetAllEventNotifiersAsync();
        if (!eventNotifiers.Any())
        {
            return;
        }

        ISmartCacheCoreOptions coreOptions = coreOptionsMonitor.CurrentValue;

        (Type, object?)? valueTuple;
        if (valueHolder is var (value, valueType) && coreOptions.MissValueSizeThreshold is > 0 and var size)
        {
            byte[] valueBytes = new byte[size];
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            await using MemoryStream valueStream = new (valueBytes);
#else
            using MemoryStream valueStream = new (valueBytes);
#endif

            using (SmartCacheMetrics.StartSerializeActivity(logger, SmartCacheMetrics.Tags.Subject.Value))
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

        string selfLocationId = companion.SelfLocationId;
        CacheMissDescriptor descriptor = new (selfLocationId, keyHolder.Key, creationDate, locationId ?? selfLocationId, valueTuple);
        CachePayloadHolder<CacheMissDescriptor> descriptorHolder = new (descriptor, logger, SmartCacheMetrics.Tags.Subject.Value);

        foreach (CacheEventNotifier eventNotifier in eventNotifiers)
        {
            eventNotifier.NotifyCacheMissAndForget(descriptorHolder);
        }
    }

    private DateTime GetMinimumCreationDate([NotNull] ref TimeSpan? maxAge, Type callerType, DateTime timestamp)
    {
        ISmartCacheCoreOptions coreOptions = coreOptionsMonitor.Get(Options.DefaultName, callerType);
        IOnTheFlySmartCacheCoreOptions otfCoreOptions = otfCoreOptionsMonitor.Get(Options.DefaultName, callerType);

        TimeSpan finalMaxAge = maxAge ?? coreOptions.MaxAge;

        DateTime minimumCreationDate;
        try
        {
            minimumCreationDate = timestamp - finalMaxAge;
        }
        catch (ArgumentOutOfRangeException)
        {
            minimumCreationDate = DateTime.MinValue;
        }

        if (otfCoreOptions.MinimumCreationDate is { } otfMinimumCreationDate && otfMinimumCreationDate > minimumCreationDate)
        {
            minimumCreationDate = otfMinimumCreationDate;
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
        if (descriptor.Emitter != companion.SelfLocationId)
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

        using (SmartCacheMetrics.ActivitySource.StartRichActivity(logger, $"{nameof(SmartCache)}.Invalidate"))
        {
            CoreInvalidate(keys.Keys, memoryCache.Remove);
            CoreInvalidate(externalMissDictionary.Keys, k => RemoveExternalMiss(new CacheKeyHolder(k, logger)));
        }

        if (broadcast)
        {
            NotifyInvalidation(invalidationRule);
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

    private void NotifyInvalidation(IInvalidationRule invalidationRule)
    {
        _ = Task.Run(NotifyInvalidationAsync);

        async Task NotifyInvalidationAsync()
        {
            IEnumerable<CacheEventNotifier> eventNotifiers = await companion.GetAllEventNotifiersAsync();
            if (!eventNotifiers.Any())
            {
                return;
            }

            InvalidationDescriptor descriptor = new (companion.SelfLocationId, invalidationRule);
            CachePayloadHolder<InvalidationDescriptor> descriptorHolder = new (descriptor, logger, SmartCacheMetrics.Tags.Subject.Value);
            foreach (CacheEventNotifier eventNotifier in eventNotifiers)
            {
                eventNotifier.NotifyInvalidationAndForget(descriptorHolder);
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

        if (emitter == companion.SelfLocationId)
        {
            return;
        }

        if (valueType is not null)
        {
            SetValue(new CacheKeyHolder(key, logger), valueType, descriptor.Value, timestamp, skipNotify: true);
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
