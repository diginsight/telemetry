using System.Diagnostics;
using System.Text;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Azure.Monitor.OpenTelemetry.Exporter;
//using Azure.Monitor.OpenTelemetry.AspNetCore;
using Common;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace EasySampleBlazorv2.Server;


public class OpenTelemetryOptions
{
    public string LokiEndpoint { get; set; }
    public string OltpEndpoint { get; set; }

    public bool EnableTraces { get; set; } = false;
    public bool EnableLogs { get; set; } = false;
    public bool EnableMetrics { get; set; } = true;
    public double TracingSamplingRatio { get; set; } = 0.25;

    public string[] MetricsToDrop { get; set; } = Array.Empty<string>();
}

public static class AddOpenTelemetryExtension
{
    public static Type T = typeof(AddOpenTelemetryExtension);
    internal const string ENERGY_MANAGER_SERVICE = "easysample-blazor";

    public static IApplicationBuilder UseObservability(this IApplicationBuilder app)
    {
        using var scope = TraceLogger.BeginMethodScope(T, new { app });

        app.UseOpenTelemetryPrometheusScrapingEndpoint();

        return app;
    }

    public static IHostBuilder UseLokiLogging(this IHostBuilder hostBuilder, IConfiguration configuration)
    {
        //OpenTelemetryOptions openTelemetryOptions = new OpenTelemetryOptions();
        //configuration.GetSection(nameof(OpenTelemetryOptions)).Bind(openTelemetryOptions);

        //if (!openTelemetryOptions.EnableLogs)
        //{
        //    return hostBuilder;
        //}

        //return hostBuilder.UseSerilog((_, services, configuration) =>
        //    {
        //        configuration
        //            .MinimumLevel.Debug()
        //            .Enrich.FromLogContext()
        //            .Enrich.WithSpan()
        //            .WriteTo.GrafanaLoki(openTelemetryOptions.LokiEndpoint, labels: new List<LokiLabel> { new LokiLabel { Key = "service", Value = "energy-manager" } })
        //            .WriteTo.Console();
        //    });
        return null;
    }

