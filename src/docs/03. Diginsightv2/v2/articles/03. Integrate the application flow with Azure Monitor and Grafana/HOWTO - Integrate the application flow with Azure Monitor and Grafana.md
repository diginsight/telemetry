# INTRODUCTION 

__Application observability__ is about aggregating, correlating and analyzing the following key elements:<br>
-  __Logs__ with application execution details and data.
-  __The requests and operations structure__ (sometimes also referred as __Activity, Traces or Spans__) with the structure of application calls, related to an event or exception condition.
-  __Metrics__: numeric values (such as latencies, payload sizes, frequencies) that can be aggregated and correlated with the operations structure and the logs.

The image below shows examples about the __3 observability elements__ on Azure Monitor Performance Management (APM) Tools:<br><br>
![Alt text](<01. Opentelemetry elements.jpg>)
<!-- /images/other/ -->

__Application observability__ is evolving with the introduction of __Open Telemetry__.<br>
__Microsoft technologies for observability__ such as __Azure Monitor__ and __Application Insights__ are embracing __Open Telemetry__ as the future for instrumentation.<br>
[OpenTelemetry + Azure Monitor blog post](https://techcommunity.microsoft.com/t5/azure-observability-blog/opentelemetry-azure-monitor/ba-p/2737823)<br>
![Alt text](<02. Opentelemetry citatiion.jpg>)
<br>
For this reason, __connecting diginsight to OpenTelemetry__ allows reaching __Azure Monitor__ components and standard Open Telemetry targets such as __Azure Grafana__ and __Grafana custom implementations__ (eg. on Kubernetes).
<br><br>
In the following paragraphs we'll explore __how to integrate diginsight with OpenTelemetry__ and __how we can use grafana and Azure monitor components__ to analize our application behaviour.

# INTEGRATE DIGINSIGHT TO OPENTELEMETRY

1. In your `ConfigureServices` method add the `AddObservability()` method as shown below<br>
![Alt text](<03. AddObservability.png>)

    ```c#
    public void ConfigureServices(IServiceCollection services)
    {
        using var scope = _logger.BeginMethodScope(new { services });

        services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddSingleton<IParallelService, ParallelService>();
        services.AddClassConfiguration();
        services.AddObservability(Configuration); 

        services.AddControllersWithViews();
        services.AddRazorPages(); 
    }
    ```

2. then, implement the `AddObservability()` extension method as shown below:<br>
    ```c#
    public static IServiceCollection AddObservability(this IServiceCollection services, IConfiguration configuration)
    {
        var cloudRoleName = typeof(Program).Assembly.GetName().Name;
        var cloudRoleNamespace = typeof(Program).Assembly.GetName().FullName;
        var cloudRoleInstance = typeof(Program).Assembly.GetName().FullName;
        var aiConnectionString = configuration["ApplicationInsights:ConnectionString"];

        var resourceAttributes = new Dictionary<string, object> {
                { "service.name", cloudRoleName },
                { "service.namespace", cloudRoleNamespace },
                { "service.instance.id", cloudRoleInstance }};

        ObservabilityDefaults.ActivitySource = TraceLogger.ActivitySource;
        ObservabilityDefaults.Meter = EasySampleMetrics.Instance.Meter; 

        var builder = services.AddOpenTelemetry();
        builder = builder.WithTracing(builder =>
        {
            builder.ConfigureResource(resourceBuilder => resourceBuilder.AddAttributes(resourceAttributes));

            builder.AddProcessor<DurationMetricProcessor>();
            builder.AddAspNetCoreInstrumentation();
            builder.AddHttpClientInstrumentation();
            builder.AddConsoleExporter(options => options.Targets = ConsoleExporterOutputTargets.Debug);
            builder.AddAzureMonitorTraceExporter();

            builder.AddSource("Azure.*");
            builder.AddSource(TraceLogger.ActivitySource.Name);
        });

        builder = builder.WithMetrics(builder =>
        {
            builder.AddHttpClientInstrumentation();
            builder.AddConsoleExporter();
            builder.AddPrometheusExporter();

            builder.AddMetrics<EasySampleMetrics>();
            builder.AddMeter(EasySampleMetrics.StaticObservabilityName);

            builder.AddAzureMonitorMetricExporter();
            builder.ConfigureResource(resourceBuilder => resourceBuilder.AddAttributes(resourceAttributes));
        });

        builder = builder.ConfigureResource(builder => builder.AddService(serviceName: cloudRoleName));

        builder = builder.UseAzureMonitor(options =>
        {
            options.ConnectionString = aiConnectionString;
        });

        return services;
    }
    ```

where in this code:
- `services.AddOpenTelemetry();` is initializing __OpenTelemetry__ and returning a builder object that you can use to further configure telemetry processors, exporters, and instrumentation.
- `builder.WithTracing();` configures tracing for __OpenTelemetry__ :
    - __builder.ConfigureResource(builder => builder.AddAttributes(resourceAttributes));__: configures the resource attributes such as the "module names" that are shown on the application map. 
    - __builder.AddProcessor<DurationMetricProcessor>();__ adds a processor to the tracing pipeline to publish methods duration as metrics.
    - __builder.AddAspNetCoreInstrumentation(); and builder.AddHttpClientInstrumentation();__: add instrumentation for ASP.NET Core and HttpClient.
    - __builder.AddConsoleExporter(options => options.Targets = ConsoleExporterOutputTargets.Debug);__: This line is adding a console exporter with the target set to debug. Exporters send the telemetry data to a backend or destination.
    - __builder.AddAzureMonitorTraceExporter();__: adds the Azure Monitor exporter that sends methods (also Activities or Spans) information to AzureMonitor.
    - __builder.AddSource("Azure.*"); and builder.AddSource(TraceLogger.ActivitySource.Name);__: add sources of telemetry data, by default, Diginsight use TraceLogger.ActivitySource to send telemetry data to AzureMonitor.
        ```c#
        builder = builder.WithTracing(builder =>
        {
            builder.ConfigureResource(resourceBuilder => resourceBuilder.AddAttributes(resourceAttributes));

            builder.AddProcessor<ObservabilityLogProcessor>();
            builder.AddProcessor<DurationMetricProcessor>();
            builder.AddAspNetCoreInstrumentation();
            builder.AddHttpClientInstrumentation();
            builder.AddConsoleExporter(options => options.Targets = ConsoleExporterOutputTargets.Debug);
            builder.AddAzureMonitorTraceExporter();

            builder.AddSource("Azure.*");
            builder.AddSource(TraceLogger.ActivitySource.Name);
        });
        ```
- `builder.WithMetrics();` configures metrics for __OpenTelemetry__ :
    - __builder.AddMetrics<EasySampleMetrics>();__: adds a custom metrics class EasySampleMetrics. that is used to send __span_durations__ to OpenTelemetry.
    - __builder.AddMeter(EasySampleMetrics.StaticObservabilityName);__: adds a meter with the name "EasySampleBlazorv2.Server".
    - __builder.AddAzureMonitorMetricExporter();__: adds the Azure Monitor exporter that sends metrics information to AzureMonitor.
    - __builder.ConfigureResource(builder => builder.AddAttributes(resourceAttributes));__: configures the resource attributes for the metrics such as the module name that is sending the metrics.
- `builder.ConfigureResource(b => b.AddService(serviceName: cloudRoleName));` configures the entity that is producing the telemetry data, in this case the service name is the assembly name.
- `builder.UseAzureMonitor(options => {...})` connects the application to the Azure Monitor by means of the Azure Monitor Application Insight Connection String.<br><br>

# ANALYZE TELEMETRY FROM AZURE MONITOR
Diginsight sends methods durations to Azure Monitor as the __span_duration__ metric where the method name is set in the __span_name__ dimension.
you have fine grane control on which methods and which method letencies are sent by means of the `PublishMetrics` and `PublishFlow` configurations.

These settings are set to `false`, by default.
you can enable them in the `appsettings.json` for all classes, or at any specific class level as shown below:

```json
"AppSettings": {
    "PublishMetrics": false,
    "PublishFlow": false,
    "PlantWidgetController.PublishMetrics": true, // sets PublishMetrics to true for class PlantWidgetController
    "PlantWidgetController.PublishFlow": true, // sets PublishFlow to true for class PlantWidgetController
    "WidgetDataService.PublishMetrics": true,
    "WidgetDataService.PublishFlow": true,
    "WidgetDataService.GetWidget.PublishMetrics": true
}
```


In the following image we see how to create a chart with the average __span_duration__ of method __PlantWidgetController.Get__.
![Alt text](<04. ChartWithSpanDurationMetric.png>)

the same latencies can be analyzed in the following logs query of the everage latencies summarised bt :
![Alt text](<05. SpanDurationByAttribute.png>)

When analyzing exceptions on the Applicaiton Insights Application map the activities flow leading to the exception also shows the methods names that were published to OpenTelemetry.

![Alt text](<06. End to end transaction detail with method names.png>)

the same latencies can be shown on a grafana dashboard (or Managed Grafana dashboard on Azure)
![Alt text](<07. Diginsight latencies on a Grafana dashboard.png>)
<br><br>

# Build and Test 
You can easily test Diginsight integration with OpenTelemetry by means of the EasySampleBlazorv2 project:
- Clone diginsight repository
- Open and build solution Common.Diagnostics.sln. 
- Set the EasySampleBlazorv2.Server as startup project
- add the __connection string__ to your Application Insights resource in the project __secrets.json__ file<br><br>
![Alt text](<08. use EasySampleBlazorv2.png>)

run **EasySampleBlazorv2.Server** and open the log file in your **\Log** folder.

# Contribute
Contribute to the repository with your pull requests. 

- [ASP.NET Core](https://github.com/aspnet/Home)
- [Visual Studio Code](https://github.com/Microsoft/vscode)

# License
See the [LICENSE](LICENSE.md) file for license rights and limitations (MIT).
