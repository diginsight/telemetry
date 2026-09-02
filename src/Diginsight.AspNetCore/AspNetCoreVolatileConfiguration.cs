using Diginsight.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Diginsight.AspNetCore;

/// <summary>
/// Provides operations for applying volatile configuration entries from ASP.NET Core HTTP requests.
/// </summary>
public static class AspNetCoreVolatileConfiguration
{
    /// <summary>
    /// Applies volatile configuration entries loaded from the specified HTTP context.
    /// </summary>
    /// <param name="httpContext">The HTTP context.</param>
    /// <param name="delete">Whether to clear the target volatile configuration storages.</param>
    /// <param name="overwrite">Whether to overwrite existing volatile configuration entries.</param>
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
