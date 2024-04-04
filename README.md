# INTRODUCTION 
__diginsight telemetry__ is a set .Net packages that that provides __automatic__ __observability__ for dotnet applications.<br> 
In particular, __the full application flow__ is made available to __local text based streams__ such as __traditional file logs__, the __Console log__ or the __Azure Streaming log__.<br>

![Alt text](<07.0a Full call on log4net.png>)

Enabling __Opentelemetry__, the same information can be made available to __remote tools__ for troubleshooting or performance analysis such as __Azure Monitor__ or __Grafana__. 

The following image shows the same call on the azure monitor transaction detail with all the __activity spans__ and __trace details__:
![Alt text](<07.1a Full call on azmon transaction.png>)

Performance information can be analyzed in the form of metrics 
![Alt text](<07.1c span_duration azmon metrics.png>)

so that latencies and other numeric values exposed by code can be analyzed in __values__ and __frequency__.


# ADDITIONAL INFORMATION 

Application flow observability is provided by means of .Net __ILogger, System Diagnostics__ classes so that diginsight telemetry can be mixed and analyzed with other components telemetry, as long as they rely on the same standard framework classes.<br>
Observability for remote tools is provided by means of __OpenTelemetry__ so that telemetry data can be targeted to __Azure Monitor__ and also other analysis tools such as __Graphana__.
<br><br>
Diginsight application flow is __consistent__:
- __with code__: the application flow is published with information about classes, method names and call nesting so the __'gap' from telemetry and code__ is shortened for __application developers__ and __Site Reliability Engineers__.
- __across tools__: every information or metric visible on the __local text based streams__ can be published and observed on the __remote analysis tools__ (eg. for analysis of frequency of occurrence).
- __across applications__ application flow published in the same way for all applications. so it is __easily readable for peopble without background knowledge__ on the application logic.<br><br>

Diginsight is __efficient__ and __cost effective__ so that __local analysis__ and __analysis on the remote tools__ can be supported __without compromises on performance__ and  __without compromises on cost of telemetry__.

> __diginsight v3 is now available__<br>
> the following article describes improvements of diginsight v3 over the previous version 
[Introduction to Diginsight v3.md](<articles/10. Introduction to Diginsight v3/Introduction to Diginsight v3.md>).<br>
> Documentation about v2 packages is still available here [Diginsight v2 documentation.md](<articles/v2/README.md>).<br>

# APPLICATION OBSERVABILITY CONCEPTS 
__Application observability__ is about aggregating, correlating and analyzing the following key elements:<br>
-  __Logs__ with application execution details and data.
-  __The requests and operations structure__ (sometimes also referred as __Activity, Traces or Spans__) with the structure of application calls, related to an event or exception condition.
-  __Metrics__: numeric values (such as latencies, payload sizes, frequencies) that can be aggregated and correlated with the operations structure and the logs.

The image below shows examples about the __3 observability elements__ on Azure Monitor Performance Management (APM) Tools:<br><br>
![Alt text](<01. Opentelemetry elements.jpg>)
<!-- /images/other/ -->

Diginsight __makes observability easy__ as:
- it __integrates the 3 observability elements__ (Log, Traces, Metrics) into high performance __text-based streams__.<br>
In particular, traditional File log, Console log or Azure Streaming Console log can be integrated with the full application execution flow.<br>
- it __publishes the 3 observability elements__ to OpenTelemetry and allowing application analysis by means of remote APM tools such as __Azure Monitor__ and __Grafana__.<br>
<br>

## Example analysis on Diginsight telemetry

The following image shows a diginsight application flow on a text based stream with latency call for `DataAnalyticsReportsController.GetDevices` method:
![Alt text](<06.1 diginsightv3 flow on textbased stream.png>)


Starting from its `traceid` (`42488cedb33da51726293a70c3463c71`), the same flow can be found as an __Azure Monitor Application transaction__:
![Alt text](<06.2a diginsightv3 flow on azmon.png>)

Latencies for the same function can be analyzed with the `span_duration` metric, filtered on `DataAnalyticsReportsController.GetDevices` method.
![Alt text](<06.3a diginsightv3 metric on azmon.png>)

In facts, the `span_duration` metric allows analyzing latencies of __any method__ within code.<br>
Also, we'll see that the developer can easily add __other metrics__ and __metric properties__ to split and compare values in different conditions (eg. by site properties, user properties etc).

## Diginsight v3 packages
Diginsight is composed of the following packages:
![Alt text](<05. Diginsight v3 packages.png>)

telemetry is implemented by
- Diginsight.Core
- Diginsight.Diagnostics
- Diginsight.AspNetCore 
- Diginsight.Diagnostics.Log4Net
- Diginsight.Diagnostics.AspNetCore


