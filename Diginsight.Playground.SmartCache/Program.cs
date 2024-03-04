using Diginsight.SmartCache;
using Diginsight.SmartCache.Externalization.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Diginsight.Playground.SmartCache;

internal class Program
{
    private static async Task Main()
    {
        IHost host = new HostBuilder()
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
            .Build();

        await host.StartAsync();

        IServiceProvider serviceProvider = host.Services;

        ILogger logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        ISmartCacheService cacheService = serviceProvider.GetRequiredService<ISmartCacheService>();
        ICacheKeyService cacheKeyService = serviceProvider.GetRequiredService<ICacheKeyService>();

        string? line;
        while (!string.IsNullOrEmpty(line = Console.ReadLine()))
        {
            IEnumerable<Guid> guids = await cacheService.GetAsync(
                new MethodCallCacheKey(cacheKeyService, typeof(Program), nameof(Main), line),
                async static () =>
                {
                    await Task.Delay(1000);
                    return Enumerable.Range(0, 10).Select(static _ => Guid.NewGuid()).ToArray();
                }
            );

            logger.LogInformation("Got guids {Guids} for key {Key}", guids.ToLogString(), line);
        }

        await host.StopAsync();
    }
}
