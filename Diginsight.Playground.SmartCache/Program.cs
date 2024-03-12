using Diginsight.SmartCache;
using Diginsight.SmartCache.Externalization;
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
                        .Configure<SmartCacheCoreOptions>(configuration.GetSection("SmartCache:Core"))
                        .Configure<SmartCacheServiceBusOptions>(configuration.GetSection("SmartCache:ServiceBus"))
                        .AddSmartCache()
                        .SetServiceBusCompanion();
                }
            )
            .Build();

        await host.StartAsync();

        IServiceProvider serviceProvider = host.Services;

        ILogger logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        ISmartCache smartCache = serviceProvider.GetRequiredService<ISmartCache>();

        string? line;
        while (!string.IsNullOrEmpty(line = Console.ReadLine()))
        {
            if (line.StartsWith('!'))
            {
                string prefix = line[1..];

                smartCache.Invalidate(new MyInvalidationRule(prefix));

                logger.LogInformation("Invalidating keys starting with '{Prefix}'", prefix);
            }
            else
            {
                IEnumerable<Guid> guids = await smartCache.GetAsync(
                    new MyCacheKey(line),
                    async static () =>
                    {
                        await Task.Delay(1000);
                        return Enumerable.Range(0, 50).Select(static _ => Guid.NewGuid()).ToArray();
                    }
                );

                logger.LogInformation("Got guids {Guids} for key '{Key}'", guids.ToLogString(), line);
            }
        }

        await host.StopAsync();
    }

    [CacheInterchangeName(nameof(MyCacheKey))]
    private sealed record MyCacheKey(string Line) : ICacheKey, IInvalidatable
    {
        public bool IsInvalidatedBy(IInvalidationRule invalidationRule, out Func<Task>? invalidationCallback)
        {
            invalidationCallback = null;
            return invalidationRule is MyInvalidationRule myInvalidationRule && Line.StartsWith(myInvalidationRule.Prefix, StringComparison.Ordinal);
        }
    }

    [CacheInterchangeName(nameof(MyInvalidationRule))]
    private sealed record MyInvalidationRule(string Prefix) : IInvalidationRule
    {
        public InvalidationReason Reason => InvalidationReason.Created;
    }
}
