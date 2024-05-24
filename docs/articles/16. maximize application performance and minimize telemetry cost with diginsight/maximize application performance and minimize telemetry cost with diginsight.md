# INTRODUCTION 
Diginsight brings __application behavior observability__ to the next step.<br>
In particular The '__full application flow__' is made available to local text based streams such as the '__Console log__' or the '__Streaming log__'; The same information can be made available to '__remote tools__' for troubleshooting or performance analysis such as '__Azure Monitor__' or '__Grafana__'.<br>

The following example shows the execution Web API call, .<br>
![alt text](<000.01 Full call on log4net.png>)

In the following paragraphs we'll understand how this can be obtained without impact on the application performance.<br>
Also, you will soon learn that diginsight can be of great help with identifying and reducing `high latency flows` and `redundant flows` within the application execution paths.<br>
So diginsight can greatly contribute to application performance optimization more than provide a limitation to it. 

> With article:<br>
[HOWTO - Make your application flow observable.md](HOWTO%20-%20Make%20your%20application%20flow%20observable.md)<br>
We'll explore __how we can make our application flow fully observable__.<br><br>

# Performance considerations

the following image includes key drivers used by diginsights to avoid performence impacts:

![alt text](<001.02 NoPerformanceImpact.png>)

## Driver n°1: No heap pressure when disabled
The following code snippet shows a method instrumented by means of __diginsight__ `System.Diagnostics` __activities__:
```c#
public async Task<IEnumerable<Plant>> GetPlantByIdCachedAsync(Guid id)
{
    using var activity = Program.ActivitySource.StartMethodActivity(logger, () => new { id });

    // Method implementation
    // ...

    activity?.SetOutput(plants);
    return plants;
}
```

When __disabling an activity source__, the activities for it are not created and `StartMethodActivity` returns `null`.<br>
Also, in case __logging__ or __payload rendering__ are disabled, if __delegate notation__ is used to provide the `StartMethodActivity` payload, the delegate is not used and __the payload class is not allocated__ into the heap.

![alt text](<001.03b No heap pressure.png>)

In such conditions, diginsight activities are __not at all generated or used__ and do not provide any performance impact to the overall application.

## Driver n°2: No processing for disabled logs 
Needless to say, when log is disabled, method payloads are not processed and no strings are generated for method spans __start__ and __completion__.

Also intermediate `logger.LogXxxx()` statements, when using `structured logging` notation do not involve any string composition.

```c#
public async Task<IEnumerable<Plant>> GetPlantByIdCachedAsync(Guid id)
{
    using var activity = Program.ActivitySource.StartMethodActivity(logger, () => new { id });

    // Method implementation
    // ...
    logger.LogInformation("Plant '{Name}' ({Id}) accessed", plant.Name, plant.Id)

    activity?.SetOutput(plants);
    return plants;
}
```

## Driver n°3: Intelligent sampling can be used to limit data sent to the remote tools
__TracingSamplingRatio__ option within `OpenTelemetry` section can be used to activate __intelligent sampling__.

With OpenTelemetry, a full execution within a component is identified as a __trace__.<br>
The image below shows an example __trace__ where all rows share the same __trace_id__.

![alt text](<001.04 End to end transaction flow.png>)

When an exevution flow is selected for sending to the remote tools, __all the rows within the flow are sent__.<br>
When an exevution flow is omitted, __all the rows within the flow are omitted__.<br>
This way. __consistency and readability__ of data sent to the remote tools is ensured.<br>
Also, __data sent to the remote tools is limited in size__, as well as its __cost__ and __performance impact__.

The image below shows an __end to end transation detail__ sent to the azure monitor where all the transaction flow is sent, regardless of the sampling ratio configured for the application.
![alt text](<001.04 Full call on azmon transaction-1.png>)

The configuration section below, specifies a `"TracingSamplingRatio": 0.1`.<br>
In such case, __only one execution flow should be sent__ to the remote tools, __out of 10__.

```c#
  "OpenTelemetry": {
    "EnableMetrics": true,
    "EnableTraces": true,
    "TracingSamplingRatio": 0.1,
    "ActivitySources": [
      "Azure.Cosmos.Operation",
      "Azure.Storage.Blobs.BlobBaseClient",
      "Microsoft.AspNetCore",
      "Diginsight.*",
      "ABB.*"
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

Reducing data volumes sent to the remote tools still __allows ability to analyze system behaviour and trends__.
At the same time, full troubleshooting capability is still available with the local troubleshooting tools (eg. the __console log__ or the __streaming log__).


## Driver n°4: Traces sent to the remote tools are higly configurable
Data sent to the remote tools can be configured by means of the `OpenTelemetry` section:
```c#
"OpenTelemetry": {
    "EnableMetrics": true,
    "EnableTraces": true,
    "TracingSamplingRatio": 0.1,
    "ActivitySources": [
        "Azure.Cosmos.Operation",
        "Azure.Storage.Blobs.BlobBaseClient",
        "Microsoft.AspNetCore",
        "Diginsight.*",
        "ABB.*"
    ]
},
```
- `EnableMetrics` (def. true): specifies whether metrics are sent to the remote tools
- `EnableTraces` (def. true): specifies whether traces are sent to the remote tools
- `TracingSamplingRatio` (def. 1): specifies the sampling ration for data sent to the remote tools.
- `ActivitySources`: identifies the sources enabled.

## Driver n°5: Metrics sent to the remote tools are higly configurable
the `span_duration` metric sent to the remote tools can be configured by means of the `RecordSpanDurations` class aware option.<br>
In particular, the `RecordSpanDurations` flag can be set at __assembly__, __namespace__ or __class__ granularity level.

The code snippet below specifies that `RecordSpanDurations` flag is set only for `Microsoft` and `Diginsight` namespaces:

```c#
"Diginsight": {
    "Activities": {
        "RecordSpanDurations": false,
        "RecordSpanDurations@Microsoft.*": true,
        "RecordSpanDurations@Diginsight.*": true
    }
}
```

# Additional best practices
When using diginsight it is important to follow normal guidelines of general good sense:

- limit using Method Scopes (and logging statements in general) on strict loops 
- limit using Method Scopes (and logging statements in general) on deeply recursive methods 

<br><br>

# Build and Test 
Clone the repository, open and build solution Diginsight.sln. 
run EasySample and open the log file in your **\Log** folder.

# Contribute
Contribute to the repository with your pull requests. 

- [ASP.NET Core](https://github.com/aspnet/Home)
- [Visual Studio Code](https://github.com/Microsoft/vscode)

# License
See the [LICENSE](LICENSE.md) file for license rights and limitations (MIT).
