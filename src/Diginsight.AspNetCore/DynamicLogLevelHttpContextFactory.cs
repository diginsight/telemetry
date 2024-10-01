using Diginsight.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Diginsight.AspNetCore;

internal sealed class DynamicLogLevelHttpContextFactory : IHttpContextFactory
{
    private static readonly object DynamicLogLevelLoggerFactoryItem = new ();
    private static readonly object DynamicLogLevelScopeItem = new ();

    private readonly IHttpContextFactory decoratee;
    private readonly ILoggerFactorySetter loggerFactorySetter;
    private readonly IDynamicLogLevelInjector dynamicLogLevelInjector;

    public DynamicLogLevelHttpContextFactory(
        IHttpContextFactory decoratee,
        ILoggerFactorySetter loggerFactorySetter,
        IDynamicLogLevelInjector dynamicLogLevelInjector
    )
    {
        this.decoratee = decoratee;
        this.loggerFactorySetter = loggerFactorySetter;
        this.dynamicLogLevelInjector = dynamicLogLevelInjector;
    }

    public HttpContext Create(IFeatureCollection featureCollection)
    {
        HttpContext httpContext = decoratee.Create(featureCollection);

        if (dynamicLogLevelInjector.TryCreateLoggerFactory(httpContext, loggerFactorySetter.LoggerProviders) is { } loggerFactory)
        {
            httpContext.Items[DynamicLogLevelLoggerFactoryItem] = loggerFactory;
            httpContext.Items[DynamicLogLevelScopeItem] = loggerFactorySetter.WithLoggerFactory(loggerFactory);
        }

        return httpContext;
    }

    public void Dispose(HttpContext httpContext)
    {
        if (httpContext.Items.TryGetValue(DynamicLogLevelScopeItem, out object? rawDisposable)
            && rawDisposable is IDisposable disposable)
        {
            disposable.Dispose();
        }
        if (httpContext.Items.TryGetValue(DynamicLogLevelLoggerFactoryItem, out object? rawLoggerFactory)
            && rawLoggerFactory is ILoggerFactory loggerFactory)
        {
            loggerFactory.Dispose();
        }

        decoratee.Dispose(httpContext);
    }
}
