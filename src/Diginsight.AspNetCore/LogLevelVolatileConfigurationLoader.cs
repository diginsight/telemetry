using Diginsight.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Diginsight.AspNetCore;

/// <summary>
/// Loads volatile log level configuration entries from ASP.NET Core HTTP request headers.
/// </summary>
public sealed class LogLevelVolatileConfigurationLoader : IAspNetCoreVolatileConfigurationLoader
{
    private const string HeaderName = "Log-Level";

    /// <inheritdoc />
    public string StorageName => KnownVolatileConfigurationStorageNames.LogLevel;

    /// <inheritdoc />
    public IEnumerable<KeyValuePair<string, string?>> Load(HttpContext httpContext)
    {
        LoggerFilterOptions loggerFilterOptions = new ();
        if (!DynamicHttpHeadersParser.UpdateLogLevel(httpContext.Request.Headers[HeaderName].NormalizeHttpHeaderValue(), loggerFilterOptions, false))
        {
            return [ ];
        }

        return loggerFilterOptions.Rules
            .Select(
                static x => KeyValuePair.Create(
                    $"{(x.ProviderName is { } providerName ? $"{providerName}:" : "")}LogLevel:{x.CategoryName ?? "Default"}",
                    x.LogLevel is { } logLevel ? logLevel.ToString("G") : null
                )
            );
    }

    /// <summary>
    /// Registers the log level volatile configuration loader in the specified service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public static void AddToServices(IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAspNetCoreVolatileConfigurationLoader, LogLevelVolatileConfigurationLoader>());
        services.Configure<DiginsightDistributedContextOptions>(static x => { x.NonBaggageKeys.Add(HeaderName); });
    }
}
