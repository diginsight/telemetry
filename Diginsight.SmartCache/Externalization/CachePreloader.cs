﻿using Diginsight.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Diginsight.SmartCache.Externalization;

public sealed class CachePreloader : ICachePreloader
{
#if NET6_0_OR_GREATER
    private static Random SharedRandom
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Random.Shared;
    }
#else
    private static readonly Random SharedRandom = new ();
#endif

    private readonly ILogger<CachePreloader> logger;
    private readonly ICacheCompanion companion;

    public CachePreloader(
        ILogger<CachePreloader> logger,
        ICacheCompanion companion
    )
    {
        this.logger = logger;
        this.companion = companion;
    }

    public async Task PreloadAsync<T>(ICacheKey key, Func<Task<T>> fetchAsync)
    {
        using Activity? activity = SmartCacheMetrics.ActivitySource.StartMethodActivity(logger, new { key });

        CacheKeyHolder keyHolder = new CacheKeyHolder(key, logger);
        // TODO activity?.SetTag("cache.key", keyLogString);

        SmartCacheMetrics.Instruments.Preloads.Add(1);

        DateTime timestamp = SmartCacheService.Truncate(DateTime.UtcNow);

        T value;
        StrongBox<double> latencyMsecBox = new ();
        using (SmartCacheMetrics.Instruments.FetchDuration.StartLap(latencyMsecBox, SmartCacheMetrics.Tags.Type.Preload))
        {
            value = await fetchAsync();
        }

        logger.LogDebug("Fetched in {LatencyMsec} ms", (long)latencyMsecBox.Value);

        _ = Task.Run(() => NotifyAsync(keyHolder, timestamp, value));
    }

    private async Task NotifyAsync<TValue>(CacheKeyHolder keyHolder, DateTime creationDate, TValue value)
    {
        using Activity? activity = SmartCacheMetrics.ActivitySource.StartMethodActivity(logger, new { key = keyHolder.Key, creationDate });

        IEnumerable<CacheEventNotifier> eventNotifiers = await companion.GetAllEventNotifiersAsync();
        if (!eventNotifiers.Any())
        {
            return;
        }

        string selfLocationId = companion.SelfLocationId;
        CacheMissDescriptor descriptor = new (selfLocationId, keyHolder.Key, creationDate, selfLocationId, (typeof(TValue), value));
        CachePayloadHolder<CacheMissDescriptor> descriptorHolder = new (descriptor, logger, SmartCacheMetrics.Tags.Subject.Value);

        CacheEventNotifier[] eventNotifiersArray = eventNotifiers.ToArray();
        eventNotifiersArray[SharedRandom.Next(eventNotifiersArray.Length)].NotifyCacheMissAndForget(descriptorHolder);
    }
}