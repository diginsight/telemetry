using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Diginsight.AspNetCore;

public abstract class DynamicLogLevelMiddleware : IMiddleware
{
    private readonly ILoggerFactorySetter loggerFactorySetter;

    protected IEnumerable<ILoggerProvider> LoggerProviders => loggerFactorySetter.LoggerProviders;

    protected DynamicLogLevelMiddleware(ILoggerFactorySetter loggerFactorySetter)
    {
        this.loggerFactorySetter = loggerFactorySetter;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        using ILoggerFactory? loggerFactory = TryCreateLoggerFactory(context);
        using IDisposable? _0 = loggerFactory is not null ? loggerFactorySetter.WithLoggerFactory(loggerFactory) : null;
        await next(context);
    }

    protected abstract ILoggerFactory? TryCreateLoggerFactory(HttpContext context);
}
