using Diginsight.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Diginsight.AspNetCore;

/// <summary>
/// Loads dynamic configuration entries from ASP.NET Core HTTP request headers.
/// </summary>
public sealed class DefaultDynamicConfigurationLoader : IDynamicConfigurationLoader
{
    private const string HeaderName = "Dynamic-Configuration";

    private readonly IHttpContextAccessor httpContextAccessor;

    /// <summary>
    /// DI constructor.
    /// </summary>
    public DefaultDynamicConfigurationLoader(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public IEnumerable<KeyValuePair<string, string?>> Load()
    {
        if (httpContextAccessor.HttpContext is not { } httpContext)
        {
            return [ ];
        }

        return [ ..DynamicHttpHeadersParser.ParseConfiguration(httpContext.Request.Headers[HeaderName].NormalizeHttpHeaderValue(), false) ];
    }

    /// <summary>
    /// Registers the default dynamic configuration loader in the specified service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public static void AddToServices(IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.TryAddSingleton<IDynamicConfigurationLoader, DefaultDynamicConfigurationLoader>();
        services.Configure<DiginsightDistributedContextOptions>(static x => { x.NonBaggageKeys.Add(HeaderName); });
    }
}
