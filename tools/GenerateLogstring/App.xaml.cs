using Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;

namespace GenerateLogstring
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static Type T = typeof(App);
        const string CONFIGVALUE_APPINSIGHTSKEY = "AppInsightsKey", DEFAULTVALUE_APPINSIGHTSKEY = "";

        public static IHost Host;
        private ILogger<App> _logger;

        static App()
        {
            using (var scope = TraceLogger.BeginMethodScope(T))
            {
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
        }

        public App()
        {
            using (var scope = Host.BeginMethodScope(T))
            {
            }
        }
        protected override async void OnStartup(StartupEventArgs e)
        {
            var logger = Host.GetLogger<App>();
            using (var scope = logger.BeginMethodScope())
            {
                var configuration = TraceLogger.GetConfiguration();
                //ConfigurationHelper.Init(configuration);
                //var appInsightKey = ConfigurationHelper.GetClassSetting<App, string>(CONFIGVALUE_APPINSIGHTSKEY, DEFAULTVALUE_APPINSIGHTSKEY); // , CultureInfo.InvariantCulture

                Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                        .ConfigureAppConfiguration(builder =>
                        {
                            builder.Sources.Clear();
                            builder.AddConfiguration(configuration);
                        }).ConfigureServices((context, services) =>
                        {
                            ConfigureServices(context.Configuration, services);
                        })
                        .ConfigureLogging((context, loggingBuilder) =>
                        {
                            var classConfigurationGetter = new ClassConfigurationGetter<App>(context.Configuration);
                            var appInsightKey = classConfigurationGetter.Get(CONFIGVALUE_APPINSIGHTSKEY, DEFAULTVALUE_APPINSIGHTSKEY);

                            loggingBuilder.AddConfiguration(context.Configuration.GetSection("Logging"));

                            loggingBuilder.ClearProviders();

                            //var consoleProvider = new TraceLoggerConsoleProvider();
                            //loggingBuilder.AddDiginsightFormatted(consoleProvider, configuration);

                            var options = new Log4NetProviderOptions();
                            options.Log4NetConfigFileName = "log4net.config";
                            var log4NetProvider = new Log4NetProvider(options);
                            loggingBuilder.AddDiginsightFormatted(log4NetProvider, configuration);

                            //var telemetryConfiguration = new TelemetryConfiguration(appInsightKey);
                            //var appinsightOptions = new ApplicationInsightsLoggerOptions();
                            //var tco = Options.Create<TelemetryConfiguration>(telemetryConfiguration);
                            //var aio = Options.Create<ApplicationInsightsLoggerOptions>(appinsightOptions);
                            ////loggingBuilder.AddDiginsightJson(new ApplicationInsightsLoggerProvider(tco, aio), configuration);
                            //loggingBuilder.AddDiginsightFormatted(new ApplicationInsightsLoggerProvider(tco, aio), configuration);

                        }).Build();

                Host.InitTraceLogger();

                // LogStringExtensions.RegisterLogstringProvider(this);

                await Host.StartAsync(); scope.LogDebug($"await Host.StartAsync();");

                var mainWindow = Host.Services.GetRequiredService<MainWindow>(); scope.LogDebug($"Host.Services.GetRequiredService<MainWindow>(); returns {mainWindow.GetLogString()}");
                mainWindow.Show(); scope.LogDebug($"mainWindow.Show();");

                base.OnStartup(e); scope.LogDebug($"base.OnStartup(e);");
            }
        }
        private void ConfigureServices(IConfiguration configuration, IServiceCollection services)
        {
            services.AddSingleton<MainWindow>();

            // var descriptor = new ServiceDescriptor(typeof(ILogger), typeof(TraceLogger), ServiceLifetime.Singleton);
            // services.Replace(descriptor);

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
    }
}
