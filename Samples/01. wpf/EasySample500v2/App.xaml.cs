﻿#region using
using Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
//using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Debug;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
#endregion

namespace EasySample
{
    /// <summary>Interaction logic for App.xaml</summary>
    public partial class App : Application
    {
        static Type T = typeof(App);
        public IHost Host;
        private ILogger<App> _logger;

        static App()
        {
            //TraceManager.Init(null);
            //using (var sec = TraceManager.GetCodeSection(T))
            //{
            try
            {
                // sec.Debug("this is a debug trace");
                // sec.Information("this is a Information trace");
                // sec.Warning("this is a Warning trace");
                // sec.Error("this is a error trace");

                throw new InvalidOperationException("this is an exception");
            }
            catch (Exception ex)
            {
                //sec.Exception(ex);
            }
            //}
        }

        public App()
        {
        }
        protected override async void OnStartup(StartupEventArgs e)
        {
            var configuration = TraceLogger.GetConfiguration();
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
                        loggingBuilder.ClearProviders();

                        var options = new Log4NetProviderOptions();
                        options.Log4NetConfigFileName = "log4net.config";
                        var log4NetProvider = new Log4NetProvider(options);
                        loggingBuilder.AddDiginsight(log4NetProvider, configuration);

                    }).Build();

            _logger.Start(Host);
            using (var scope = _logger.BeginMethodScope())
            {
                // LogStringExtensions.RegisterLogstringProvider(this);
            }
            await Host.StartAsync();

            var mainWindow = Host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
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