# INTRODUCTION 
__diginsight telemetry__ is a set .Net packages that that provides __automatic__ __observability__ for dotnet applications.<br> 
In particular, __the full application flow__ is made available to __local text based streams__ such as __traditional file logs__, the __Console log__ or the __Azure Streaming log__ and also to remote analysis tools such as __Azure Monitor__ and __Prometheus__/__Grafana__.<br>

The image below shows the text based stream associated to to a Web API call.
![Alt text](<000.01 Full call on log4net.png>)

The following image shows the same call on the __Azure Monitor Transaction Detail__ where the call structure is shown as a hierarchy of __activities__ (alaso called __spans__) and __trace details__:
![Alt text](<000.02 Full call on azmon transaction.png>)

Performance information gathered by __diginsight__ can be analyzed in the form of __metrics__.<br>
The following image shows the __Azure Monitor Metrics__ dashboard where method invocations and latencies can be analized in __value__ and __frequency__:
![Alt text](<000.03 span_duration azmon metrics.png>)


# ADDITIONAL INFORMATION 

Application flow observability is provided by means of __.Net__ __ILogger, System Diagnostics__ classes so that diginsight telemetry can be mixed and analyzed with other components telemetry, as long as they rely on the same standard framework classes.<br>
Observability for remote tools is provided by means of __OpenTelemetry__ so that telemetry data can be targeted to __Azure Monitor__ and also other analysis tools such as __Prometheus__/__Graphana__.

The following image shows diginsight metrics such as __span durations__  and __frequencies__ on a custom __Grafana__ dashboard receiving data by means of __Opentelemetry Prometheus__ stack.
![alt text](<001.00 Prometheus Grafana dashboard.png>)
<br>
<br>
Diginsight application flow is:
- __consistent across tools__: every information or metric visible on the __local text based streams__ can be published and observed on the __remote analysis tools__ (eg. on Appinsight Transaction detail or Appinsight Metrics).
- __consistent with code__: the application flow is published with information about classes, method names and call nesting so the __'gap' from telemetry and code__ is shortened for __application developers__ and __site reliability engineers__.
![alt text](<001.01 Consistency across tools and code.png>)

- __consistent across applications__ application flow published in the same way for all applications. so it is __easily readable for peopble without background knowledge__ on the application logic.
![alt text](<001.02 Consistency across applications.png>) 
<br><br>

Diginsight uses __dynamic logging__, __smart sampling__, __automatic truncation__ and other strategies to __maximize applications efficiency__ and __minimize telemetry cost__ so that __local analysis__ and __analysis on the remote tools__ can be supported __without compromises on performance__ and  __without compromises on cost of telemetry__.

![alt text](<001.03b NoPerformanceImpact.png>)

Diginsight __log layout__ and __automatic rendering__ for entities can be fully customized to ensure best readability of the application flow.

