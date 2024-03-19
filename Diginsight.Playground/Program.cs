using Diginsight.Diagnostics;
using Diginsight.Diagnostics.TextWriting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace Diginsight.Playground;

internal class Program : BackgroundService
{
    private static readonly ActivitySource ActivitySource = new (typeof(Program).Namespace!);

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
        DiginsightTextWriter.DisplayTiming = true;

        DiginsightActivitiesOptions diginsightActivitiesOptions = new ()
        {
            LogActivities = true,
            RecordSpanDurations = false,
        };
        IDeferredLoggerFactory loggerFactory = new DeferredLoggerFactory(activitiesOptions: diginsightActivitiesOptions);
        ILogger logger = loggerFactory.CreateLogger<Program>();
        ActivitySource deferredActivitySource = loggerFactory.ActivitySource;

        IHost host;
        using (deferredActivitySource.StartMethodActivity(logger))
        {
            host = new HostBuilder()
                .ConfigureAppConfiguration(
                    (_, configurationBuilder) =>
                    {
                        using Activity? innerActivity = deferredActivitySource.StartRichActivity(logger, "ConfigureAppConfiguration");

                        logger.LogDebug("Json file");
                        configurationBuilder.AddJsonFile("appsettings.json");
                    }
                )
                .ConfigureServices(
                    (hostBuilderContext, services) =>
                    {
                        using Activity? innerActivity = deferredActivitySource.StartRichActivity(logger, "ConfigureServices");

                        IConfiguration configuration = hostBuilderContext.Configuration;

                        logger.LogDebug("Misc");
                        services
                            .AddSingleton<ILineTokenParser>(new SimpleTokenParser("processid", ProcessIdToken.Instance))
                            .AddHostedService<Program>();

                        const string diginsightSectionName = "Diginsight";

                        logger.LogDebug("Diginsight");
                        services
                            .ConfigureClassAware<DiginsightActivitiesOptions>(configuration, diginsightSectionName)
                            .AddDiginsight()
                            .WithTracing(
                                tracerProviderBuilder => tracerProviderBuilder
                                    .AddDiginsight()
                                    .AddSource(ActivitySource.Name)
                                    .AddSource(deferredActivitySource.Name)
                            );

                        logger.LogDebug("Logging");
                        services
                            .AddLogging(
                                loggingBuilder =>
                                {
                                    loggingBuilder
                                        .AddConfiguration(configuration.GetSection("Logging"))
                                        .AddDiginsightConsole(configuration.GetSection($"{diginsightSectionName}:Console").Bind);
                                }
                            );
                    }
                )
                .UseDiginsightServiceProvider((_, serviceProviderOptions) => { serviceProviderOptions.DeferredLoggerFactory = loggerFactory; })
                .Build();

            logger.LogDebug("Host built");
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

            using (Activity? activity = ActivitySource.StartMethodActivity(logger, new Dictionary<string, object> { ["SomeInput"] = "hola" }, logLevel: LogLevel.Information))
            {
                logger.LogWarning($"bar {404:x} {{paperino}}");
                activity.StoreOutput(Math.E);
            }

            _ = Console.ReadLine();

            using (Activity? activity = ActivitySource.StartMethodActivity(logger, new Dictionary<string, string> { ["SomeInput"] = "hello" }, logLevel: LogLevel.Information))
            {
                using (ActivitySource.StartRichActivity(logger, "Deep"))
                {
                    logger.LogWarning($"baz {409:x} {{pluto}}");
                }

                activity.StoreOutput(Math.PI);
                activity.StoreNamedOutputs(new { statusCode = HttpStatusCode.Conflict, character = "pluto" });
            }

            using (Activity? activity = ActivitySource.StartMethodActivity(logger, logLevel: LogLevel.Information))
            {
                logger.LogWarning($"quux {2023:x} {{topolino}}");
                activity.StoreNamedOutputs(new Dictionary<string, int>() { ["day"] = 10, ["month"] = 11, ["year"] = 2023 });
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
