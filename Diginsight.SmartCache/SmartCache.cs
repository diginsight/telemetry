using Diginsight.CAOptions;
using Diginsight.Diagnostics;
using Diginsight.SmartCache.Externalization;
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
    private readonly SmartCacheDownstreamSettings downstreamSettings;
    private readonly TimeProvider timeProvider;

    private readonly IMemoryCache memoryCache;
    private readonly IReadOnlyDictionary<string, PassiveCacheLocation> passiveLocations;
    private readonly ISmartCacheCoreOptions staticCoreOptions;

    private readonly IDictionary<ICacheKey, ValueTuple> keys = new ConcurrentDictionary<ICacheKey, ValueTuple>();
    private readonly ExternalMissDictionary externalMissDictionary = new ();
    private readonly ConcurrentDictionary<string, Latency> locationLatencies = new ();

    private long memoryCacheSize = 0;

    public SmartCache(
        ILogger<SmartCache> logger,
        ICacheCompanion companion,
        IClassAwareOptionsMonitor<SmartCacheCoreOptions> coreOptionsMonitor,
        IOptionsMonitor<MemoryCacheOptions> memoryCacheOptionsMonitor,
        ILoggerFactory loggerFactory,
        SmartCacheDownstreamSettings downstreamSettings,
        TimeProvider? timeProvider = null
    )
    {
        this.logger = logger;
        this.companion = companion;
        this.coreOptionsMonitor = coreOptionsMonitor;
        this.downstreamSettings = downstreamSettings;
        this.timeProvider = timeProvider ?? TimeProvider.System;

        memoryCache = new MemoryCache(memoryCacheOptionsMonitor.Get(nameof(SmartCache)), loggerFactory);

        passiveLocations = companion.PassiveLocations.ToDictionary(static x => x.Id);

        staticCoreOptions = coreOptionsMonitor.CurrentValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static DateTimeOffset Truncate(DateTimeOffset timestamp)
    {
        return new DateTimeOffset(timestamp.Year, timestamp.Month, timestamp.Day, timestamp.Hour, timestamp.Minute, timestamp.Second, timestamp.Offset);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<T> GetAsync<T>(
        ICacheKey key,
        Func<CancellationToken, Task<T>> fetchAsync,
        SmartCacheOperationOptions? operationOptions,
        Type? callerType,
        CancellationToken cancellationToken
    )
    {
        using Activity? activity = SmartCacheObservability.ActivitySource.StartMethodActivity(logger, new { key, operationOptions, callerType });

        callerType ??= RuntimeUtils.GetCaller().DeclaringType;
        operationOptions ??= new SmartCacheOperationOptions();

        CacheKeyHolder keyHolder = new (key);

        SmartCacheObservability.Instruments.Calls.Add(1);

        if (operationOptions.Disabled)
        {
            SmartCacheObservability.Instruments.Sources.Add(1, SmartCacheObservability.Tags.Type.Disabled);

            using (SmartCacheObservability.Instruments.FetchDuration.StartLap(SmartCacheObservability.Tags.Type.Disabled))
            {
                activity?.SetTag("cache.disabled", 1);
                return await fetchAsync(cancellationToken);
            }
        }

        ISmartCacheCoreOptions coreOptions = coreOptionsMonitor.Get(callerType);

        Expiration? maxAge = operationOptions.MaxAge;
        DateTimeOffset timestamp = Truncate(timeProvider.GetUtcNow());
        DateTimeOffset minimumCreationDate = GetMinimumCreationDate(ref maxAge, timestamp, coreOptions);
        bool forceFetch = maxAge.Value == Expiration.Zero || minimumCreationDate >= timestamp;

        using (forceFetch ? downstreamSettings.WithZeroMaxAge() : downstreamSettings.WithMinimumCreationDate(minimumCreationDate))
        {
            return await GetAsync(
                keyHolder,
                fetchAsync,
                timestamp,
                forceFetch ? null : minimumCreationDate,
                operationOptions.AbsoluteExpiration,
                operationOptions.SlidingExpiration,
                coreOptions,
                cancellationToken
            );
        }
    }

    private DateTimeOffset GetMinimumCreationDate([NotNull] ref Expiration? maxAge, DateTimeOffset timestamp, ISmartCacheCoreOptions coreOptions)
    {
        Expiration finalMaxAge = Choose(coreOptions.MaxAge, maxAge, staticCoreOptions.MaxAge);

        DateTimeOffset minimumCreationDate = finalMaxAge.IsNever ? DateTimeOffset.MinValue : timestamp - finalMaxAge.Value;
        if (coreOptions.MinimumCreationDate is { } dynamicMinimumCreationDate && dynamicMinimumCreationDate > minimumCreationDate)
        {
            minimumCreationDate = dynamicMinimumCreationDate;
        }

        maxAge = finalMaxAge;

        return minimumCreationDate;
    }

    private async Task<T> GetAsync<T>(
        CacheKeyHolder keyHolder,
        Func<CancellationToken, Task<T>> fetchAsync,
        DateTimeOffset timestamp,
        DateTimeOffset? maybeMinimumCreationDate,
        Expiration? absExpiration,
        Expiration? sldExpiration,
        ISmartCacheCoreOptions coreOptions,
        CancellationToken cancellationToken
    )
    {
        using Activity? activity = SmartCacheObservability.ActivitySource.StartMethodActivity(
            logger, new { keyHolder.Key, timestamp, maybeMinimumCreationDate, absExpiration, sldExpiration }
        );

        using TimerLap memoryLap = SmartCacheObservability.Instruments.FetchDuration.CreateLap(SmartCacheObservability.Tags.Type.Memory);
        memoryLap.DisableCommit = true;

        ValueEntry<T>? localEntry;
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
            {
                localEntry = memoryCache.Get<ValueEntry<T>?>(keyHolder.Key);
                externalEntry = discardExternalMiss ? null : externalMissDictionary.Get(keyHolder.Key);
            }

            if (localEntry is not null)
            {
                logger.LogDebug("Cache entry found");
            }
        }

        async Task<T> FetchAndSetValueAsync([SuppressMessage("ReSharper", "VariableHidesOuterVariable")] Activity? activity)
        {
            SmartCacheObservability.Instruments.Sources.Add(1, SmartCacheObservability.Tags.Type.Miss);
            activity?.SetTag("cache.hit", 0);

            T value;
            StrongBox<double> latencyMsecBox = new ();
            using (SmartCacheObservability.Instruments.FetchDuration.StartLap(latencyMsecBox, SmartCacheObservability.Tags.Type.Miss))
            {
                value = await fetchAsync(cancellationToken);
            }

            long latencyMsec = (long)latencyMsecBox.Value;

            logger.LogDebug("Fetched in {LatencyMsec} ms", latencyMsec);

            SetValue(keyHolder, value, timestamp, coreOptions, absExpiration, sldExpiration, discardExternalMiss);
            return value;
        }

        DateTimeOffset? localCreationDate = localEntry?.CreationDate;

        if (externalEntry is var (othersCreationDate, locationIds) && !(othersCreationDate - coreOptions.LocalEntryTolerance <= localCreationDate))
        {
            DateTimeOffset minimumCreationDate = maybeMinimumCreationDate!.Value;
            if (othersCreationDate >= minimumCreationDate)
            {
                logger.LogDebug("Key is also available and up-to-date in other locations: {LocationIds}", locationIds);

                ConcurrentBag<string> invalidLocations = [ ];

                IReadOnlyDictionary<string, CacheLocation> locations = (await companion.GetActiveLocationsAsync(locationIds))
                    .Concat<CacheLocation>(passiveLocations.Values)
                    .ToDictionary(static x => x.Id);

                IEnumerable<Func<CancellationToken, Task<(CacheLocationOutput<T>, KeyValuePair<string, object?>)?>>> taskFactories = locationIds
                    .GroupJoin(
                        locationLatencies,
                        static l => l,
                        static kv => kv.Key,
                        static (l, kvs) => (LocationId: l, Latency: kvs.FirstOrDefault().Value ?? new Latency())
                    )
                    .OrderBy(static kv => kv.Latency)
                    .Select(static kv => kv.LocationId)
                    .Select(
                        Func<CancellationToken, Task<(CacheLocationOutput<T>, KeyValuePair<string, object?>)?>> (locationId) =>
                        {
                            if (!locations.TryGetValue(locationId, out CacheLocation? location))
                            {
                                return static _ => Task.FromResult<(CacheLocationOutput<T>, KeyValuePair<string, object?>)?>(null);
                            }

                            return async ct =>
                            {
                                CacheLocationOutput<T>? maybeOutput =
                                    await location.GetAsync<T>(keyHolder, minimumCreationDate, () => invalidLocations.Add(locationId), ct);

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

                (CacheLocationOutput<T> Output, KeyValuePair<string, object?> MetricTag)? maybeOutputTagged;
                try
                {
                    maybeOutputTagged = await TaskUtils.WhenAnyValid(
                        taskFactories.ToArray(),
                        coreOptions.LocationPrefetchCount,
                        coreOptions.LocationMaxParallelism,
                        // ReSharper disable once AsyncApostle.AsyncWait
                        isValid: static t => new ValueTask<bool>(t.Status != TaskStatus.RanToCompletion || t.Result is not null),
                        cancellationToken: cancellationToken
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
                    SmartCacheObservability.Instruments.KeySerializedSize.Record(keyHolder.GetAsBytes().LongLength, metricTag);
                    SmartCacheObservability.Instruments.ValueSerializedSize.Record(valueSerializedSize, metricTag);
                    SmartCacheObservability.Instruments.Sources.Add(1, metricTag);
                    SmartCacheObservability.Instruments.CompanionFetchDuration.Underlying.Record(latencyMsec, metricTag);
                    SmartCacheObservability.Instruments.CompanionFetchRelativeDuration.Record(latencyMsec / valueSerializedSize * 1000, metricTag);

                    SetValue(keyHolder, item, othersCreationDate, coreOptions, absExpiration, sldExpiration, discardExternalMiss);
                    return item!;
                }
            }
            else
            {
                logger.LogDebug(
                    "Cache miss: creation date validation failed (minimum: {MinimumCreationDate:s}, older: {LocalCreationDate:s})",
                    minimumCreationDate,
                    localCreationDate ?? DateTimeOffset.MinValue
                );
            }

            return await FetchAndSetValueAsync(activity);
        }

        memoryLap.DisableCommit = false;

        if (localCreationDate >= maybeMinimumCreationDate)
        {
            logger.LogDebug(
                "Cache hit: valid creation date (minimum: {MaybeMinimumCreationDate:s}, newer: {LocalCreationDate:s})",
                maybeMinimumCreationDate,
                localCreationDate.Value
            );

            memoryLap.AddTags(SmartCacheObservability.Tags.Found.True);
            SmartCacheObservability.Instruments.Sources.Add(1, SmartCacheObservability.Tags.Type.Memory);
            activity?.SetTag("cache.hit", 1);

            return localEntry!.Data;
        }
        else
        {
            logger.LogDebug(
                "Cache miss: creation date validation failed (minimum: {MaybeMinimumCreationDate:s}, older: {LocalCreationDate:s})",
                maybeMinimumCreationDate,
                localCreationDate ?? DateTimeOffset.MinValue
            );

            memoryLap.AddTags(SmartCacheObservability.Tags.Found.False);

            return await FetchAndSetValueAsync(activity);
        }
    }

    private void SetValue<T>(
        CacheKeyHolder keyHolder,
        T value,
        DateTimeOffset creationDate,
        ISmartCacheCoreOptions? dynamicCoreOptions,
        Expiration? absExpiration = null,
        Expiration? sldExpiration = null,
        bool skipNotify = false
    )
    {
        SetValue(keyHolder, typeof(T), value, creationDate, dynamicCoreOptions, absExpiration, sldExpiration, skipNotify);
    }

    private void SetValue(
        CacheKeyHolder keyHolder,
        Type valueType,
        object? value,
        DateTimeOffset creationDate,
        ISmartCacheCoreOptions? dynamicCoreOptions = null,
        Expiration? absExpiration = null,
        Expiration? sldExpiration = null,
        bool skipNotify = false
    )
    {
        using Activity? activity = SmartCacheObservability.ActivitySource.StartMethodActivity(
            logger, new { key = keyHolder.Key, valueType, creationDate, absExpiration, sldExpiration, skipNotify }
        );

        ICacheKey key = keyHolder.Key;

        keys[key] = default;
        RemoveExternalMiss(keyHolder);

        IValueEntry entry = ValueEntry.Create(value, valueType, creationDate);

        Expiration finalAbsExpiration = Choose(dynamicCoreOptions?.AbsoluteExpiration, absExpiration, staticCoreOptions.AbsoluteExpiration);

        ISmartCacheCoreOptions coreOptions = dynamicCoreOptions ?? staticCoreOptions;
        StorageMode storageMode = coreOptions.StorageMode;
        int missValueThreshold = coreOptions.MissValueSizeThreshold;

        if (storageMode != StorageMode.Auto)
        {
            foreach (PassiveCacheLocation passiveLocation in passiveLocations.Values)
            {
                WriteToLocation(passiveLocation, keyHolder, entry, finalAbsExpiration, missValueThreshold, skipNotify);
            }

            if (storageMode == StorageMode.Passive)
                return;
        }

        Expiration candidateSldExpiration = Choose(dynamicCoreOptions?.SlidingExpiration, sldExpiration, staticCoreOptions.SlidingExpiration);
        Expiration finalSldExpiration = candidateSldExpiration < finalAbsExpiration ? candidateSldExpiration : finalAbsExpiration;

        long keySize;
        try
        {
            using (SmartCacheObservability.Instruments.SizeComputationDuration.StartLap(SmartCacheObservability.Tags.Subject.Key))
            {
                keySize = Size.Get(key);
            }
            SmartCacheObservability.Instruments.KeyObjectSize.Record(keySize);
        }
        catch (Exception)
        {
            keySize = 0;
        }

        long valueSize;
        using (SmartCacheObservability.Instruments.SizeComputationDuration.StartLap(SmartCacheObservability.Tags.Subject.Value))
        {
            valueSize = Size.Get(value);
        }
        SmartCacheObservability.Instruments.ValueObjectSize.Record(valueSize);

        long size = keySize + valueSize;

        CacheItemPriority priority =
            size >= coreOptions.LowPrioritySizeThreshold ? CacheItemPriority.Low
            : size >= coreOptions.MidPrioritySizeThreshold ? CacheItemPriority.Normal
            : CacheItemPriority.High;

        MemoryCacheEntryOptions entryOptions = new ()
        {
            AbsoluteExpirationRelativeToNow = finalAbsExpiration.IsNever ? null : finalAbsExpiration.Value,
            SlidingExpiration = finalSldExpiration.IsNever ? null : finalSldExpiration.Value,
            Size = size,
            Priority = priority,
        };

        entryOptions.RegisterPostEvictionCallback(
            (k, v, r, _) =>
            {
                using (ActivityUtils.UnsetCurrent())
                {
                    Interlocked.Add(ref memoryCacheSize, -size);
                    SmartCacheObservability.Instruments.TotalSize.Add(-size);

                    OnEvicted(new CacheKeyHolder((ICacheKey)k), (IValueEntry)v!, r, finalAbsExpiration);
                }
            }
        );

        memoryCache.Set(key, entry, entryOptions);

        Interlocked.Add(ref memoryCacheSize, size);
        SmartCacheObservability.Instruments.TotalSize.Add(size);

        if (!skipNotify)
        {
            NotifyMiss(keyHolder, creationDate, missValueThreshold, (value, valueType), null);
        }
    }

    private void OnEvicted(CacheKeyHolder keyHolder, IValueEntry entry, EvictionReason reason, Expiration expiration)
    {
        using Activity? activity = SmartCacheObservability.ActivitySource.StartMethodActivity(logger, new { key = keyHolder.Key, reason, expiration });

        SmartCacheObservability.Instruments.Evictions.Add(
            1,
            reason switch
            {
                EvictionReason.Removed => SmartCacheObservability.Tags.Eviction.Removed,
                EvictionReason.Replaced => SmartCacheObservability.Tags.Eviction.Replaced,
                EvictionReason.Expired or EvictionReason.TokenExpired => SmartCacheObservability.Tags.Eviction.Expired,
                EvictionReason.Capacity => SmartCacheObservability.Tags.Eviction.Capacity,
                EvictionReason.None => throw new InvalidOperationException($"unexpected {nameof(EvictionReason)}"),
                _ => throw new ArgumentOutOfRangeException($"unrecognized {nameof(EvictionReason)}"),
            }
        );

        if (reason is EvictionReason.None or EvictionReason.Replaced)
        {
            return;
        }

        keys.Remove(keyHolder.Key);

        if (reason != EvictionReason.Capacity)
        {
            return;
        }

        foreach (PassiveCacheLocation passiveLocation in passiveLocations.Values)
        {
            WriteToLocation(passiveLocation, keyHolder, entry, expiration, 0);
        }
    }

    private void WriteToLocation(
        PassiveCacheLocation location, CacheKeyHolder keyHolder, IValueEntry entry, Expiration expiration, int missValueSizeThreshold, bool skipNotify = false
    )
    {
        location.WriteAndForget(
            keyHolder,
            entry,
            expiration,
            skipNotify
                ? static () => Task.CompletedTask
                : () => NotifyMissAsync(keyHolder, entry.CreationDate, missValueSizeThreshold, null, location.Id)
        );
    }

    private void NotifyMiss(
        CacheKeyHolder keyHolder, DateTimeOffset creationDate, int missValueSizeThreshold, (object?, Type)? valueHolder, string? locationId
    )
    {
        TaskUtils.RunAndForget(() => NotifyMissAsync(keyHolder, creationDate, missValueSizeThreshold, valueHolder, locationId));
    }

    private async Task NotifyMissAsync(
        CacheKeyHolder keyHolder, DateTimeOffset creationDate, int missValueSizeThreshold, (object?, Type)? valueHolder, string? locationId
    )
    {
        if (locationId is not null)
        {
            externalMissDictionary.Add(keyHolder.Key, creationDate, locationId);
        }

        IEnumerable<CacheEventNotifier> eventNotifiers = await companion.GetAllEventNotifiersAsync();
        if (!eventNotifiers.Any())
        {
            return;
        }

        (Type, object?)? valueTuple;
        if (valueHolder is var (value, valueType) && missValueSizeThreshold is > 0 and var size)
        {
            byte[] valueBytes = new byte[size];
#if NET || NETSTANDARD2_1_OR_GREATER
            await using MemoryStream valueStream = new (valueBytes);
#else
            using MemoryStream valueStream = new (valueBytes);
#endif

            using (SmartCacheObservability.Instruments.SerializationDuration.StartLap(SmartCacheObservability.Tags.Operation.Serialization, SmartCacheObservability.Tags.Subject.Value))
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
        CachePayloadHolder<CacheMissDescriptor> descriptorHolder = new (descriptor, SmartCacheObservability.Tags.Subject.Value);

        foreach (CacheEventNotifier eventNotifier in eventNotifiers)
        {
            eventNotifier.NotifyCacheMissAndForget(descriptorHolder);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Expiration Choose(Expiration? maybeDynamic, Expiration? maybeOperation, Expiration fallback)
    {
        return maybeDynamic is { IsNever: false } dynamic ? dynamic : (maybeOperation ?? fallback);
    }

    public bool TryGetDirectFromMemory(ICacheKey key, [NotNullWhen(true)] out Type? type, out object? value)
    {
        using Activity? activity = SmartCacheObservability.ActivitySource.StartMethodActivity(logger, new { key });

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
        using Activity? activity = SmartCacheObservability.ActivitySource.StartMethodActivity(logger, () => new { invalidationRule, broadcast });

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

        CoreInvalidate(keys.Keys, memoryCache.Remove);
        CoreInvalidate(externalMissDictionary.Keys, k => RemoveExternalMiss(new CacheKeyHolder(k)));

        if (broadcast)
        {
            NotifyInvalidation(invalidationRule);
        }

        TaskUtils.RunAndForget(
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
        TaskUtils.RunAndForget(NotifyInvalidationAsync);

        async Task NotifyInvalidationAsync()
        {
            IEnumerable<CacheEventNotifier> eventNotifiers = await companion.GetAllEventNotifiersAsync();
            if (!eventNotifiers.Any())
            {
                return;
            }

            InvalidationDescriptor descriptor = new (companion.SelfLocationId, invalidationRule);
            CachePayloadHolder<InvalidationDescriptor> descriptorHolder = new (descriptor, SmartCacheObservability.Tags.Subject.Value);
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
            DateTimeOffset timestamp,
            string location,
            Type? valueType) = descriptor;

        if (emitter == companion.SelfLocationId)
        {
            return;
        }

        if (valueType is not null)
        {
            SetValue(new CacheKeyHolder(key), valueType, descriptor.Value, timestamp, skipNotify: true);
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
        private readonly ConcurrentDictionary<ICacheKey, Entry> underlying = new ();

        public IEnumerable<ICacheKey> Keys => underlying.Keys;

        private readonly object lockObject = new ();

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

        public void Add(ICacheKey key, DateTimeOffset timestamp, string location)
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

        public sealed record Entry(DateTimeOffset Timestamp, IEnumerable<string> Locations);
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
                average = (average * count + latency) / ++count;
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
                        return (0, true);
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
#if NET || NETSTANDARD2_1_OR_GREATER
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
                return (long)GetUnmanagedSizeMethod.MakeGenericMethod(type).Invoke(null, [ ])!;
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
