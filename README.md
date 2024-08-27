# Introduction 
__Diginsight telemetry__ is a set .Net packages that that provides __automatic__ __observability__ for dotnet applications.<br> 
In particular, __the full application flow__ is made available to __local text based streams__ such as __traditional file logs__, the __Console Log__ or the __Azure Streaming Log__ and also to remote analysis tools such as __Azure Monitor__ and __Prometheus__/__Grafana__.

Diginsight allows __observability__ of the __full application lifecycle__, including __static methods__, __injection sequences__ and the __application startup__ and __shutdown sequences__ where configuration problems and much complexity are often hidden.

__Diginsight telemetry__ is produced by standard __ILogger<>__ and __System.Diagnostic activity__ classes so it integrates (without replacing) other logging systems telemetry. Also, __diginsight telemetry__ fully integrated with __Opentelemetry__ and the __W3C Trace Context__ Specification so __traceids__ are preserved across process invocations of a distributed system.

__Diginsight telemetry__ targets __all dotnet framework versions__ starting from __netstandard2.0__.<br>Samples are available on [telemetry_samples](https://github.com/diginsight/telemetry_samples) repository to demonstrate use of telemetry on __.net 4.8__ up to [__blazor webassembly__,]__.net6__ and __.net8+__ assemblies.
<br><br>

# Additional Information 
Diginsight telemetry is __readable__, __consistent__ and __efficient__:<br>
- __readable__: telemetry is __easily readable__ on `local troubleshooting tools`, `live server consoles` and `remote analysis tools` such as the Azure monitor. The generated application flow is __consistent with application code__ so that problems understanding is much simplified.<br>
- __consistent__: the application flow is __rendered consistently__ on `local troubleshooting tools`, where maximun flow detail is available, and to the `remote analysis tools` such as the Azure monitor, where metrics and data from past executions can be easily compared.<br>
- __efficient__: application flow is generated with fully optimized techniques (such as dynamic compilation). Also, the full application flow observability can be __enabled dynamically__ and __only on the specific executions__ that are under analysis. this ensures that diginsight can be leveraged with no pratical impact on application performance.<br>


The image below shows the text based stream associated to to a Web API call.
![Alt text](<src/docs/000.01 Full call on log4net.png>)

The following image shows the same call on the __Azure Monitor Transaction Detail__ where the call structure is shown as a hierarchy of __activities__ (also called __spans__) and __trace details__:
![Alt text](<src/docs/000.02 Full call on azmon transaction.png>)


Diginsight uses __dynamic logging__ to support __full observability on live environments__.<br>
Live environments logging level is normally limited to __Warning__ or __Information__ levels to limit telemetry volumes produced by the applications.<br>
With __dynamic logging__ Log level can be raied to debug or trace __for a single call__, for example, by means of the __Log-Level http headers__.

The image below shows a __call to a live environment__ where the log level is set to Debug or Trace for 2 categories:
![alt text](<src/docs/000.021a live environment request with loglevel debug.png>)

The image below shows the __live environment AKS console__ where __our call is traced with full datail__, __while other calls are being processed with limited Log level__.
![alt text](<src/docs/000.021b live environment request with loglevel debug.png>)


Performance information gathered by __diginsight__ can be analyzed in the form of __metrics__.<br>
The following image shows the __Azure Monitor Metrics__ dashboard where method invocations and latencies can be analized in __value__ and __frequency__:
![Alt text](<src/docs/000.03 span_duration azmon metrics.png>)<br><br>


 __Intelligent sampling__, __dynamic compilation__, __automatic truncation__ and other strategies are used to __maximize application efficiency__ and __minimize telemetry cost__<br>For these reasons __Local analysis__ and __analysis on the remote tools__ can be supported __without compromises on performance__ and  __without compromises on cost of telemetry__ in __test__ and __production__ environments.<br><br>
![alt text](<src/docs/001.03d NoPerformanceImpact.png>)<br>

>[HowTo: Use diginsight telemetry with no impact on Application performance an telemetry cost](<src/docs/02. Advanced/10.00 - Maximize application performance and minimize telemetry cost with diginsight.md>)<br>
>Explores how diginsight telemetry can be used without impact on __application performance__ and __telemetry cost__.<br>


# Example analysis

The following image shows a diginsight application flow on a text based stream for `DataAnalyticsReportsController.GetDevices` method.
The flow can be easily obtained from __developer machine log file__, or from `application live console` such as __Azure app streaming log__ or a __Kubernetes console log__:
![Alt text](<src/docs/002.01 diginsightv3 flow on textbased stream.png>)


Starting from its `traceid` (`42488cedb33da51726293a70c3463c71`), the same flow can be found as an __Azure Monitor Application transaction__:
![Alt text](<src/docs/002.02 diginsightv3 flow on azmon.png>)

from the image we can observe that __internal component calls are shown into the transaction flow__ and not just interactions across different components.<br>
Also, note that __the transaction flow structure is consistent__ with the transaction flow rendered on the live console log, where more detail is available.


Latencies for the same function can be analyzed in a chart with the `span_duration` metric, filtered on `DataAnalyticsReportsController.GetDevices` method.
![Alt text](<src/docs/002.03 diginsightv3 metric on azmon.png>)

In facts, the `span_duration` metric allows analyzing latencies of __any method__ within code.<br>
Also, we'll see that the developer can easily add __other metrics__ and __metric tags__ to split and compare values in different conditions (eg. by site properties, user properties etc).

# Learn more

The following articles provide __easy steps to integrate diginsight into our code__, how to __configure telemetry for the local text based strams__ and how to __configure telemetry for the remote analysis tools__.<br>
Also, details are provided to use its relevant features such as __Dynamic configuration__ and __Dynamic Logging__.<br>

Example code used in the articles is also available in the [telemetry_samples](https://github.com/diginsight/telemetry_samples) repository.


>- [Getting Started](<src/docs/00.01 - Getting Started.md>): explores __basic steps we can follow to integrate diginsight telemetry__<br>
>- [Observability Concepts](<src/docs/01. Concepts/00.01 - Observability Concepts.md>): Explores basic concepts for __application observability and Opentelemetry__.<br>
>- [HowTo: use dynamic logging to manage loglevel dynamically, at runtime](<src/docs/01. Concepts/11.00 - HowTo - Use Dynamic-Logging to manage loglevel dinamically at runtime.md>)<br>
>Explores how we can troubleshoot applications by means of __dynamic logging__.<br>
>- [HowTo: use dynamic configuration to manage configurations and feature flags dynamically, at runtime](<src/docs/01. Concepts/11.01 - HowTo - Use Dynamic-Configuration to manage configurations and feature flags dynamically at runtime.md>)<br>
>Explores how we can troubleshoot applications by means of __dynamic configuration__.<br>4
>- [HowTo: configure diginsight telemetry to the local text based streams](<src/docs/01. Concepts/12.00 - Configure diginsight telemetry to the local text based streams.md>)<br>
>Explores how we configure diginsight telemetry to the __local analysis tools__.<br>
>- [HowTo: configure diginsight telemetry to the remote tools](<src/docs/02. Advanced/09.00 - Configure diginsight telemetry to the remote tools.md>)<br>
>Explores how we configure diginsight telemetry to the __remote analysis tools__.<br>
>- [HowTo: maximize application performance and minimize telemetry cost with diginsight](<src/docs/02. Advanced/10.00 - Maximize application performance and minimize telemetry cost with diginsight.md>)<br>
>Explores diginsight telemetry can be used without impact on __application performance__ and __telemetry cost__.<br>

Advanced topics
>- [HowTo: customize entities rendering on diginsight log streams](<src/docs/02. Advanced/13.00 - Customize entities rendering on diginsight log streams.md>).
>- [HowTo: customize metrics sent to the remote tools](<src/docs/02. Advanced/14.00 - Customize metrics sent to the remote tools.md>).
>- [HowTo: customize diginsight log streams row content](<src/docs/02. Advanced/15.00 - Customize diginsight log streams row content.md>).
>- [HowTo: troubleshoot the startup sequence](<src/docs/02. Advanced/17.00 - Troubleshoot the startup sequence.md>).
>- [HowTo: use class aware configurations to support comonent level or class level configurations](<src/docs/02. Advanced/18.00 - HowTo - Use class aware configurations to support comonent level or class level configurations.md>).



# Previous versions
> __diginsight v3 is now available__<br>
> the following article describes improvements of diginsight v3 over the previous version.
[Introduction to Diginsight v3](<src/docs/03. Diginsightv2/10.01 - Introduction to Diginsight v3.md>).<br>
> Features such as __old frameworks support (eg. .Net Framework 4.5+)__ or __observability for startup and static sections__ may still have limited support on Diginsight v3.<br>
> In these cases the developer may decide to keep the old model.<br>
> Diginsight v2 will not be discontinued until feature parity is reached.<br>
> Documentation about v2 packages is still available here [Diginsight v2 documentation](<src/docs/03. Diginsightv2/v2/README.md>).<br>

<br>

# Summary
__diginsight telemetry__ is a set .Net packages that that provides __automatic__ __observability__ for dotnet applications.<br> 
In particular, __the full application flow__ is made available to __local text based streams__ such as __traditional file logs__, the __Console log__ or the __Azure Streaming log__.<br>

Enabling __Opentelemetry__, the same information can be made available to __remote tools__ for troubleshooting or performance analysis such as __Azure Monitor__ or __Grafana__. 

# Samples
You can start using diginsight telemetry by running the samples on the [telemetry_samples](https://github.com/diginsight/telemetry_samples) repository.

Article [HOWTO - Use Diginsight Samples](<src/docs/01. Concepts/15.00 - HowTo - Use Diginsight Samples.md>): explores how we can use diginsight samples to test and understand integration of Diginsight telemetry in our own projects.

![alt text](<src/docs/004.01a Diginsight samples solution.png>)

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
See the [LICENSE](<LICENSE>) file for license rights and limitations (MIT).
