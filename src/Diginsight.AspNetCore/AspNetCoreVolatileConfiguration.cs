using Diginsight.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Diginsight.AspNetCore;

public static class AspNetCoreVolatileConfiguration
{
    public static void Apply(HttpContext httpContext, bool delete, bool overwrite)
    {
        IServiceProvider serviceProvider = httpContext.RequestServices;
        IVolatileConfigurationStorageProvider storageProvider = serviceProvider.GetRequiredService<IVolatileConfigurationStorageProvider>();

        foreach (IAspNetCoreVolatileConfigurationLoader loader in serviceProvider.GetServices<IAspNetCoreVolatileConfigurationLoader>())
        {
            IVolatileConfigurationStorage storage = storageProvider.Get(loader.StorageName);
            IEnumerable<KeyValuePair<string, string?>> entries = delete ? [ ] : loader.Load(httpContext);
            storage.Apply(entries, overwrite);
        }
    }
}
