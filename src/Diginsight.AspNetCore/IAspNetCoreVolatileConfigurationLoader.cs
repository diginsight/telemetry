using Microsoft.AspNetCore.Http;

namespace Diginsight.AspNetCore;

public interface IAspNetCoreVolatileConfigurationLoader
{
    string StorageName { get; }

    IEnumerable<KeyValuePair<string, string?>> Load(HttpContext httpContext);
}
