using Diginsight.Diagnostics;
using Diginsight.SmartCache.Externalization.Middleware;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Diginsight.SmartCache.Externalization.Kubernetes;

internal sealed class KubernetesCacheLocation : ActiveCacheLocation
{
    private readonly ILogger<KubernetesCacheLocation> logger;
    private readonly KubernetesCacheCompanionHelper helper;
    private readonly ISmartCacheMiddlewareOptions middlewareOptions;

    public override KeyValuePair<string, object?> MetricTag => SmartCacheObservability.Tags.Type.Distributed;

    public KubernetesCacheLocation(
        string podIp,
        ILogger<KubernetesCacheLocation> logger,
        KubernetesCacheCompanionHelper helper,
        IOptions<SmartCacheMiddlewareOptions> middlewareOptions
    )
        : base(podIp)
    {
        this.logger = logger;
        this.helper = helper;
        this.middlewareOptions = middlewareOptions.Value;
    }

    public override async Task<CacheLocationOutput<TValue>?> GetAsync<TValue>(
        CacheKeyHolder keyHolder, DateTime minimumCreationDate, Action markInvalid, CancellationToken cancellationToken
    )
    {
        using Activity? activity = SmartCacheObservability.ActivitySource.StartMethodActivity(logger, new { key = keyHolder.Key, minimumCreationDate });
        using TimerLap lap = SmartCacheObservability.Instruments.FetchDuration.CreateLap(SmartCacheObservability.Tags.Type.Distributed);

        try
        {
            HttpResponseMessage responseMessage;
            using (lap.Start())
            {
                responseMessage = await helper.MakeHttpClient()
                    .PostAsync(
                        helper.MakeRequestUri(Id, middlewareOptions.GetPathSegment),
                        new StringContent(keyHolder.GetAsString(), SmartCacheSerialization.Encoding, "application/json"),
                        cancellationToken
                    );
            }

            TValue item;
            long valueSerializedSize;
            using (responseMessage)
            {
                responseMessage.EnsureSuccessStatusCode();
                HttpContent responseContent = responseMessage.Content;

                valueSerializedSize = responseContent.Headers.ContentLength!.Value;

#if NET6_0_OR_GREATER
                await using (Stream contentStream = await responseContent.ReadAsStreamAsync(cancellationToken))
#elif NETSTANDARD2_1_OR_GREATER
                await using (Stream contentStream = await responseContent.ReadAsStreamAsync())
#else
                using (Stream contentStream = await responseContent.ReadAsStreamAsync())
#endif
                using (SmartCacheObservability.StartDeserializeActivity(logger, SmartCacheObservability.Tags.Subject.Value))
                {
                    item = SmartCacheSerialization.Deserialize<TValue>(contentStream);
                }
            }

            double latencyMsecD = lap.ElapsedMilliseconds;
            long latencyMsecL = (long)latencyMsecD;

            logger.LogDebug("Cache hit (latency: {LatencyMsec}): Returning up-to-date value from pod {PodIp}", latencyMsecL, Id);

            lap.AddTags(SmartCacheObservability.Tags.Found.True);
            return new CacheLocationOutput<TValue>(item, valueSerializedSize, latencyMsecD);
        }
        catch (Exception e)
            when (e is InvalidOperationException or HttpRequestException || e is OperationCanceledException oce && oce.CancellationToken != cancellationToken)
        {
            markInvalid();
            logger.LogDebug("Partial cache miss: Failed to retrieve value from pod {PodIp}", Id);

            lap.AddTags(SmartCacheObservability.Tags.Found.False);
            return null;
        }
    }
}
