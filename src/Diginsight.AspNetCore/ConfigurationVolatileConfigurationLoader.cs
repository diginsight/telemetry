using Diginsight.Options;
using Microsoft.AspNetCore.Http;

namespace Diginsight.AspNetCore;

public sealed class ConfigurationVolatileConfigurationLoader : IAspNetCoreVolatileConfigurationLoader
{
    public string StorageName => KnownVolatileConfigurationStorageNames.Configuration;

    public IEnumerable<KeyValuePair<string, string?>> Load(HttpContext httpContext)
    {
        return DynamicHttpHeadersParser.ParseConfiguration(httpContext.Request.Headers["Volatile-Configuration"].NormalizeHttpHeaderValue(), true);
    }
}
