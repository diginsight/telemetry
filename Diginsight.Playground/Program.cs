﻿using Diginsight.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using System.Diagnostics;
using System.Net;

namespace Diginsight.Playground;

internal class Program : BackgroundService
{
    private static readonly ActivitySource ActivitySource = new ("Default");

    private readonly ILogger logger;
    private readonly IHostApplicationLifetime applicationLifetime;

    public Program(
        ILogger<Program> logger,
        IHostApplicationLifetime applicationLifetime
    )
    {
        this.logger = logger;
        this.applicationLifetime = applicationLifetime;
    }

    private static void Main()
    {
        IHost host = new HostBuilder()
            .ConfigureAppConfiguration(
                static (_, configurationBuilder) =>
                {
                    configurationBuilder.AddJsonFile("appsettings.json");
                }
            )
            .ConfigureServices(
                static (hostBuilderContext, services) =>
                {
                    IConfiguration configuration = hostBuilderContext.Configuration;

                    services
                        .AddObservability()
                        .WithTracing(
                            static tracerProviderBuilder => tracerProviderBuilder
                                .AddObservability()
                                .AddSource(ActivitySource.Name)
                                .SetSampler(new AlwaysOnSampler())
                                .AddConsoleExporter()
                        );

                    services
                        .AddLogging(
                            loggingBuilder =>
                                loggingBuilder
                                    .AddConfiguration(configuration.GetSection("Logging"))
                                    .AddObservabilityConsole(configuration.GetSection("ObservabilityConsole").Bind)
                        );

                    services.AddHostedService<Program>();
                }
            )
            .Build();

        host.Services.EnsureObservability();

        host.Run();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using (ActivitySource.StartMethodActivity(logger, new { SomeInput = "ciao" }, logLevel: LogLevel.Information))
        {
            logger.LogWarning($"foo {42:x} {{pippo}}");
        }

        using (ActivitySource.StartMethodActivity(logger, new Dictionary<string, object> { ["SomeInput"] = "hola" }, logLevel: LogLevel.Information))
        {
            logger.LogWarning($"bar {404:x} {{paperino}}");
            logger.StoreOutput(Math.E);
        }

        _ = Console.ReadLine();

        using (ActivitySource.StartMethodActivity(logger, new Dictionary<string, string> { ["SomeInput"] = "hello" }, logLevel: LogLevel.Information))
        {
            logger.LogWarning($"baz {409:x} {{pluto}}");
            logger.StoreOutput(Math.PI);
            logger.StoreNamedOutputs(new { statusCode = HttpStatusCode.Conflict, character = "pluto" });
        }

        using (ActivitySource.StartMethodActivity(logger, logLevel: LogLevel.Information))
        {
            logger.LogWarning($"quux {2023:x} {{topolino}}");
            logger.StoreNamedOutputs(new Dictionary<string, int>() { ["day"] = 10, ["month"] = 11, ["year"] = 2023 });
        }

        applicationLifetime.StopApplication();

        return Task.CompletedTask;
    }
}