    public static IServiceCollection AddObservability(this IServiceCollection services, IConfiguration configuration)
    {
        using var scope = TraceLogger.BeginMethodScope(T, new { services, configuration });
        
        // https://learn.microsoft.com/en-us/azure/azure-monitor/app/opentelemetry-configuration?tabs=aspnetcore
        // Create a dictionary of resource attributes.
        var cloudRoleName = typeof(Program).Assembly.GetName().Name;
        var cloudRoleNamespace = typeof(Program).Assembly.GetName().FullName;
        var cloudRoleInstance = typeof(Program).Assembly.GetName().FullName;
        var aiConnectionString = configuration["ApplicationInsights:ConnectionString"];


        var resourceAttributes = new Dictionary<string, object> {
                { "service.name", cloudRoleName },
                { "service.namespace", cloudRoleNamespace },
                { "service.instance.id", cloudRoleInstance }};

        ObservabilityDefaults.ActivitySource = TraceLogger.ActivitySource;
        ObservabilityDefaults.Meter = EasySampleMetrics.Instance.Meter;  // KeyVaultSample / SpanDuration

        //services.AddOpenTelemetryTracing((builder) => builder
        //    // Configure the resource attribute `service.name` to MyServiceName
        //    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("MyServiceName"))
        //    // Add tracing of the AspNetCore instrumentation library
        //    .AddAspNetCoreInstrumentation()
        //    .AddConsoleExporter()
        //);

        var builder = services.AddOpenTelemetry();
        builder = builder.WithTracing(builder =>
        {
            builder.ConfigureResource(resourceBuilder => resourceBuilder.AddAttributes(resourceAttributes));

            builder.AddProcessor<ObservabilityLogProcessor>();
            builder.AddProcessor<DurationMetricProcessor>();
            builder.AddAspNetCoreInstrumentation();
            builder.AddHttpClientInstrumentation();
            //builder.AddConsoleExporter();
            builder.AddConsoleExporter(options => options.Targets = ConsoleExporterOutputTargets.Debug);

            if (!string.IsNullOrEmpty(aiConnectionString)) { builder.AddAzureMonitorTraceExporter(); }

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

        builder = builder.WithMetrics(builder =>
        {
            //builder.AddAspNetCoreInstrumentation();
            builder.AddHttpClientInstrumentation();
            builder.AddConsoleExporter();
            builder.AddPrometheusExporter();

            //builder.AddOtlpExporter();
            //builder.AddMetrics<KeyVaultSampleMetrics>();
            //builder.AddMeter(KeyVaultSampleMetrics.StaticObservabilityName);
            builder.AddMetrics<EasySampleMetrics>();
            builder.AddMeter(EasySampleMetrics.StaticObservabilityName);

            if (!string.IsNullOrEmpty(aiConnectionString)) { builder.AddAzureMonitorMetricExporter(); }
                
            builder.ConfigureResource(resourceBuilder => resourceBuilder.AddAttributes(resourceAttributes));
        });
        //services.ConfigureOpenTelemetryMeterProvider((sp, builder) =>
        //{
        //    //builder.AddAspNetCoreInstrumentation();
        //    builder.AddHttpClientInstrumentation();
        //    builder.AddConsoleExporter();
        //    builder.AddPrometheusExporter();

        //    //builder.AddOtlpExporter();
        //    //builder.AddMetrics<KeyVaultSampleMetrics>();
        //    //builder.AddMeter(KeyVaultSampleMetrics.StaticObservabilityName);
        //    builder.AddAzureMonitorMetricExporter();
        //    builder.ConfigureResource(resourceBuilder => resourceBuilder.AddAttributes(resourceAttributes));
        //});

        //var builder = services.AddOpenTelemetry();

        builder = builder.ConfigureResource(builder => builder.AddService(serviceName: cloudRoleName));

        //builder.WithMetrics(builder => builder
        //    .AddMeter(AverageLoadAdapter.ObservabilityName)
        //    .AddMeter(NextMaintenanceDateAdapter.ObservabilityName)
        //    .AddMeter(WatticsService.ObservabilityName)
        //    .AddPrometheusExporter());

        //builder.WithTracing(tracing => tracing.AddSource(Startup.ActivitySource.Name));

        if (!string.IsNullOrEmpty(aiConnectionString))
        {
            builder = builder.UseAzureMonitor(options =>
            {
                options.ConnectionString = aiConnectionString;
            });
        }


        return services;
    }
    public static IServiceCollection AddObservability1(this IServiceCollection services, IConfiguration configuration)
    {
        AppContext.SetSwitch("Azure.Experimental.EnableActivitySource", true);

        OpenTelemetryOptions openTelemetryOptions = new OpenTelemetryOptions();
        configuration.GetSection(nameof(OpenTelemetryOptions)).Bind(openTelemetryOptions);

        //ObservabilityDefaults.ActivitySource = WidgetDataService.ActivitySource;
        //TraceLogger.ActivitySource = WidgetDataService.ActivitySource;
        //ObservabilityDefaults.Meter = WidgetDataMetrics.Instance.Meter;  // KeyVaultSample / SpanDuration
        //ActivitySource.AddActivityListener(new ActivityListener()
        //{
        //    ShouldListenTo = (source) => true,
        //    Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllDataAndRecorded,
        //    ActivityStarted = activity => { },
        //    ActivityStopped = activity => { }
        //});

        var builder = services.AddOpenTelemetry();

        //builder.ConfigureResource(resourceBuilder =>
        //{
        //    resourceBuilder.AddService(serviceName: ENERGY_MANAGER_SERVICE);

        //    if (DebugHelper.IsEnvironment("Development"))
        //    {
        //        resourceBuilder.AddAttributes(new Dictionary<string, object>
        //        {
        //            ["environment.name"] = "local",
        //            ["local.user"] = Environment.UserName
        //        });
        //    }
        //});

        if (openTelemetryOptions.EnableMetrics)
        {
            builder.WithMetrics(builder => builder
                    .AddAspNetCoreInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddHttpClientInstrumentation()
                    //.AddMeter(TraceLogger.ObservabilityName)

                    // adding em_ prefix to all the metrics
                    // now it is done at PodMonitor Level
                    // .AddView(instrument => new MetricStreamConfiguration { Name = $"{instrument.Name}" })

                    // Exponetial Histograms not yet supported by the prometheus exporter https://github.com/open-telemetry/opentelemetry-dotnet/issues/4800
                    // .AddView(instrument =>
                    // {
                    //     return instrument.GetType().GetGenericTypeDefinition() == typeof(Histogram<>)
                    //         ? new Base2ExponentialBucketHistogramConfiguration()
                    //         : null;
                    // })
                    .AddPrometheusExporter());
        }

        if (openTelemetryOptions.EnableTraces)
        {
            var cloudRoleName = typeof(Program).Assembly.GetName().Name;
            var cloudRoleNamespace = typeof(Program).Assembly.GetName().FullName;
            var cloudRoleInstance = typeof(Program).Assembly.GetName().FullName;

            var resourceAttributes = new Dictionary<string, object> {
                { "service.name", cloudRoleName },
                { "service.namespace", cloudRoleNamespace },
                { "service.instance.id", cloudRoleInstance }};


            builder.WithTracing(builder => builder
                .ConfigureResource(resourceBuilder => resourceBuilder.AddAttributes(resourceAttributes))
                .AddProcessor<ObservabilityLogProcessor>()
                .AddProcessor<DurationMetricProcessor>()
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.EnrichWithHttpRequest = (activity, httpRequest) =>
                    {
                        var context = httpRequest.HttpContext;

                        activity.DisplayName = $"{context.Request.Scheme.ToUpperInvariant()} {context.Request.Method.ToUpperInvariant()} {context.Request.Path}";

                        activity?.AddTag("http.client_ip", context.Connection.RemoteIpAddress);
                        activity?.AddTag("http.request_content_length", httpRequest.ContentLength);
                        activity?.AddTag("http.request_content_type", httpRequest.ContentType);
                    };
                    options.EnrichWithHttpResponse = (activity, httpResponse) =>
                    {
                        activity?.AddTag("http.response_content_length", httpResponse.ContentLength);
                        activity?.AddTag("http.response_content_type", httpResponse.ContentType);
                    };
                    options.EnrichWithException = (activity, exception) =>
                    {
                        activity?.SetTag("stack_trace", exception.StackTrace);
                    };
                })
                .AddHttpClientInstrumentation(options =>
                {
                    options.EnrichWithHttpRequestMessage = (activity, httpRequestMessage) =>
                    {
                        if (httpRequestMessage.Content is not null)
                        {
                            activity?.AddTag("http.request_content", Encoding.UTF8.GetString(httpRequestMessage.Content.ReadAsByteArrayAsync().Result));
                        }
                    };
                    options.EnrichWithHttpResponseMessage = (activity, httpResponseMessage) =>
                    {
                        var contentByteArray = httpResponseMessage.Content.ReadAsByteArrayAsync().Result;
                        activity?.AddTag("http.response_content_length", contentByteArray.Length);
                        if (!httpResponseMessage.IsSuccessStatusCode && httpResponseMessage.Content is not null)
                        {
                            activity?.AddTag("http.response_content", Encoding.UTF8.GetString(contentByteArray));
                        }
                    };
                    options.EnrichWithException = (activity, exception) =>
                    {
                        activity?.SetTag("stack_trace", exception.StackTrace);
                        activity?.SetTag("error", true);
                    };
                    options.FilterHttpRequestMessage = (httpRequestMessage) =>
                    {
                        return !httpRequestMessage.RequestUri.ToString().Contains("applicationinsights.azure.com");
                    };
                })
                //.AddRedisInstrumentation()
                .AddConsoleExporter(options => options.Targets = ConsoleExporterOutputTargets.Debug)
                .AddAzureMonitorTraceExporter()
                .AddSource("Azure.*")
                .AddSource(TraceLogger.ActivitySource.Name)
                .SetErrorStatusOnException()
            // .SetSampler(serviceProvider => new HttpHeaderSampler(serviceProvider, new ParentBasedSampler(new TraceIdRatioBasedSampler(openTelemetryOptions.TracingSamplingRatio))))
            // .AddConsoleExporter()
            //.AddOtlpExporter(options =>
            //{
            //    options.Endpoint = new Uri(openTelemetryOptions.OltpEndpoint);
            //})
            );

        }

        //ApplicationInsights:ConnectionString
        var aiConnectionString = configuration["ApplicationInsights:ConnectionString"];
        builder.UseAzureMonitor(options =>
        {
            options.ConnectionString = aiConnectionString;
        });


        return services;
    }
}