Paragraph [GETTING STARTED](#GETTING-STARTED) discusses basic steps we can follow to integrate diginsight telemetry.

The following articles explain in details:
- [HowTo: use dynamic logging to manage loglevel dynamically, at runtime](<articles/11. use Dynamic-Logging to manage loglevel dinamically at runtime/Use Dynamic-Logging to manage loglevel dinamically at runtime.md>).

- [HowTo: use Dynamic-Configuration to manage configurations and feature flags dynamically, at runtime](<articles/11.a use Dynamic-Configuration to manage configurations and feature flags dynamically at runtime/Use Dynamic-Configuration to manage configurations and feature flags dynamically at runtime.md>).

- [HowTo: Configure diginsight telemetry to the local text based streams](<articles/12. Configure diginsight telemetry to the local text based streams/Configure diginsight telemetry to the local text based streams.md>).

- [HowTo: Configure diginsight telemetry to the remote tools](<articles/12.a Configure diginsight telemetry to the remote tools/Configure diginsight telemetry to the remote tools.md>).

- [HowTo: customize entities rendering on diginsight log streams](<articles/13. Customize entities rendering on diginsight log streams/Customize entities rendering on diginsight log streams.md>).

- [HowTo: Customize metrics sent to the remote tools](<articles/14. Customize metrics sent to the remote tools/Customize metrics sent to the remote tools.md>).

- [HowTo: Customize diginsight log streams row content](<articles/15. Customize diginsight log streams row content/Customize diginsight log streams row content.md>).

- [HowTo: maximize application performance and minimize telemetry cost with diginsight](<articles/16. maximize application performance and minimize telemetry cost with diginsight/maximize application performance and minimize telemetry cost with diginsight.md>).

<br>

> __diginsight v3 is now available__<br>
> the following article describes improvements of diginsight v3 over the previous version.
[Introduction to Diginsight v3](<articles/10. Introduction to Diginsight v3/Introduction to Diginsight v3.md>).<br>
> Features such as old frameworks support (eg. .Net Framework 4.5+) or observability for startup and static sections may still have limited support on Diginsight v3.<br>
> In these cases the developer may decide to keep the old model.<br>
> Diginsight v2 will not be discontinued until feature parity is reached.<br>
> Documentation about v2 packages is still available here [Diginsight v2 documentation](<articles/v2/README.md>).<br>



# APPLICATION OBSERVABILITY CONCEPTS 
__Application observability__ is about aggregating, correlating and analyzing the following key elements:<br>
-  __Logs__ with application execution details and data.
-  __The requests and operations structure__ (sometimes also referred as __Activity, Traces or Spans__) with the structure of application calls, related to an event or exception condition.
-  __Metrics__: numeric values (such as latencies, payload sizes, frequencies) that can be aggregated and correlated with the operations structure and the logs.

The image below shows examples about the __3 observability elements__ on Azure Monitor Performance Management (APM) Tools:<br><br>
![alt text](<002.00 Opentelemetry elements.png>)<!-- /images/other/ -->

Diginsight __makes observability easy__ as:
- it __integrates the 3 observability elements__ (Log, Traces, Metrics) into high performance __text-based streams__.<br>
In particular, traditional File log, Console log or Azure Streaming Console log can be integrated with the full application execution flow.<br>
- it __publishes the 3 observability elements__ to OpenTelemetry and allowing application analysis by means of remote APM tools such as __Azure Monitor__ and __Grafana__.<br>
<br>

## Example analysis on Diginsight telemetry

The following image shows a diginsight application flow on a text based stream for `DataAnalyticsReportsController.GetDevices` method:
![Alt text](<002.01 diginsightv3 flow on textbased stream.png>)


Starting from its `traceid` (`42488cedb33da51726293a70c3463c71`), the same flow can be found as an __Azure Monitor Application transaction__:
![Alt text](<002.02 diginsightv3 flow on azmon.png>)

Latencies for the same function can be analyzed with the `span_duration` metric, filtered on `DataAnalyticsReportsController.GetDevices` method.
![Alt text](<002.03 diginsightv3 metric on azmon.png>)

In facts, the `span_duration` metric allows analyzing latencies of __any method__ within code.<br>
Also, we'll see that the developer can easily add __other metrics__ and __metric properties__ to split and compare values in different conditions (eg. by site properties, user properties etc).


# GETTING STARTED

With __version 3__ diginsight streamlines OpenTelemetry integration embracing standard notation for activity tracing with __dotnet System Diagnostic API__.

Using  __dotnet System Diagnostic API__ the following notation can be used to instrument a code span:
![alt text](<003.00 Code span with Opentelemetry.png>)

Using __diginsight v3__ the same section can be instrumented with the following notation:
![alt text](<003.01 Code span with diginsight.png>)

where, `StartMethodActivity`:
- gathers automatically the method name, 
- renders automatically the method payload
- writes the Span START and END to the logger variable 

and `SetOutput` stores  the method `result` for rendering within method END line.

# Steps to use Diginsight v3
You can ottain a __console log__ or __file log__ with diginsight by means of the following steps.<br>
The code snippets below are available as working samples within the [telemetry_samples](https://github.com/diginsight/telemetry_samples) repository.

Article [HOWTO - Use Diginsight Samples](<articles/04. HowTo Use Diginsight Samples/HOWTO - Use Diginsight Samples.md>): explores how we can use diginsight samples to test and understand integration of Diginsight telemetry in our own projects.


## STEP 01 - Add a package reference to the package __Diginsight.Diagnostics__ or __Diginsight.Diagnostics.Log4Net__
In the first step you can just add a diginsight reference to your code:<br>
![Alt text](<004.01 STEP1 - add reference.png>)

## STEP 02 - Configure logging within the Startup sequence
in the second step you can configure the startup sequence to enable  diginsight log:
![alt text](<004.02b STEP - configure logging.png>)

in this case: 
- __AddDiginsightConsole()__ is used to enabled log to the application Console.
- __AddDiginsightLog4Net()__ is used to enabled file log by means of log4net.

a separate - __log4net.config__ can be used to specify the usual log4net configuration:
![alt text](<004.03 Log4Net configuration file.png>)

also, the __Diginsight:Activities__ section can be used to specify __enabled ActivitySources__ and whether __Activity logging__ is enabled. <br>
![alt text](<004.04 DiginsightActivities configuration.png>)

## STEP 03 - Add telemetry to code with __StartMethodActivity()__ and __ILogger Statements__
we are now ready to add instrumentation to the code and make the application flow observable:
![alt text](<003.01 Code span with diginsight.png>)

## STEP 04 - Enable OpenTelemetry and send data to the remote tools
With few changes to the startup sequence, __telemetry can be sent to the remote tools__.
Telemetry to the local tools is less expensive, more efficient, well protected and often it is not even persisted.
So, telemetry to the local tools can include verbose data with the maximum level of information.<br>

Telemetry to the remote tools is more expensive (in cost and performance) so it will normally include only critical and warning non verbose information.

In our samples we enable openteemetry by means of the __AddObservability()__ extension method that essentially: 
- Configures __Opentelemetry options__
- Registers __Opentelemetry logging provider__
- Configures __tracing to the remote tools__
- Configures __metrics  to the remote tools__

![alt text](<004.00 AddObservability Extension method.png>)

details about opentelemetry configuration is available here:
[HowTo: Configure diginsight telemetry to the remote tools](<articles/13.a Configure diginsight telemetry to the remote tools/Configure diginsight telemetry to the remote tools.md>).

<br><br>


# Previous versions of diginsight
> __diginsight v3 is now available__<br>
> the following article describes improvements of diginsight v3 over the previous version.
[Introduction to Diginsight v3](<articles/10. Introduction to Diginsight v3/Introduction to Diginsight v3.md>).<br>
> Features such as __old frameworks support (eg. .Net Framework 4.5+)__ or __observability for startup and static sections__ may still have limited support on Diginsight v3.<br>
> In these cases the developer may decide to keep the old model.<br>
> Diginsight v2 will not be discontinued until feature parity is reached.<br>
> Documentation about v2 packages is still available here [Diginsight v2 documentation](<articles/v2/README.md>).<br>

<br>

# SUMMARY
__diginsight telemetry__ is a set .Net packages that that provides __automatic__ __observability__ for dotnet applications.<br> 
In particular, __the full application flow__ is made available to __local text based streams__ such as __traditional file logs__, the __Console log__ or the __Azure Streaming log__.<br>

Enabling __Opentelemetry__, the same information can be made available to __remote tools__ for troubleshooting or performance analysis such as __Azure Monitor__ or __Grafana__. 

# SAMPLES
You can start using diginsight telemetry by running the samples on the [telemetry_samples](https://github.com/diginsight/telemetry_samples) repository.

Article [HOWTO - Use Diginsight Samples](<articles/04. HowTo Use Diginsight Samples/HOWTO - Use Diginsight Samples.md>): explores how we can use diginsight samples to test and understand integration of Diginsight telemetry in our own projects.

![alt text](<004.01a Diginsight samples solution.png>)

# Build and Test 
You can easily test Diginsight integration with OpenTelemetry by means of the EasySampleBlazorv2 project:
- Clone diginsight repository
- Open and build solution Diginsight.sln. 
- build the solution



<br><br>

# Contribute
Contribute to the repository with your pull requests. 

- [ASP.NET Core](https://github.com/aspnet/Home)
- [Visual Studio Code](https://github.com/Microsoft/vscode)

# License
See the [LICENSE](LICENSE.md) file for license rights and limitations (MIT).
