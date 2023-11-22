using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Common;
using Microsoft.Extensions.Hosting;

namespace EasySampleBlazorv2.Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");

            builder.Logging.SetMinimumLevel(LogLevel.Trace);

            var serviceProvider = builder.Services.BuildServiceProvider();
            //// Gets the standard ILoggerProvider (i.e. the console provider)
            //var consoleProvider = serviceProvider.GetRequiredService<ILoggerProvider>();
            //Console.WriteLine($"loggerProvider: '{consoleProvider}'"); // default logger is WebAssemblyConsoleLogger

            // Creates a Trace logger provider
            var consoleProvider = new TraceLoggerConsoleProvider();
            var traceLoggerProvider = new TraceLoggerFormatProvider(builder.Configuration) { ConfigurationSuffix = "Console" };
            traceLoggerProvider.AddProvider(consoleProvider);

            // replaces the provider with the trace logger provider
            builder.Logging.ClearProviders();
            builder.Logging.AddProvider(traceLoggerProvider); //i.e. builder.Services.AddSingleton(traceLoggerProvider);

            serviceProvider = builder.Services.BuildServiceProvider();
            ILoggerFactory loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            Console.WriteLine($"loggerFactory: '{loggerFactory}'");
            // gets a logger from the ILoggerFactory
            var logger = loggerFactory.CreateLogger<Program>();

            Console.WriteLine($"logger: '{logger}'");

            using (var scope = logger.BeginMethodScope())
            {
                builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

                var host = builder.Build();
                Common.ActivityExtensions.InitTraceLogger(host.Services);

                await host.RunAsync();
            }
        }
    }
}
