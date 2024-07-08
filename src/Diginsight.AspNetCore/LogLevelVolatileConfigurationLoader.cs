using Diginsight.CAOptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Diginsight.AspNetCore;

public sealed class LogLevelVolatileConfigurationLoader : IAspNetCoreVolatileConfigurationLoader
{
    public string StorageName => KnownVolatileConfigurationStorageNames.LogLevel;

    public IEnumerable<KeyValuePair<string, string?>> Load(HttpContext httpContext)
    {
        LoggerFilterOptions loggerFilterOptions = new ();
        if (!DynamicHttpHeadersParser.ParseLogLevel(httpContext.Request.Headers["Log-Level"].NormalizeHttpHeaderValue(), loggerFilterOptions, false))
        {
            return [ ];
        }

        return loggerFilterOptions.Rules
            .Select(
                static x => new KeyValuePair<string, string?>(
                    $"{(x.ProviderName is { } providerName ? $"{providerName}:" : "")}LogLevel:{x.CategoryName ?? "Default"}",
                    x.LogLevel!.Value.ToString("G")
                )
            );
    }
}
