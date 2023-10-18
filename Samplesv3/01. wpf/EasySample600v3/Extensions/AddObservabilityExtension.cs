using Azure.Monitor.OpenTelemetry.AspNetCore;
using Azure.Monitor.OpenTelemetry.Exporter;
using Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
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

        public static IServiceCollection AddObservability(this IServiceCollection services, string aiConnectionString, string cloudRoleNamespace, string cloudRoleName, string cloudRoleInstance)
        {
            using var scope = TraceLogger.BeginMethodScope(T, new { services, aiConnectionString, cloudRoleNamespace, cloudRoleName, cloudRoleInstance });

            // https://learn.microsoft.com/en-us/azure/azure-monitor/app/opentelemetry-configuration?tabs=aspnetcore
            // Create a dictionary of resource attributes.
            var resourceAttributes = new Dictionary<string, object> {
                { "service.name", cloudRoleName },
                { "service.namespace", cloudRoleNamespace },
                { "service.instance.id", cloudRoleInstance }};

            services.ConfigureOpenTelemetryTracerProvider((sp, builder) =>
            {
                builder.ConfigureResource(resourceBuilder => resourceBuilder.AddAttributes(resourceAttributes));
                
                builder.AddAspNetCoreInstrumentation();
                builder.AddHttpClientInstrumentation();
                builder.AddConsoleExporter();
                builder.AddAzureMonitorTraceExporter();
                // builder.AddRedisInstrumentation();

                builder.AddSource("Azure.*");
                builder.AddSource(App.ActivitySource.Name);
                builder.AddSource(ApplicationMetrics.Instance.ObservabilityName);
                //builder.SetSampler(serviceProvider => new HttpHeaderSampler(serviceProvider, new ParentBasedSampler(new TraceIdRatioBasedSampler(openTelemetryOptions.TracingSamplingRatio))));
                //builder.AddOtlpExporter(options =>
                //    {
                //        options.Endpoint = new Uri(openTelemetryOptions.OltpEndpoint);
                //    });
            });

            services.ConfigureOpenTelemetryMeterProvider((sp, builder) =>
            {
                builder.AddAspNetCoreInstrumentation();
                builder.AddHttpClientInstrumentation();
                builder.AddConsoleExporter();
                builder.AddPrometheusExporter();

                //builder.AddOtlpExporter();
                builder.AddMetrics<ApplicationMetrics>();
                builder.AddMeter(ApplicationMetrics.StaticObservabilityName);
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
