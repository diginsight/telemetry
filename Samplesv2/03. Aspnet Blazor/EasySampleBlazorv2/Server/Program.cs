#region using
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using Common;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.ApplicationInsights;
using Microsoft.Extensions.Logging.Debug;
using Microsoft.Extensions.Logging.AzureAppServices;
using OpenTelemetry.Trace;
#endregion

namespace EasySampleBlazorv2.Server
{
    public class Program
    {
        private static Type T = typeof(Program);

        public static void Main(string[] args)
        {
            using var scope = TraceLogger.BeginMethodScope(T);

            var builder = CreateHostBuilder(args)
                          .ConfigureLogging((context, loggingBuilder) =>
                          {
                              using var scopeInner = TraceLogger.BeginNamedScope(T, "ConfigureLogging.Callback");

                              loggingBuilder.AddConfiguration(context.Configuration.GetSection("Logging"));

                              loggingBuilder.ClearProviders(); scopeInner.LogDebug($"loggingBuilder.ClearProviders();");

                              var options = new Log4NetProviderOptions();
                              options.Log4NetConfigFileName = "log4net.config";
                              var log4NetProvider = new Log4NetProvider(options);
                              loggingBuilder.AddDiginsightFormatted(log4NetProvider, context.Configuration); scopeInner.LogDebug($"loggingBuilder.AddDiginsightFormatted(log4NetProvider, context.Configuration);");

                              var appInsightsKey = context.Configuration["AppSettings:AppInsightsKey"]; scopeInner.LogDebug(new { appInsightsKey });
                              if (!string.IsNullOrEmpty(appInsightsKey))
                              {
                                  TelemetryConfiguration telemetryConfiguration = new TelemetryConfiguration(appInsightsKey);
                                  ApplicationInsightsLoggerOptions appinsightOptions = new ApplicationInsightsLoggerOptions();
                                  var tco = Options.Create<TelemetryConfiguration>(telemetryConfiguration);
                                  var aio = Options.Create<ApplicationInsightsLoggerOptions>(appinsightOptions);
                                  loggingBuilder.AddDiginsightFormatted(new ApplicationInsightsLoggerProvider(tco, aio), context.Configuration); scopeInner.LogDebug($"loggingBuilder.AddDiginsightFormatted(new ApplicationInsightsLoggerProvider(tco, aio), {context.Configuration.GetLogString()});");
                                  //loggingBuilder.AddDiginsightJson(new ApplicationInsightsLoggerProvider(tco, aio), context.Configuration); scopeInner.LogDebug($"loggingBuilder.AddDiginsightJson(new ApplicationInsightsLoggerProvider(tco, aio), {context.Configuration.GetLogString()});");
                                  // loggingBuilder.AddFilter<ApplicationInsightsLoggerProvider>("", LogLevel.Debug);
                              }

                              //var debugProvider = new TraceLoggerDebugProvider();
                              //var traceLoggerProvider = new TraceLoggerFormatProvider(context.Configuration) { ConfigurationSuffix = "Debug" };
                              //traceLoggerProvider.AddProvider(debugProvider);
                              //loggingBuilder.AddProvider(traceLoggerProvider); scopeInner.LogDebug($"loggingBuilder.AddProvider(traceLoggerProvider);");

                              // loggingBuilder.AddAzureWebAppDiagnostics(); // STREAMING LOG not working ?

                              //var consoleProvider = new TraceLoggerConsoleProvider();
                              //var traceLoggerProviderConsole = new TraceLoggerFormatProvider(context.Configuration) { ConfigurationSuffix = "Console" };
                              //traceLoggerProviderConsole.AddProvider(consoleProvider);
                              //loggingBuilder.AddProvider(traceLoggerProviderConsole); // i.e. builder.Services.AddSingleton(traceLoggerProvider);

                              //var debugProvider = new DebugLoggerProvider();
                              //var traceLoggerProviderDebug = new TraceLoggerFormatProvider(context.Configuration) { ConfigurationSuffix = "Debug" };
                              //traceLoggerProviderDebug.AddProvider(debugProvider);
                              //loggingBuilder.AddProvider(traceLoggerProviderDebug); // i.e. builder.Services.AddSingleton(traceLoggerProvider);
                          });

            var host = builder.Build();
            _ = host.Services.GetRequiredService<TracerProvider>();

            host.InitTraceLogger();

            //var logger = host.GetLogger<Program>();
            //using (var scope = logger.BeginMethodScope())
            //{
            host.Run();
            //}
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            using var scope = TraceLogger.BeginMethodScope(T);

            var webHostBuilder = Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
            return webHostBuilder;
        }
    }
}
