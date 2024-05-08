using Diginsight.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Diginsight.SmartCache.Externalization.Http;

internal sealed class HttpCacheLocation : ActiveCacheLocation
{
    private readonly Func<HttpClient> createHttpClient;
    private readonly ILogger logger;
    private readonly ISmartCacheHttpOptions httpOptions;

    public override KeyValuePair<string, object?> MetricTag => SmartCacheObservability.Tags.Type.Distributed;

    public HttpCacheLocation(
        string host,
        Func<HttpClient> createHttpClient,
        ILogger<HttpCacheLocation> logger,
        IOptions<SmartCacheHttpOptions> httpOptions
    )
        : base(host)
    {
        this.createHttpClient = createHttpClient;
        this.logger = logger;
        this.httpOptions = httpOptions.Value;
    }

    public override async Task<CacheLocationOutput<TValue>?> GetAsync<TValue>(
        CacheKeyHolder keyHolder, DateTimeOffset minimumCreationDate, Action markInvalid, CancellationToken cancellationToken
    )
    {
        using Activity? activity = SmartCacheObservability.ActivitySource.StartMethodActivity(logger, new { key = keyHolder.Key, minimumCreationDate });
        using TimerLap lap = SmartCacheObservability.Instruments.FetchDuration.CreateLap(SmartCacheObservability.Tags.Type.Distributed);

        try
        {
            HttpResponseMessage responseMessage;
            using (lap.Start())
            {
                responseMessage = await HttpCacheCompanionHelper.SendAsync(createHttpClient(), httpOptions, Id, httpOptions.GetPathSegment, keyHolder, true, cancellationToken);
            }

            TValue item;
            long valueSerializedSize;
            using (responseMessage)
            {
                responseMessage.EnsureSuccessStatusCode();
                HttpContent responseContent = responseMessage.Content;

                valueSerializedSize = responseContent.Headers.ContentLength!.Value;

#if NET
                await using (Stream contentStream = await responseContent.ReadAsStreamAsync(cancellationToken))
#elif NETSTANDARD2_1_OR_GREATER
                await using (Stream contentStream = await responseContent.ReadAsStreamAsync())
#else
                using (Stream contentStream = await responseContent.ReadAsStreamAsync())
#endif
                using (SmartCacheObservability.Instruments.SerializationDuration.StartLap(SmartCacheObservability.Tags.Operation.Deserialization, SmartCacheObservability.Tags.Subject.Value))
                {
                    item = SmartCacheSerialization.Deserialize<TValue>(contentStream);
                }
            }

            double latencyMsecD = lap.ElapsedMilliseconds;
            long latencyMsecL = (long)latencyMsecD;

            logger.LogDebug("Cache hit (latency: {LatencyMsec} ms): Returning up-to-date value from host {Host}", latencyMsecL, Id);

            lap.AddTags(SmartCacheObservability.Tags.Found.True);
            return new CacheLocationOutput<TValue>(item, valueSerializedSize, latencyMsecD);
        }
        catch (Exception e)
            when (e is InvalidOperationException or HttpRequestException || e is OperationCanceledException oce && oce.CancellationToken != cancellationToken)
        {
            markInvalid();
            logger.LogDebug("Partial cache miss: Failed to retrieve value from host {Host}", Id);

            lap.AddTags(SmartCacheObservability.Tags.Found.False);
            return null;
        }
    }
}
