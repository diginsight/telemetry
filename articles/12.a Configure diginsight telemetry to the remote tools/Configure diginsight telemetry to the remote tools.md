# HowTo: Configure diginsight telemetry to the remote tools

## INTRODUCTION 
__Diginsight__ is a very thin layer built on __.Net System.Diagnostics__ Activity API and __ILogger__ API.<br>
In particular, sending __.Net System.Diagnostics__ and __ILogger__ telemetry to remote tools by means of __OpenTelemetry__ and/or __Prometheus__ results in sending the full __diginsight application flow__ to them.

This article discusses how Diginsight telemetry can be sento to remote analysis tools such as __Azure Monitor__ or __Grafana__.<br>
Also, the article shows how such telemetry can be easily analyzed on __Azure Monitor__ tools such as the __Transaction Search__ and __Transaction Detail__, the Azure Monitor __Metrics__ and __Logs__ or __Azure Monitor Dashboards__.<br>

The code snippets below are available as working samples within the [telemetry_samples](https://github.com/diginsight/telemetry_samples) repository.

Article [HOWTO - Use Diginsight Samples.md](<../04. HowTo Use Diginsight Samples/HOWTO - Use Diginsight Samples.md>): explores how we can use diginsight samples to test and understand integration of Diginsight telemetry in our own projects.

## STEP 01 - Add a package reference to the package __Diginsight.Core__, __Diginsight.Diagnostics__ and __Diginsight.Diagnostics.AspNetCore.OpenTelemetry__

In the first step you can just add a diginsight references to your code:
![alt text](<001.01 Add Diginsight references1.png>)

## STEP 02 - Configure telemetry on the Startup sequence
The `SampleWebApi` sample shows an example WebApi fully integrated with OpenTelemetry and AzureMonitor.

The `Program.Main` entry point configures the the Startup class with the startup sequence methods and concludes the host build process with method `.UseDiginsightServiceProvider()`.

This ensures that the proper activity listeners are installed to generate the application flow.

```c#
public static void Main(string[] args)
{
    IWebHost host = WebHost.CreateDefaultBuilder(args)
        .AddKeyVault()
        .UseStartup<Startup>()
        //.ConfigureServices(s => s.FlushOnCreateServiceProvider(loggerFactory))
        .UseDiginsightServiceProvider()
        .Build();

    //logger.LogDebug("Host built");

    host.Run();
}
```
## STEP 03 - add `AddObservability` to enable OpenTelemetry and send data to the AzureMonitor

`AddObservability` includes all details to install Opentelemetry stack and activate the connection to __AzureMonitor__.

```c#
public void ConfigureServices(IServiceCollection services)
{
    services.AddHttpContextAccessor();
    services.AddObservability(configuration);

```


## STEP 04 (Optional) - add `AddDynamicLogLevel` enable support for request level dynamic logging

`AddDynamicLogLevel` enables use of __request level dynamic log__

```c#
public void ConfigureServices(IServiceCollection services)
{
    services.AddHttpContextAccessor();
    services.AddObservability(configuration);
    services.AddDynamicLogLevel<DefaultDynamicLogLevelInjector>();

```

## ADDITIONAL DETAILS

In this section we discuss the structure of `AddObservability` method and how it installs the necessary components to connect  diginsight Telemetry to the __Azure Monitor__.

in the first section 
- __Azure SDK__ is initialized
- __OpenTelemetry__ configuration section is installed
- __sampling__ and __metric recording__ are installed

```c#
public static IServiceCollection AddObservability(this IServiceCollection services, IConfiguration configuration)
{
    // this enables Azure SDK libraries to produce telemetry spans
    AppContext.SetSwitch("Azure.Experimental.EnableActivitySource", true);

    // assigns the application activity source to Diginsight
    DiginsightDefaults.ActivitySource = Program.ActivitySource;

    // reads and installs OpenTelemetry configuration
    IConfiguration openTelemetryConfiguration = configuration.GetSection("OpenTelemetry");
    OpenTelemetryOptions openTelemetryOptions = new();
    openTelemetryConfiguration.Bind(openTelemetryOptions);
    services.Configure<OpenTelemetryOptions>(openTelemetryConfiguration);
    services.PostConfigureFromHttpRequestHeaders<OpenTelemetryOptions>();

    // installs logging sampler to support intelligent sampling
    services.TryAddSingleton<IActivityLoggingSampler, HttpHeadersActivityLoggingSampler>();
    services.Decorate<IActivityLoggingSampler, MyActivityLoggingSampler>();

    // installs metrics recording (eg. span_duration metric)
    services.TryAddSingleton<ISpanDurationMetricRecorderSettings, MySpanDurationMetricRecorderSettings>();
    services.TryAddEnumerable(
        ServiceDescriptor.Singleton<IActivityListenerRegistration, MyDurationMetricRecorderRegistration<SpanDurationMetricRecorder>>());

    services.TryAddSingleton<ICustomDurationMetricRecorderSettings, MyCustomDurationMetricRecorderSettings>();
    services.TryAddEnumerable(
        ServiceDescriptor.Singleton<IActivityListenerRegistration, MyDurationMetricRecorderRegistration<CustomDurationMetricRecorder>>());

    // ActivitySourceDetectorRegistration ensures activity sources sending telemetry are traced
    services.TryAddEnumerable(
        ServiceDescriptor.Singleton<IActivityListenerRegistration, ActivitySourceDetectorRegistration>());


```

in a second section configures Logging for the local text based __Console__, __Log4Net__ and __AzureMonitor__.
Also, here Opentelemetry is initialized and the `Diginsight:Activities` section is read.
```c#
var azureMonitorConnectionString = configuration["ApplicationInsights:ConnectionString"];
services.AddLogging(
    loggingBuilder =>
    {
        loggingBuilder.ClearProviders();

        // enables logging to the console
        if (configuration.GetValue("AppSettings:ConsoleProviderEnabled", true))
        {
            loggingBuilder.AddDiginsightConsole();
        }

        // enables logging to Log4Net
        if (configuration.GetValue("AppSettings:Log4NetProviderEnabled", false))
        {
            loggingBuilder.AddDiginsightLog4Net("log4net.config");
        }

        // enables logging to Azure Monitor
        if (!string.IsNullOrEmpty(azureMonitorConnectionString))
        {
            loggingBuilder.AddOpenTelemetry(
                otlo => otlo.AddAzureMonitorLogExporter(
                    exporterOptions => { exporterOptions.ConnectionString = azureMonitorConnectionString; }
                )
            );
        }
    }
);

// reads the `Diginsight:Activities` section
services.ConfigureClassAware<DiginsightActivitiesOptions>(configuration.GetSection("Diginsight:Activities"));
services.PostConfigureFromHttpRequestHeaders<DiginsightActivitiesOptions>();

// enables OpenTelemetry connection
var builder = services.AddDiginsightOpenTelemetry();

```

if __Metrics__ are enabled the WithMetrics() section installs metrics from the runtime and from __Diginsight Meter__.
```c#
        if (openTelemetryOptions.EnableMetrics)
        {
            builder.WithMetrics(
                meterProviderBuilder =>
                {
                    meterProviderBuilder
                        .AddDiginsight()
                        .AddAspNetCoreInstrumentation()
                        .AddRuntimeInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddMeter(DiginsightDefaults.Meter.Name)
                        .AddPrometheusExporter();

                    if (!string.IsNullOrEmpty(azureMonitorConnectionString))
                    {
                        meterProviderBuilder.AddAzureMonitorMetricExporter(
                            exporterOptions => { exporterOptions.ConnectionString = azureMonitorConnectionString; }
                        );
                    }
                }
            );
        }
```
if __Traces__ are enabled the WithTracing() section installs the enabled __trace sources__ and __trace sampling__.

```c#
if (openTelemetryOptions.EnableTraces)
{
    builder.WithTracing(
        tracerProviderBuilder =>
        {
            tracerProviderBuilder
                .AddDiginsight()
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSource(openTelemetryOptions.ActivitySources.ToArray())
                .AddSource(Program.ActivitySource.Name)
                .SetErrorStatusOnException()
                .SetSampler(
                    static sp =>
                    {
                        OpenTelemetryOptions openTelemetryOptions = sp.GetRequiredService<IOptions<OpenTelemetryOptions>>().Value;
                        Sampler decoratee = new ParentBasedSampler(new TraceIdRatioBasedSampler(openTelemetryOptions.TracingSamplingRatio));
                        return ActivatorUtilities.CreateInstance<HttpHeadersSampler>(sp, decoratee);
                    }
                );

            if (!string.IsNullOrEmpty(azureMonitorConnectionString))
            {
                tracerProviderBuilder.AddAzureMonitorTraceExporter(
                    exporterOptions => { exporterOptions.ConnectionString = azureMonitorConnectionString; }
                );
            }
        }
    );
}
```

## CONFIGURATION
as with all .net applications logging categories can be enabled  by means of the `Logging` section:
```json
"Logging": {
    "LogLevel": {
        "Default": "Information",
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information",
        "*.IdentityLoggerAdapter": "None",
        "*.ServiceBusCacheCompanion": "None"
    }
},
```

`ApplicationInsights` section includes connection to the __Application Insight__.<br>
In case available, this information can be configured on a __Azure Key Vault__.
```json
"ApplicationInsights": {
    "ConnectionString": "", // Key Vault
    "IncludeEventId": false
},
```

the `OpenTelemetry` section includes the enabled activity sources for the remote tools (eg. __AzureMonitor__).
```json
"OpenTelemetry": {
    "EnableMetrics": true,
    "EnableTraces": true,
    "TracingSamplingRatio": 0.1,
    "ActivitySources": [
        "Azure.Cosmos.Operation",
        "Azure.Storage.Blobs.BlobBaseClient",
        "Microsoft.AspNetCore",
        "System.Net.Http",
        "SampleWebApi"
        //"Diginsight.*",
        //"*"
    ],
    "ExcludedHttpHosts": [
        "login.microsoftonline.com",
        ".documents.azure.com",
        ".applicationinsights.azure.com",
        ".monitor.azure.com",
        ".b2clogin.com"
    ],
    "DurationMetricTags": [
        "widget_template",
        "site_name"
    ]
},
```

the `Diginsight` section includes the enabled activity sources for logging (eg. __Console__, __Log4Net__).
```json
"Diginsight": {
    "Activities": {
        "ActivitySources": [
        "Azure.Cosmos.Operation",
        "Azure.Storage.Blobs.BlobBaseClient",
        "Microsoft.AspNetCore",
        "System.Net.Http",
        "SampleWebApi",
        //"Diginsight.*",
        "*"
        ],
        "LogActivities": true,
        "RecordSpanDurations": true
    }
},
```

`LogActivities` and `RecordSpanDurations` are class aware settings that can be used to specify which classes are expected to send __Span logs__ and the __Span duration metric__.

the `FeatureManagement` section includes example feature flags that can be used to reduce logs when not needed.

```json
"FeatureManagement": {
    "TraceRequestBody": false,
    "TraceResponseBody": false
},
```