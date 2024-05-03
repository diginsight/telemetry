namespace Diginsight.SmartCache.Externalization.Http;

internal static class HttpCacheCompanionHelper
{
    public static async Task<HttpResponseMessage> SendAsync(
        HttpClient httpClient,
        ISmartCacheHttpOptions httpOptions,
        string host,
        string pathSegment,
        ICachePayloadHolder payloadHolder,
        bool full,
        CancellationToken cancellationToken = default
    )
    {
        using HttpRequestMessage requestMessage = new (
            HttpMethod.Post,
            $"{(httpOptions.UseHttps ? "https" : "http")}://{host}{httpOptions.RootPath}{pathSegment}"
        );
        requestMessage.Content = new StringContent(payloadHolder.GetAsString(), SmartCacheSerialization.Encoding, "application/json");
        requestMessage.PreventSmartCacheDownstreamHeaders();

        return await httpClient
            .SendAsync(requestMessage, full ? HttpCompletionOption.ResponseContentRead : HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    }
}
