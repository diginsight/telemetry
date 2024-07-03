using Microsoft.AspNetCore.Http;

namespace Diginsight.AspNetCore;

public interface IHttpContextVolatileConfigurationLoader
{
    string StorageName { get; }

    IEnumerable<KeyValuePair<string, string?>> Load(HttpContext httpContext);
}
