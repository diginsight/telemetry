#region using
using Common;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Debug;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using static System.Formats.Asn1.AsnWriter;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using OpenTelemetry.Resources;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Azure.Monitor.OpenTelemetry.Exporter;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Refit;
using Polly;
#endregion

namespace EasySample
{
    /// <summary>Interaction logic for App.xaml</summary>
    public partial class App : Application
    {
        static Type T = typeof(App);
        const string CONFIGVALUE_APPINSIGHTSKEY = "AppInsightsKey", DEFAULTVALUE_APPINSIGHTSKEY = "";

        public static ActivitySource ActivitySource = new ActivitySource("EasySamplev3.App"); // , "1.0.0"

        public static IHost Host;
        private ILogger<App> _logger;

        static App()
        {
            using var scope = TraceLogger.BeginMethodScope(T);
            using Activity activity = ActivitySource.StartActivity(TraceLogger.GetMethodName());

            ActivitySource.AddActivityListener(new ActivityListener()
            {
                ShouldListenTo = (source) => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStarted = (Activity activity) => TraceLogger.LogDebug($"Started: {activity.OperationName} {activity.Id}"),
                ActivityStopped = (Activity activity) => TraceLogger.LogDebug($"Stopped: {activity.OperationName} {activity.Id} {activity.Duration}")
            });

            try
            {
                // sec.Debug("this is a debug trace");
                // sec.Information("this is a Information trace");
                // sec.Warning("this is a Warning trace");
                // sec.Error("this is a error trace");
                throw new InvalidOperationException("this is an exception");
            }
            catch (Exception /*ex*/) { /*sec.Exception(ex);*/ }
        }

