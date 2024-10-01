using Diginsight.Diagnostics;
using Diginsight.Diagnostics.TextWriting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;
using System.Text;
using ApplicationException = System.ApplicationException;

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
        //DiginsightTextWriter.DisplayTiming = true;

        DiginsightActivitiesOptions diginsightActivitiesOptions = new ()
        {
            LogActivities = true,
            RecordSpanDurations = false,
        };

        IDeferredLoggerFactory loggerFactory = new DeferredLoggerFactory(activitiesOptions: diginsightActivitiesOptions);
        loggerFactory.ActivitySourceFilter = static x => x == ActivitySource;
        ILogger logger = loggerFactory.CreateLogger<Program>();

        IHost host;
        using (ActivitySource.StartMethodActivity(logger))
        using (ActivitySource.StartRichActivity(logger, "Inner"))
        {
            host = new HostBuilder()
                .ConfigureAppConfiguration(
                    (_, configurationBuilder) =>
                    {
                        using Activity? innerActivity = ActivitySource.StartRichActivity(logger, "ConfigureAppConfiguration");

                        logger.LogDebug("Json file");
                        configurationBuilder.AddJsonFile("appsettings.json");
                    }
                )
                .ConfigureServices(
                    (hostBuilderContext, services) =>
                    {
                        using Activity? innerActivity = ActivitySource.StartRichActivity(logger, "ConfigureServices");

                        IConfiguration configuration = hostBuilderContext.Configuration;

                        logger.LogDebug("Misc");
                        services
                            .AddSingleton<ILineTokenParser>(new SimpleTokenParser("processid", ProcessIdToken.Instance))
                            .AddHostedService<Program>();

                        const string diginsightSectionName = "Diginsight";

                        logger.LogDebug("Diginsight");
                        services
                            .ConfigureClassAware<DiginsightActivitiesOptions>(configuration, diginsightSectionName);

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

                        services.FlushOnCreateServiceProvider(loggerFactory);
                    }
                )
                .UseDiginsightServiceProvider()
                .Build();

            logger.LogDebug("Host built");
        }

        host.Run();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        void Execute()
        {
            using Activity? rootActivity = ActivitySource.StartMethodActivity(logger, logLevel: LogLevel.Information);

            using (ActivitySource.StartRichActivity(logger, "First", new { SomeInput = "ciao" }))
            {
                logger.LogWarning($"foo {42:x} {{pippo}}");
            }

            using (Activity? activity = ActivitySource.StartRichActivity(logger, "Second", new Dictionary<string, object> { ["SomeInput"] = "hola" }))
            {
                logger.LogWarning($"bar {404:x} {{paperino}}");
                activity.SetOutput(Math.E);
            }

            using (Activity? activity = ActivitySource.StartRichActivity(logger, "Third", new Dictionary<string, string> { ["SomeInput"] = "hello" }))
            {
                using (ActivitySource.StartRichActivity(logger, "ThirdDeep"))
                {
                    logger.LogWarning($"baz {409:x} {{pluto}}");
                    Thread.Sleep(1000);
                }

                activity.SetOutput(Math.PI);
                activity.SetNamedOutputs(new { statusCode = HttpStatusCode.Conflict, character = "pluto" });
            }

            using (Activity? activity = ActivitySource.StartRichActivity(logger, "Fourth"))
            {
                logger.LogWarning($"quux {2023:x} {{topolino}}");
                activity.SetNamedOutputs(new Dictionary<string, int>() { ["day"] = 10, ["month"] = 11, ["year"] = 2023 });
            }

            try
            {
                throw new ApplicationException("Kaboom!");
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "explosion");
            }

            applicationLifetime.StopApplication();
        }

        TaskUtils.RunAndForget(Execute, stoppingToken);
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

            public void Append(StringBuilder sb, ref int length, in LinePrefixData linePrefixData, bool useColor)
            {
                sb.Append($"{Process.GetCurrentProcess().Id,5}");
                length += 5;
            }
        }
    }
}
