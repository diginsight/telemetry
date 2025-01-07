using Diginsight.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Diginsight.AspNetCore;

public sealed class DefaultDynamicConfigurationLoader : IDynamicConfigurationLoader
{
    private const string HeaderName = "Dynamic-Configuration";

    private readonly IHttpContextAccessor httpContextAccessor;

    public DefaultDynamicConfigurationLoader(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    public IEnumerable<KeyValuePair<string, string?>> Load()
    {
        if (httpContextAccessor.HttpContext is not { } httpContext)
        {
            return [ ];
        }

        return DynamicHttpHeadersParser
            .ParseConfiguration(httpContext.Request.Headers[HeaderName].NormalizeHttpHeaderValue(), false)
            .ToArray();
    }

    public static void AddToServices(IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.TryAddSingleton<IDynamicConfigurationLoader, DefaultDynamicConfigurationLoader>();
        services.Configure<DiginsightDistributedContextOptions>(static x => { x.NonBaggageKeys.Add(HeaderName); });
    }
}
