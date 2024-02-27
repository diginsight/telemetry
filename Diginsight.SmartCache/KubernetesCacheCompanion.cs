using Diginsight.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Diginsight.SmartCache;

internal sealed class KubernetesCacheCompanion : CacheCompanion
{
    private readonly ILogger<KubernetesCacheCompanion> logger;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly ISmartCacheKubernetesOptions kubernetesOptions;
    private readonly ISmartCacheMiddlewareOptions middlewareOptions;

    public KubernetesCacheCompanion(
        string podIp,
        ILogger<KubernetesCacheCompanion> logger,
        IHttpClientFactory httpClientFactory,
        IOptions<SmartCacheKubernetesOptions> kubernetesOptions,
        IOptions<SmartCacheMiddlewareOptions> middlewareOptions
    )
        : base(podIp)
    {
        this.logger = logger;
        this.httpClientFactory = httpClientFactory;
        this.kubernetesOptions = kubernetesOptions.Value;
        this.middlewareOptions = middlewareOptions.Value;
    }

    public override async Task<CacheLocationOutput<TValue>?> GetAsync<TValue>(
        CacheKeyHolder keyHolder, DateTime minimumCreationDate, Action markInvalid, CancellationToken cancellationToken
    )
    {
        using Activity? activity = SmartCacheMetrics.ActivitySource.StartMethodActivity(logger, new { key = keyHolder.Key, minimumCreationDate });

        using TimerLap lap = SmartCacheMetrics.Instruments.FetchDuration.CreateLap(SmartCacheMetrics.Tags.Type.Distributed);
        try
        {
            HttpResponseMessage responseMessage;
            using (lap.Start())
            {
                responseMessage = await MakeHttpClient().PostAsync(
                    MakeRequestUri(middlewareOptions.GetPathSegment),
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
                using (SmartCacheMetrics.StartDeserializeActivity(logger, SmartCacheMetrics.Tags.Subject.Value))
                {
                    item = SmartCacheSerialization.Deserialize<TValue>(contentStream);
                }
            }

            double latencyMsecD = lap.ElapsedMilliseconds;
            long latencyMsecL = (long)latencyMsecD;

            logger.LogDebug("Cache hit (latency: {LatencyMsec}): Returning up-to-date value from companion {CompanionId}", latencyMsecL, Id);

            lap.AddTags(SmartCacheMetrics.Tags.Found.True);
            return new CacheLocationOutput<TValue>(item, valueSerializedSize, latencyMsecD);
        }
        catch (Exception e)
            when (e is InvalidOperationException or HttpRequestException || e is TaskCanceledException tce && tce.CancellationToken != cancellationToken)
        {
            lap.AddTags(SmartCacheMetrics.Tags.Found.False);
            markInvalid();
            logger.LogDebug("Partial cache miss: Failed to retrieve value from companion {CompanionId}", Id);
        }

        lap.AddTags(SmartCacheMetrics.Tags.Found.False);
        return null;
    }

    protected override Task PublishCacheMissAndForgetAsync(CachePayloadHolder<CacheMissDescriptor> descriptorHolder)
    {
        return PublishAndForgetAsync(descriptorHolder, middlewareOptions.CacheMissPathSegment);
    }

    protected override Task PublishInvalidationAndForgetAsync(CachePayloadHolder<InvalidationDescriptor> descriptorHolder)
    {
        return PublishAndForgetAsync(descriptorHolder, middlewareOptions.InvalidatePathSegment);
    }

    private async Task PublishAndForgetAsync<T>(CachePayloadHolder<T> descriptorHolder, string pathSegment)
        where T : notnull
    {
        HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, MakeRequestUri(pathSegment))
        {
            Content = new StringContent(descriptorHolder.GetAsString(), SmartCacheSerialization.Encoding),
        };
        using HttpResponseMessage responseMessage = await MakeHttpClient().SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
    }

    private HttpClient MakeHttpClient() => httpClientFactory.CreateClient(nameof(KubernetesCacheCompanion));

    private string MakeRequestUri(string pathSegment) => $"{(kubernetesOptions.UseHttps ? "https" : "http")}://{Id}{middlewareOptions.RootPath}{pathSegment}";
}
