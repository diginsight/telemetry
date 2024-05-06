using Microsoft.Extensions.Http;

namespace Diginsight.SmartCache;

internal sealed class SmartCacheHttpMessageHandlerBuilderFilter : IHttpMessageHandlerBuilderFilter
{
#if NET
    public static readonly HttpRequestOptionsKey<bool> PreventSmartCacheDownstreamOptionsKey = new("PreventSmartCacheDownstream");
#else
    public const string PreventSmartCacheDownstreamOptionsKey = "PreventSmartCacheDownstream";
#endif

    private readonly SmartCacheDownstreamSettings downstreamSettings;

    public SmartCacheHttpMessageHandlerBuilderFilter(SmartCacheDownstreamSettings downstreamSettings)
    {
        this.downstreamSettings = downstreamSettings;
    }

    public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next)
    {
        return builder =>
        {
            builder.AdditionalHandlers.Insert(0, new SmartCacheHttpMessageHandler(downstreamSettings));

            next(builder);
        };
    }

    private sealed class SmartCacheHttpMessageHandler : DelegatingHandler
    {
        private readonly SmartCacheDownstreamSettings downstreamSettings;

        public SmartCacheHttpMessageHandler(SmartCacheDownstreamSettings downstreamSettings)
        {
            this.downstreamSettings = downstreamSettings;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage requestMessage, CancellationToken cancellationToken)
        {
            bool IsNotPrevented()
            {
#if NET
                return requestMessage.Options.TryGetValue(PreventSmartCacheDownstreamOptionsKey, out bool prevent) && prevent;
#else
                return requestMessage.Properties.TryGetValue(PreventSmartCacheDownstreamOptionsKey, out object? rawPrevent) && rawPrevent is true;
#endif
            }

            if (IsNotPrevented() && downstreamSettings.Header is { } header)
            {
                requestMessage.Headers.Add(header.Key, header.Value);
            }

            return base.SendAsync(requestMessage, cancellationToken);
        }
    }
}