using Diginsight.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Diginsight.AspNetCore;

public sealed class LogLevelVolatileConfigurationLoader : IAspNetCoreVolatileConfigurationLoader
{
    private const string HeaderName = "Log-Level";

    public string StorageName => KnownVolatileConfigurationStorageNames.LogLevel;

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

    public static void AddToServices(IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAspNetCoreVolatileConfigurationLoader, LogLevelVolatileConfigurationLoader>());
        services.Configure<DiginsightDistributedContextOptions>(static x => { x.NonBaggageKeys.Add(HeaderName); });
    }
}
