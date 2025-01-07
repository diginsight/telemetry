using Diginsight.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Diginsight.AspNetCore;

public sealed class ConfigurationVolatileConfigurationLoader : IAspNetCoreVolatileConfigurationLoader
{
    private const string HeaderName = "Volatile-Configuration";

    public string StorageName => KnownVolatileConfigurationStorageNames.Configuration;

    public IEnumerable<KeyValuePair<string, string?>> Load(HttpContext httpContext)
    {
        return DynamicHttpHeadersParser.ParseConfiguration(httpContext.Request.Headers[HeaderName].NormalizeHttpHeaderValue(), true);
    }

    public static void AddToServices(IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAspNetCoreVolatileConfigurationLoader, ConfigurationVolatileConfigurationLoader>());
        services.Configure<DiginsightDistributedContextOptions>(static x => { x.NonBaggageKeys.Add(HeaderName); });
    }
}
