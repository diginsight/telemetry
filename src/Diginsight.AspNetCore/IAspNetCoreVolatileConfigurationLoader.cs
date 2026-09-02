using Microsoft.AspNetCore.Http;

namespace Diginsight.AspNetCore;

/// <summary>
/// Represents an interface for loading volatile configuration entries from ASP.NET Core HTTP contexts.
/// </summary>
public interface IAspNetCoreVolatileConfigurationLoader
{
    /// <summary>
    /// Gets the name of the volatile configuration storage to update.
    /// </summary>
    string StorageName { get; }

    /// <summary>
    /// Loads volatile configuration entries from the specified HTTP context.
    /// </summary>
    /// <param name="httpContext">The HTTP context.</param>
    /// <returns>A lazy enumerable of volatile configuration entries.</returns>
    IEnumerable<KeyValuePair<string, string?>> Load(HttpContext httpContext);
}