        public App()
        {
            using Activity activity = ActivitySource.StartActivity(TraceLogger.GetMethodName());
            using (var scope = Host.BeginMethodScope(T))
            {
            }
        }
        protected override async void OnStartup(StartupEventArgs e)
        {
            using Activity activity = ActivitySource.StartActivity(TraceLogger.GetMethodName());
            var logger = Host.GetLogger<App>();
            using var scope = logger.BeginMethodScope();

            //using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            //                              .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("EasySample600v3"))
            //                              .AddSource(ActivitySource.Name)
            //                              .AddConsoleExporter()
            //                              .Build();

            ////// Create a new logger factory. It is important to keep the LoggerFactory instance active throughout the process lifetime.
            ////var loggerFactory = LoggerFactory.Create(builder =>
            ////{
            ////    var oTel = builder.AddOpenTelemetry(options =>
            ////    {
            ////        options.AddAzureMonitorLogExporter();
            ////    });
            ////    //oTel.UseAzureMonitor(options =>
            ////    //{
            ////    //    options.ConnectionString = "<Your Connection String>";
            ////    //});
            ////});

            //await DoSomeWork("banana", 8);

            var configuration = TraceLogger.GetConfiguration();
            var classConfigurationGetter = new ClassConfigurationGetter<App>(configuration);
            //var appInsightKey = ConfigurationHelper.GetClassSetting<App, string>(CONFIGVALUE_APPINSIGHTSKEY, DEFAULTVALUE_APPINSIGHTSKEY); // , CultureInfo.InvariantCulture

            Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                    .ConfigureAppConfiguration(builder =>
                    {
                        builder.Sources.Clear();
                        builder.AddConfiguration(configuration);
                        builder.AddUserSecrets<App>();
                        builder.AddEnvironmentVariables();
                    }).ConfigureServices((context, services) =>
                    {
                        ConfigureServices(context.Configuration, services);
                    })
                    .ConfigureLogging((context, loggingBuilder) =>
                    {
                        //var classConfigurationGetter = new ClassConfigurationGetter<App>(context.Configuration);
                        var appInsightKey = classConfigurationGetter.Get(CONFIGVALUE_APPINSIGHTSKEY, DEFAULTVALUE_APPINSIGHTSKEY);

                        loggingBuilder.AddConfiguration(context.Configuration.GetSection("Logging"));

                        loggingBuilder.ClearProviders();

                        var options = new Log4NetProviderOptions();
                        options.Log4NetConfigFileName = "log4net.config";
                        var log4NetProvider = new Log4NetProvider(options);
                        loggingBuilder.AddProvider(log4NetProvider); // , configuration
                        TraceLogger.InitConfiguration(configuration);

                        var connectionString = configuration["Logging:ApplicationInsights:ConnectionString"];

                        //loggingBuilder.AddApplicationInsights(configureTelemetryConfiguration: (config) =>
                        //        config.ConnectionString = configuration.GetConnectionString(connectionString),
                        //        configureApplicationInsightsLoggerOptions: (options) => { });

                        //options.Log4NetConfigFileName = "log4net.config";
                        //var log4NetProvider = new Log4NetProvider(options);
                        //loggingBuilder.AddProvider(log4NetProvider);
                        //loggingBuilder.AddDiginsightFormatted(log4NetProvider, configuration);

                        //var telemetryConfiguration = new TelemetryConfiguration(appInsightKey);
                        //var appinsightOptions = new ApplicationInsightsLoggerOptions();
                        //var tco = Options.Create<TelemetryConfiguration>(telemetryConfiguration);
                        //var aio = Options.Create<ApplicationInsightsLoggerOptions>(appinsightOptions);
                        ////loggingBuilder.AddDiginsightJson(new ApplicationInsightsLoggerProvider(tco, aio), configuration);
                        //loggingBuilder.AddDiginsightFormatted(new ApplicationInsightsLoggerProvider(tco, aio), configuration);

                        // appinsight metrics provider
                        // opentelemetry metrics provider

                    }).Build();

            Host.InitTraceLogger();

            // LogStringExtensions.RegisterLogstringProvider(this);

            await Host.StartAsync(); scope.LogDebug($"await Host.StartAsync();");

            var mainWindow = Host.Services.GetRequiredService<MainWindow>(); scope.LogDebug($"Host.Services.GetRequiredService<MainWindow>(); returns {mainWindow.GetLogString()}");

            mainWindow.Show(); scope.LogDebug($"mainWindow.Show();");

            base.OnStartup(e); scope.LogDebug($"base.OnStartup(e);");

        }
        private void ConfigureServices(IConfiguration configuration, IServiceCollection services)
        {
            //services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddHttpContextAccessor();
            services.AddClassConfiguration();

            var appSettingsSection = configuration.GetSection(nameof(AppSettings));
            var settings = appSettingsSection.Get<AppSettings>();

            services.AddRefitClient<ITestCachePreload>()
                .ConfigureHttpClient(client =>
                {
                    client.BaseAddress = new Uri(settings.CachePreload.BaseUrl);
                    client.Timeout = TimeSpan.FromMinutes(25); // TODO: reduce and implement async handling on runaggregate!
                })
                .AddTransientHttpErrorPolicy(builder => builder.WaitAndRetryAsync(new[]
                {
                            TimeSpan.FromSeconds(1),
                            TimeSpan.FromSeconds(5),
                            TimeSpan.FromSeconds(10)
                }));

            services.AddApplicationInsightsTelemetry();

            var connectionString = configuration["Logging:ApplicationInsights:ConnectionString"];
            var oTel = services.AddOpenTelemetry();
            oTel.UseAzureMonitor(options =>
            {
                options.ConnectionString = connectionString;
            });

            // Create a dictionary of resource attributes.
            var resourceAttributes = new Dictionary<string, object> {
                { "service.name", "my-service" },
                { "service.namespace", "my-namespace" },
                { "service.instance.id", "my-instance" }};

            // Configure the OpenTelemetry tracer provider to add the resource attributes to all traces.
            services.ConfigureOpenTelemetryTracerProvider((sp, builder) =>
                builder.ConfigureResource(resourceBuilder => resourceBuilder.AddAttributes(resourceAttributes)));


            //oTel.WithMetrics(metrics => metrics
            //    .AddMeter("Microsoft.AspNetCore.Hosting")
            //    .AddMeter("Microsoft.AspNetCore.Server.Kestrel"));

            //oTel.WithTracing(tracing =>
            //{
            //    tracing.AddSource(ActivitySource.Name);
            //});

            //// Configure the OpenTelemetry tracer provider to add a source named "ActivitySourceName". This will ensure that all activities created by the activity source are traced.
            //services.ConfigureOpenTelemetryTracerProvider((sp, builder) => builder.AddSource(ActivitySource.Name));
            //.AddMeter(greeterMeter.Name)

            services.AddScoped<ITraceLoggerMinimumLevel, TraceLoggerMinimumLevel>(sp =>
            {
                var ok = true;
                var traceLoggerMinimumLevel = default(TraceLoggerMinimumLevel);
                var traceLoggerMinimumLevelString = configuration["AppSettings:TraceLoggerMinimumLevel"] as string;
                if (traceLoggerMinimumLevelString is not null)
                {
                    ok = Enum.TryParse<LogLevel>(traceLoggerMinimumLevelString, out LogLevel minimumLevel);
                    if (ok)
                    {
                        if (traceLoggerMinimumLevel == null) { traceLoggerMinimumLevel = new TraceLoggerMinimumLevel(); }
                        traceLoggerMinimumLevel.MinimumLevel = minimumLevel;
                    }
                }

                var contextAccessor = Host.Services.GetService<IHttpContextAccessor>();
                if (contextAccessor == null) { return traceLoggerMinimumLevel; }

                ok = contextAccessor?.HttpContext?.Request?.Headers?.TryGetValue("TraceLoggerMinimumLevel", out StringValues headerValues) ?? false;
                if (ok)
                {
                    ok = int.TryParse(headerValues.LastOrDefault(), out int minimumLevel);
                    if (ok)
                    {
                        if (traceLoggerMinimumLevel == null) { traceLoggerMinimumLevel = new TraceLoggerMinimumLevel(); }
                        traceLoggerMinimumLevel.MinimumLevel = (LogLevel)minimumLevel;
                    }
                }
                return traceLoggerMinimumLevel;
            });
            services.AddSingleton<MainWindow>();

            //// Configure OpenTelemetry Resources with the application name
            //otel.ConfigureResource(resource => resource
            //    .AddService(serviceName: builder.Environment.ApplicationName));

            //// Add Metrics for ASP.NET Core and our custom metrics and export to Prometheus
            //otel.WithMetrics(metrics => metrics
            //    // Metrics provider from OpenTelemetry
            //    .AddAspNetCoreInstrumentation()
            //    .AddMeter(greeterMeter.Name)
            //    // Metrics provides by ASP.NET Core in .NET 8
            //    .AddMeter("Microsoft.AspNetCore.Hosting")
            //    .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
            //    .AddPrometheusExporter());

            //// Add Tracing for ASP.NET Core and our custom ActivitySource and export to Jaeger
            //otel.WithTracing(tracing =>
            //{
            //    tracing.AddAspNetCoreInstrumentation();
            //    tracing.AddHttpClientInstrumentation();
            //    tracing.AddSource(greeterActivitySource.Name);
            //    if (tracingOtlpEndpoint != null)
            //    {
            //        tracing.AddOtlpExporter(otlpOptions =>
            //        {
            //            otlpOptions.Endpoint = new Uri(tracingOtlpEndpoint);
            //        });
            //    }
            //    else
            //    {
            //        tracing.AddConsoleExporter();
            //    }
            //});

        }
        protected override async void OnExit(ExitEventArgs e)
        {
            using (Host)
            {
                await Host.StopAsync(TimeSpan.FromSeconds(5));
            }

            base.OnExit(e);
        }

        private string GetMethodName([CallerMemberName] string memberName = "") { return memberName; }


        // All the functions below simulate doing some arbitrary work
        static async Task DoSomeWork(string foo, int bar)
        {
            await StepOne();
            await StepTwo();
        }

        static async Task StepOne()
        {
            await Task.Delay(500);
        }

        static async Task StepTwo()
        {
            await Task.Delay(1000);
        }
    }


    public class TraceLoggerMinimumLevel : ITraceLoggerMinimumLevel
    {
        LogLevel _minimunLevel = LogLevel.Trace;
        public TraceLoggerMinimumLevel() { }

        public LogLevel MinimumLevel { get => _minimunLevel; set => _minimunLevel = value; }
    }
}
