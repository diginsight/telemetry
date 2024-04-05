using Diginsight.AspNetCore;
using Diginsight.SmartCache.Externalization.Http;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Diginsight.SmartCache.Externalization.AspNetCore;

public static class SmartCacheAspNetCoreExtensions
{
    public static SmartCacheBuilder AddMiddleware(
        this SmartCacheBuilder builder, Action<SmartCacheHttpOptions>? configureOptions = null
    )
    {
        builder.AddHttp(configureOptions);
        builder.Services.TryAddTransient<SmartCacheMiddleware>();

        return builder;
    }

    public static SmartCacheBuilder AddHttpHeaderSupport(this SmartCacheBuilder builder)
    {
        builder.Services.PostConfigureClassAwareFromHttpRequestHeaders<SmartCacheCoreOptions>();

        return builder;
    }
}
