using Azure.Monitor.OpenTelemetry.AspNetCore;
using Azure.Monitor.OpenTelemetry.Exporter;
using Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;

namespace EasySample
{
    public static class AddOpenTelemetryExtension
    {
        public static Type T = typeof(AddOpenTelemetryExtension);

        public static IApplicationBuilder UseObservability(this IApplicationBuilder app)
        {
            using var scope = TraceLogger.BeginMethodScope(T, new { app });

            app.UseOpenTelemetryPrometheusScrapingEndpoint();

            return app;
        }

        public static IServiceCollection AddObservability(this IServiceCollection services, IConfiguration configuration)
        {
            var assembly = App.Current.GetType().Assembly;
            
            string aiConnectionString = configuration.GetValue<string>(Constants.APPINSIGHTSCONNECTIONSTRING);
            string cloudRoleName = assembly.GetName().Name;
            string cloudRoleNamespace = assembly.GetName().FullName;
            string cloudRoleInstance = assembly.GetName().FullName;


            // https://learn.microsoft.com/en-us/azure/azure-monitor/app/opentelemetry-configuration?tabs=aspnetcore
            // Create a dictionary of resource attributes.
            var resourceAttributes = new Dictionary<string, object> {
                { "service.name", cloudRoleName },
                { "service.namespace", cloudRoleNamespace },
                { "service.instance.id", cloudRoleInstance }};

            //.AddTraceProvider();
            //services.AddOpenTelemetryTracing((builder) => builder
            //    // Configure the resource attribute `service.name` to MyServiceName
            //    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("MyServiceName"))
            //    // Add tracing of the AspNetCore instrumentation library
            //    .AddAspNetCoreInstrumentation()
            //    .AddConsoleExporter()
            //);

            services.AddOpenTelemetry().WithTracing(builder =>
            {
                builder.ConfigureResource(resourceBuilder => resourceBuilder.AddAttributes(resourceAttributes));

                builder.AddProcessor<ObservabilityLogProcessor>();
                builder.AddAspNetCoreInstrumentation();
                builder.AddHttpClientInstrumentation();
                //builder.AddConsoleExporter();
                builder.AddConsoleExporter(options => options.Targets = ConsoleExporterOutputTargets.Debug);
                builder.AddAzureMonitorTraceExporter();
                // builder.AddRedisInstrumentation();

                builder.AddSource("Azure.*");
                builder.AddSource(TraceLogger.ActivitySource.Name);
                // builder.AddSource(KeyVaultSampleMetrics.Instance.ObservabilityName);
                //builder.SetSampler(serviceProvider => new HttpHeaderSampler(serviceProvider, new ParentBasedSampler(new TraceIdRatioBasedSampler(openTelemetryOptions.TracingSamplingRatio))));
                //builder.AddOtlpExporter(options =>
                //    {
                //        options.Endpoint = new Uri(openTelemetryOptions.OltpEndpoint);
                //    }); 

            });
            //services.ConfigureOpenTelemetryTracerProvider((sp, builder) =>
            //{
            //    builder.ConfigureResource(resourceBuilder => resourceBuilder.AddAttributes(resourceAttributes));

            //    //builder.AddAspNetCoreInstrumentation();
            //    builder.AddHttpClientInstrumentation();
            //    //builder.AddConsoleExporter();
            //    builder.AddConsoleExporter(options => options.Targets = ConsoleExporterOutputTargets.Debug);
            //    builder.AddAzureMonitorTraceExporter();
            //    // builder.AddRedisInstrumentation();

            //    builder.AddSource("Azure.*");
            //    builder.AddSource(TraceLogger.ActivitySource.Name);
            //    builder.AddSource(KeyVaultSampleMetrics.Instance.ObservabilityName);
            //    //builder.SetSampler(serviceProvider => new HttpHeaderSampler(serviceProvider, new ParentBasedSampler(new TraceIdRatioBasedSampler(openTelemetryOptions.TracingSamplingRatio))));
            //    //builder.AddOtlpExporter(options =>
            //    //    {
            //    //        options.Endpoint = new Uri(openTelemetryOptions.OltpEndpoint);
            //    //    });
            //});

            services.ConfigureOpenTelemetryMeterProvider((sp, builder) =>
            {
                //builder.AddAspNetCoreInstrumentation();
                builder.AddHttpClientInstrumentation();
                builder.AddConsoleExporter();
                builder.AddPrometheusExporter();

                //builder.AddOtlpExporter();
                //builder.AddMetrics<KeyVaultSampleMetrics>();
                //builder.AddMeter(KeyVaultSampleMetrics.StaticObservabilityName);
                builder.AddAzureMonitorMetricExporter();
                builder.ConfigureResource(resourceBuilder => resourceBuilder.AddAttributes(resourceAttributes));
            });

            var builder = services.AddOpenTelemetry();

            builder.ConfigureResource(builder => builder.AddService(serviceName: cloudRoleName));

            //builder.WithMetrics(builder => builder
            //    .AddMeter(AverageLoadAdapter.ObservabilityName)
            //    .AddMeter(NextMaintenanceDateAdapter.ObservabilityName)
            //    .AddMeter(WatticsService.ObservabilityName)
            //    .AddPrometheusExporter());

            //builder.WithTracing(tracing => tracing.AddSource(Startup.ActivitySource.Name));

            builder.UseAzureMonitor(options =>
            {
                options.ConnectionString = aiConnectionString;
            });

            return services;
        }
    }
}
