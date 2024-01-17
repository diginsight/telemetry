using Diginsight.Diagnostics;
using Diginsight.Diagnostics.TextWriting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;
using System.Text;

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
        IDeferredLoggerFactory loggerFactory = new DeferredLoggerFactory();
        ILogger logger = loggerFactory.CreateLogger<Program>();
        ActivitySource mainActivitySource = loggerFactory.ActivitySource;

        IHost host;
        using (Activity? activity = mainActivitySource.StartMethodActivity(logger))
        {
            host = new HostBuilder()
                .ConfigureAppConfiguration(
                    (_, configurationBuilder) =>
                    {
                        using Activity? a0 = mainActivitySource.StartRichActivity(logger, "ConfigureAppConfiguration");

                        configurationBuilder.AddJsonFile("appsettings.json");
                    }
                )
                .ConfigureServices(
                    (hostBuilderContext, services) =>
                    {
                        using Activity? a0 = mainActivitySource.StartRichActivity(logger, "ConfigureServices");

                        IConfiguration configuration = hostBuilderContext.Configuration;

                        services
                            .AddSingleton<ILineTokenParser>(new SimpleTokenParser("processid", ProcessIdToken.Instance));

                        services
                            .AddObservability(configuration.GetSection("Observability").Bind)
                            .WithTracing(
                                static tracerProviderBuilder => tracerProviderBuilder
                                    .AddObservability()
                                    .AddSource(ActivitySource.Name)
                            );

                        services
                            .AddLogging(
                                loggingBuilder =>
                                {
                                    loggingBuilder
                                        .AddConfiguration(configuration.GetSection("Logging"))
                                        .AddObservabilityConsole(configuration.GetSection("Observability:Console").Bind);
                                }
                            );

                        services.AddHostedService<Program>();
                    }
                )
                .UseObservabilityServiceProvider((_, serviceProviderOptions) => { serviceProviderOptions.DeferredLoggerFactory = loggerFactory; })
                .Build();
        }

        host.Run();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        void Execute()
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
        }

        _ = Task.Run(Execute, stoppingToken);
        return Task.CompletedTask;
    }

    private sealed class ProcessIdToken : ILineToken
    {
        public static readonly ILineToken Instance = new ProcessIdToken();

        private ProcessIdToken() { }

        public void Apply(ref MutableLineDescriptor lineDescriptor)
        {
            lineDescriptor.Appenders.Add(Appender.Instance);
        }

        public ILineToken Clone() => this;

        private sealed class Appender : IPrefixTokenAppender
        {
            public static readonly IPrefixTokenAppender Instance = new Appender();

            private Appender() { }

            public void Append(StringBuilder sb, in LinePrefixData linePrefixData)
            {
                sb.Append($"{Process.GetCurrentProcess().Id,5}");
            }
        }
    }
}
