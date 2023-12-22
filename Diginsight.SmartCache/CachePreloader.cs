using Diginsight.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Diginsight.SmartCache;

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
    private readonly ICacheCompanionProvider companionProvider;

    public CachePreloader(
        ILogger<CachePreloader> logger,
        ICacheCompanionProvider companionProvider
    )
    {
        this.logger = logger;
        this.companionProvider = companionProvider;
    }

    public async Task PreloadAsync<T>(ICacheKey key, Func<Task<T>> fetchAsync)
    {
        using Activity? activity = SmartCacheMetrics.ActivitySource.StartMethodActivity(logger, new { key });

        CacheKeyHolder keyHolder = new CacheKeyHolder(key);
        // TODO activity?.SetTag("cache.key", keyLogString);

        SmartCacheMetrics.Instruments.Preloads.Add(1);

        DateTime timestamp = SmartCacheService.Truncate(DateTime.UtcNow);

        T value;
        StrongBox<double> latencyMsecBox = new();
        using (SmartCacheMetrics.Instruments.FetchDuration.StartLap(latencyMsecBox, SmartCacheMetrics.Tags.Type.Preload))
        {
            value = await fetchAsync();
        }

        logger.LogDebug("Fetched in {LatencyMsec} ms", (long)latencyMsecBox.Value);

        _ = Task.Run(() => PublishAsync(keyHolder, timestamp, value));
    }

    private async Task PublishAsync<TValue>(CacheKeyHolder keyHolder, DateTime creationDate, TValue value)
    {
        using Activity? activity = SmartCacheMetrics.ActivitySource.StartMethodActivity(logger, new { key = keyHolder.Key, creationDate });

        IEnumerable<CacheCompanion> companions = await companionProvider.GetCompanionsAsync();
        if (!companions.Any())
        {
            return;
        }

        string selfLocationId = companionProvider.SelfLocationId;
        CacheMissDescriptor descriptor = new (selfLocationId, keyHolder.Key, creationDate, selfLocationId, (typeof(TValue), value));
        CachePayloadHolder<CacheMissDescriptor> descriptorHolder = new (descriptor, SmartCacheMetrics.Tags.Subject.Value);

        CacheCompanion[] companionArray = companions.ToArray();
        companionArray[SharedRandom.Next(companionArray.Length)].PublishCacheMissAndForget(descriptorHolder);
    }
}
