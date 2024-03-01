using Diginsight.SmartCache;
using Diginsight.SmartCache.Externalization.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Diginsight.Playground.SmartCache;

internal static class Program
{
    private static void Main()
    {
        new HostBuilder()
            .ConfigureAppConfiguration(
                static builder =>
                {
                    builder
                        .AddJsonFile("appsettings.json")
                        .AddUserSecrets(typeof(Program).Assembly)
                        .AddEnvironmentVariables();
                }
            )
            .ConfigureServices(
                static (hbc, services) =>
                {
                    IConfiguration configuration = hbc.Configuration;

                    services
                        .AddLogging(
                            lb => lb
                                .ClearProviders()
                                .AddConfiguration(configuration.GetSection("Logging"))
                                .AddSimpleConsole(
                                    static scfo =>
                                    {
                                        scfo.TimestampFormat = "O";
                                        scfo.UseUtcTimestamp = true;
                                    }
                                )
                        )
                        .Configure<SmartCacheServiceOptions>(configuration.GetSection("SmartCache:Core"))
                        .Configure<SmartCacheServiceBusOptions>(configuration.GetSection("SmartCache:ServiceBus"))
                        .AddSmartCache()
                        .SetServiceBusCompanion();
                }
            )
            .Build()
            .Run();
    }
}