You can learn how to integrate diginsight v3 into your code by means of the `Diginsight Samples`:

[HOWTO - Use Diginsight Samples.md](<articles/04. HowTo Use Diginsight Samples/HOWTO - Use Diginsight Samples.md>): explores how we can use diginsight samples to test and understand integration of Diginsight telemetry in our own projects.
<br><br>

# GETTING STARTED

With __version 3__ diginsight streamlines OpenTelemetry integration embracing standard notation for activity tracing with __dotnet System Diagnostic API__.

Using  __dotnet System Diagnostic API__ the following notation can be used to instrument a code span:

![Alt text](<06.0a Code span with Opentelemetry.png>)
Using __diginsight v3__ the same section can be instrumented with the following notation:

![Alt text](<06.1b Code span with diginsight.png>)

where, `StartMethodActivity`:
- gathers automatically the method name, 
- renders automatically the method payload
- writes the Span START and END to the logger variable 


# Steps to use Diginsight v3:
you can ottain a console log or file log with diginsight by means of the following steps.<br>
The following code snippets are available as working samples within the [telemetry_samples](https://github.com/diginsight/telemetry_samples) repository.

## STEP 01 - Add a package reference to the package __Diginsight.Diagnostics.AspNetCore__ or __Diginsight.Diagnostics.Log4Net__:
![Alt text](<08.1 STEP1 - add reference.png>)

## STEP 02 - Configure logging within the Startup sequence.

![Alt text](<08.2 STEP - configure logging.png>)


## STEP 03 - Add telemetry to code with __StartMethodActivity() and __ILogger Statements__:

![Alt text](<08.3 STEP - add telemetry to methods.png>)
<br><br>



## Tracing method parameters, variables and return values
When calling extensions methods `StartMethodActivity()` the method name is obtained by compiler generated information.<br>
You can __provide method parameters__ to the application flow by means of an __unnamed class__ in the __object payload parameter__.<br>

At the same way you can describe __variable values__ using the LogDebug overload with the __object payload parameter__.

also the __return value of a method scope__ can be tracked by means of the `activity.SetResult()` method.

The following image shows a method scope where parameters and variabes are tracked with the __object payload parameter__ and the return value is tracked with the `scope.Result` value
![alt text](/images/v2/01.8%20write%20parameters,%20variables%20and%20method%20result.jpg "Trace output").

The following image shows trace output of such traces:
![alt text](/images/v2/01.9%20write%20parameters,%20variables%20and%20method%20result%20-%20output.jpg "Trace output").


# Previous versions of diginsight
Current version of Diginsight telemetry provide support for Both __.Net Log providers__ and DotNet __Systen Diagnostics listeners__.<br>
The current document focused on using telemetry with __.Net Log providers__.<br>

[README.v1.md](README.v1.md) describes how to use Diginsight telemetry with standard DotNet __Systen Diagnostics listeners__.
<br><br>

# SUMMARY

__Common.Diagnostics__ is a .Net Standard component that provides readable log with __application execution flow__ to __.Net Log providers__ such as __Log4Net, Serilog or Application Insights, Console, EventLog and Debug__ __DotNet Log providers__.<br>

This makes the application flow fully observable, __still without compromises on performance__.<br>

1. [HOWTO - Make your application flow observable.md](HOWTO%20-%20Make%20your%20application%20flow%20observable.md) explores how to use Diginsight to fully expose our application exeution flow.

2. [HOWTO - Avoid performance impacts using diginsight telemetry.md](HOWTO%20-%20Avoid%20performance%20imacts%20using%20diginsight%20telemetry.md) explores how we can do this ensuring non impact on application performance.

# SAMPLES
You can start testing diginsight telemetry by running __EasySample600v2__ as shown below.
![Alt text](/images/v3/15.DiginsightClientSample.png)

as an alternative, you can start testing the __EasySampleBlazorv2.Server__ sample.
![Alt text](/images/v3/16.DiginsightServerSample.png)

# Build and Test 
You can easily test Diginsight integration with OpenTelemetry by means of the EasySampleBlazorv2 project:
- Clone diginsight repository
- Open and build solution Common.Diagnostics.sln. 
- Set the __EastSample600v2__ as the startup project
![Alt text](<03. EasySample600v2 project.png>)
- run the sample
run **EastSample600v2** and open the log file in your **\Log** folder.
![Alt text](<04. EasySample600v2 log file.png>)

<br><br>

# Contribute
Contribute to the repository with your pull requests. 

- [ASP.NET Core](https://github.com/aspnet/Home)
- [Visual Studio Code](https://github.com/Microsoft/vscode)

# License
See the [LICENSE](LICENSE.md) file for license rights and limitations (MIT).
