using Microsoft.Extensions.DependencyInjection;

namespace Diginsight.SmartCache.Externalization.Http;

public abstract class HttpCacheCompanion : ICacheCompanion
{
    private readonly IServiceProvider serviceProvider;
    private readonly Func<HttpClient> createHttpClient;

    private readonly ObjectFactory<HttpCacheLocation> makeLocation =
        ActivatorUtilities.CreateFactory<HttpCacheLocation>([ typeof(string), typeof(Func<HttpClient>) ]);

    private readonly ObjectFactory<HttpCacheEventNotifier> makeEventNotifier =
        ActivatorUtilities.CreateFactory<HttpCacheEventNotifier>([ typeof(string), typeof(Func<HttpClient>) ]);

    public abstract string SelfLocationId { get; }

    public abstract IEnumerable<PassiveCacheLocation> PassiveLocations { get; }

    protected HttpCacheCompanion(IServiceProvider serviceProvider, Func<HttpClient> createHttpClient)
    {
        this.serviceProvider = serviceProvider;
        this.createHttpClient = createHttpClient;
    }

    public abstract Task<IEnumerable<ActiveCacheLocation>> GetActiveLocationsAsync(IEnumerable<string> locationIds);

    public abstract Task<IEnumerable<CacheEventNotifier>> GetAllEventNotifiersAsync();

    protected ActiveCacheLocation MakeLocation(string host) => makeLocation(serviceProvider, [ host, createHttpClient ]);

    protected CacheEventNotifier MakeEventNotifier(string host) => makeEventNotifier(serviceProvider, [ host, createHttpClient ]);
}
