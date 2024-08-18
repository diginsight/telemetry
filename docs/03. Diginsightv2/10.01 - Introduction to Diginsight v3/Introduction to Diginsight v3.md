# INTRODUCTION 

__Diginsight v3__ introcuces the following key changes to diginsight telemetry:
- Stronger integration with standard __.Net Diagnostics__ and __ILogger__ API.
- Stronger integration with Opentelemetry __(Traces. Metrics. logs)__ and __Azure Monitor (Dashboards, Application Map, Transaction details)__.
- Support for __distributed tracing__ by means of W3C headers __traceparent__, __trace-id__ and __span-id__.
- __Higher performance__ and __telemetry volumes optimization__.
- __Automatic (customizable) rendering__ of __method payloads__ and __return values__.
- Support to __dynamic configuration__ of __log levels__.
- Support to __smart sampling__ to limit telemetry volumes.
- TBD: Automatic rendering of outbound calls (http, cosmosdb, ... ).
- a new __Diginsight.SmartCache__ component is available that leverages diginsight v3.

<br>

# Stronger Integration with Standard __.Net Diagnostics__ and __ILogger__ API

Adding .Net instrumentation is a matter of defining __System.Diagnostics activity sources__, creating activity scopes and logging statements:

```c#
static async Task<int> DoSomeWork(string foo, int bar)
{
    using var activity = source.StartActivity($"DoSomeWork({foo}, {bar})");

    var result1 = await StepOne(); 
    logger.LogDebug($"await StepOne(); returned {result1}");
    
    var result2 = await StepTwo(); 
    logger.LogDebug($"await StepTwo(); completed {result2}");

    var result = result1 + result2;
    return result;
}
```

with __Diginsight v3__ activity scopes can be created with similar notation, __without need of manually creating a string with method name and parameters__ information: 
```c#
static async Task DoSomeWork(string foo, int bar)
{
    using var activity = source.StartMethodActivity(logger, new { foo, bar });

    var result1 = await StepOne(); 
    logger.LogDebug($"await StepOne(); returned {result1}");
    
    var result2 = await StepTwo(); 
    logger.LogDebug($"await StepTwo(); completed {result2}");

    var result = result1 + result2;

    activity.StoreOutput(result);
    return result;
}
```

the above code will produce the following output:
```c#
text based stream log
```

Data shown on text based readable flow __is consistent__ with the originating code.


# Stronger integration with __Opentelemetry__ (Traces. Metrics. logs) and __Azure Monitor__ (Dashboards, Application Map, Transaction details)

Enabling __Opentelemetry__ the same information visible on the log files can be made available on remote tools as __Traces, Metrics and logs__.

the following image shows a transaction details with the entire flow of a single call:
```c#
transaction flow of a single call
```

__Metrics__ can be enabled to measure __duration of any method call__.<br>
In the following examle metrics are shown with latencies of methods `PlantWidgetController.Get` and `WidgetDataService.GetDataAsync` .
![Alt text](<01. Azure Monitor Dashboard with metrics.png>)

The following image shows the application map associated to an application execution where interacting application components are put in evidence:
```c#
application map diagram
```
Data shown on remote tools __is consistent__ with data on __text based readable log__ and the __originating code__.


# Supports Distributed tracing by means of W3C headers __traceparent__, __trace-id__ and __span-id__
Diginsight takes advantage of __W3C Trace Context specifications__ header __traceparent__ to maintain __trace-id__ and __span-id__ of every single call.

the example below shows a call received with __traceparent__
```c#
traceparent: 00-8652a752089f33e2659dff28d683a18f-7359b90f4355cfd9-01
trace-id:    00-8652a752089f33e2659dff28d683a18f
span-id:                                         7359b90f4355cfd9-01
```

where trace-id and span-id are properly associated to every execution span:
```c#
image with trace based stream log showing the trace-id and span-id
```

the following image shows the same transaction on azure monitor, that can be queried by means of the same ids:
```c#
image with trace based stream log showing the trace-id and span-id
```
# __Higher performance__ and __telemetry volumes optimization__

__Higher performance__ is obtained by means of optimized log strings generation.

__Telemetry volumes optimization__ is obtained by default, by limiting __time__ and __space__ used to generate every single trace.<br>
In case __string generation exceedes a configurable latency__ (in ticks) the log string is truncated and terminated with an ellipsis (...).<br>
Also, the log string is truncated and terminated with an ellipsis (...) __in case string generation exceedes a configurable space__ (in Bytes), .

when needed, the developer may require to avoid default string truncation 
as an example, when logging __queries__ or __important payload__.

```c#
NOTE: please note that disabling automatic truncation for a line may affect performance in time or space for telemetry
in case of need, please consider the following __best practices__: 
- big payload should not be logged frequently 
- big payload should be logged at Debug level or higher
```

__Telemetry volumes optimization__ can also be obtained applying 'Smart Sampling' as shown int the following paragraphs.

# __Automatic rendering__ of __method payloads__ and __return values__.
__Diginsight v3__ provides __automatic rendering__ of __method payloads__ and __return values__ by default.

when logging 
```c#
static async Task DoSomeWork(Identity identity)
{
    using (var activity = source.StartMethodActivity(logger, new { identity }));

    var result1 = await StepOne(); 
    logger.LogDebug($"await StepOne(); returned {result1}");
    
    var result2 = await StepTwo(); 
    logger.LogDebug($"await StepTwo(); completed {result2}");

    var result = result1 + result2;

    activity.StoreOutput(result);
    return result;
}
```

the __impunt payload__ or the __method result__ properties and type are rendered automatically into the text based log stream.

The developer can specify the order to be used when rendering entity properties and property __inclusions__ or __exclusions__ to make entity rendering more readable and meaningful.


# Support to __dynamic configuration__ of __log levels__
to minimize telemetry volumes, optimize performance and cost telemetry can be configured to be active only for __critical__ and __warning__ levels (optionally, for information level).

when troubleshooting is required calls to an application or API can be issued with headers 'Dynamic-Configuration'

![Alt text](<10. use Dynamic-Configuration header to Change log level configuration.png>)

# Support to __smart sampling__ to limit telemetry volumes.
__Smart sampling__ can be configured to specify that transactions execution be sent to azure monitor or other remote tools at a configurable percentage.

When configuring __Smart sampling__ a full transaction can be sampled dropped for sending to the remote tools.
So consistency and meaning is kept for data sent to the remote tools.







# Contribute
Contribute to the repository with your pull requests. 

- [ASP.NET Core](https://github.com/aspnet/Home)
- [Visual Studio Code](https://github.com/Microsoft/vscode)

# License
See the [LICENSE](LICENSE.md) file for license rights and limitations (MIT).
