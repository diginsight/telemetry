

# Introduction 
__Diginsight telemetry__ is a set .Net packages that that provides __automatic__ __observability__ for dotnet applications.<br> 
In particular, __the full application flow__ is made available to __local text based streams__ such as __traditional file logs__, the __Console Log__ or the __Azure Streaming Log__ and also to remote analysis tools such as __Azure Monitor__ and __Prometheus__/__Grafana__.

>  
>  Diginsight allows __observability__ of the __full application lifecycle__, including __static methods__, __injection sequences__ and the __application startup__ and __shutdown sequences__ where configuration problems and much complexity are often hidden.
>  

__Diginsight telemetry__ is produced by standard __ILogger<>__ and __System.Diagnostic activity__ classes so it integrates (without replacing) other logging systems telemetry. Also, __diginsight telemetry__ is fully integrated with __Opentelemetry__ and the __W3C Trace Context__ Specification so __traceids__ are preserved across process invocations of a distributed system.

__Diginsight telemetry__ targets __all dotnet framework versions__ starting from __netstandard2.0__.<br>Samples are available on [telemetry.samples](https://github.com/diginsight/telemetry.samples) repository to demonstrate use of telemetry on __.net 4.8__ up to [__blazor webassembly__,]__.net6__ and __.net8+__ assemblies.
<br>
> __Diginsight telemetry__  can capture automatically __database queries__, __outgoing requests__ as well as __missing configuration data__.<br>
This makes it is an invaluable companion for troubleshooting problems related to missing or invalid data.
<br><br>
> __Diginsight Telemetry__ is __fully dynamic__: minimal data can be enabled by default, on live environments.<br>
Full execution flow observability can be enabled __on demand__ for `specific components` or `specific requests`.
<br>

 __Diginsight Telemetry__ fully dynamic and automatic nature can be a strong base for automatic problems analysis and resolution.

# Additional Information 
Diginsight telemetry is __readable__, __consistent__ and __efficient__:<br>

>- __readable__: telemetry is __easily readable__ on `local troubleshooting tools`, `live server consoles` and `remote analysis tools` such as the Azure monitor. The generated application flow is __consistent with application code__ so that problems understanding is much simplified.<br>
>- __consistent__: the application flow is __rendered consistently__ on `local troubleshooting tools`, where maximun flow detail is available, and to the `remote analysis tools` such as the Azure monitor, where metrics and data from past executions can be easily compared.<br>
>- __efficient__: application flow is generated with fully optimized techniques (such as dynamic compilation). Also, the full application flow observability can be __enabled dynamically__ and __only on the specific executions__ that are under analysis. this ensures that diginsight can be leveraged with no pratical impact on application performance.<br>


The image below shows the text based stream associated to to a Web API call.
![alt text](<src/docs/000.01 Full call on log4net.png>)

The following image shows the same call on the __Azure Monitor Transaction Detail__ where the call structure is shown as a hierarchy of __activities__ (also called __spans__) and __trace details__:
![alt text](<src/docs/000.02 Full call on azmon transaction.png>) 
Please, note that the transaction involves two services invocations.<br>
The first image reports the __detailed log stream__ for first web application call (`PlantsController.GetPlantsAsync`).<br>
The latter image shows the full transaction detail on Azure Monitor.<br>
Both invocations are correlated by the same traceId so, for every transaction, the: __the full flow is visible across all services invocations__.


__Full observability on live environments__ can be supported with __dynamic logging__.<br>
Live environments logging level is normally limited to __Warning__ or __Information__ to avoid any cost and performance impact from the telemetry produced by applications.<br>
With __dynamic logging__ Log level can be raied to debug or trace __for a single call__, for example, by means of the __Log-Level http headers__.

The image below shows a __call to a live environment__ where the log level is set to Debug or Trace for 2 categories:
![alt text](<src/docs/000.02.1 live environment request with loglevel debug.png>)

The image below shows the __live environment App Service console__ where __our call is traced with full datail__, __while other calls are being processed with limited Log level__.
![alt text](<src/docs/000.02.2 live environment request with loglevel debug.png>)

Performance information gathered by __diginsight__ can be analyzed in the form of __metrics__.<br>
The following image shows the __Azure Monitor Metrics__ dashboard where method invocations and latencies can be analized in __value__ and __frequency__: 
![alt text](<src/docs/000.03.1 span_duration azmon metrics.png>)<br>
<br> 

 __Intelligent sampling__, __dynamic compilation__, __automatic truncation__ and other strategies are used to __maximize application efficiency__ and __minimize telemetry cost__<br>For these reasons __Local analysis__ and __analysis on the remote tools__ can be supported __without compromises on performance__ and  __without compromises on cost of telemetry__ in __test__ and __production__ environments.<br><br>
<!-- ![alt text](<src/docs/001.03d NoPerformanceImpact.png>)<br> -->

>[HowTo: Use diginsight telemetry with no impact on Application performance an telemetry cost](<src/docs/02. Advanced/10.00 - Maximize application performance and minimize telemetry cost with diginsight.md>)<br>
>Explores how diginsight telemetry can be used without impact on __application performance__ and __telemetry cost__.<br>


# Learn more

The following articles provide __easy steps to integrate diginsight into our code__, how to __configure telemetry for the local text based strams__ and how to __configure telemetry for the remote analysis tools__.<br>
Also, details are provided to use its relevant features such as __Dynamic configuration__ and __Dynamic Logging__.<br>

Example code used in the articles is also available in the [telemetry.samples](https://github.com/diginsight/telemetry.samples) repository.


>- [Getting Started](<src/docs/00. Getting Started/Getting Started.md>): explores __basic steps we can follow to integrate diginsight telemetry__<br>
>- [Example analysis](<src/docs/00.1 Example Analysis/00.01 - How to troubleshoot issues.md>): explores how diginsight can be used to __analyze applications and troubleshoot issues__.<br>
>- [Observability Concepts](<src/docs/01. Concepts/00.01 - Observability Concepts.md>): Explores basic concepts for __application observability and Opentelemetry__.<br>
>- [HowTo: configure diginsight telemetry to the local text based streams](<src/docs/01. Concepts/01.00 - Configure diginsight telemetry to the local text based streams.md>)<br>
>Explores how we configure diginsight telemetry to the __local analysis tools__.<br>
>- [HowTo: use dynamic logging to manage loglevel dynamically, at runtime](<src/docs/01. Concepts/11.00 - HowTo - Use Dynamic-Logging to manage loglevel dinamically at runtime.md>)<br>
>Explores how we can troubleshoot applications by means of __dynamic logging__.<br>
>- [HowTo: use dynamic configuration to manage dynamic options, at runtime](<src/docs/01. Concepts/11.01 - HowTo - Use dynamic configuration to manage dynamic options, at runtime.md>)<br>
>Explores how we can troubleshoot applications by means of __dynamic configuration__.<br>4
>- [HowTo: configure diginsight telemetry to the remote tools](<src/docs/01. Concepts/02.00 - HowTo - configure diginsight telemetry to the remote tools.md>)<br>
>Explores how we configure diginsight telemetry to the __remote analysis tools__.<br>
>- [HowTo: Use diginsight telemetry with no impact on Application performance an telemetry cost](<src/docs/01. Concepts/20.00 - HowTo Use diginsight telemetry with no impact on Application performance an telemetry cost.md>)<br>
>Explores diginsight telemetry can be used without impact on __application performance__ and __telemetry cost__.<br>

Advanced topics:

>- [HowTo: customize entities rendering on diginsight log streams](<src/docs/02. Advanced/13.00 - Customize entities rendering on diginsight log streams.md>):
>Explores how diginsight entities rendereing can be fully customized.<br>
>- [HowTo: customize metrics sent to the remote tools](<src/docs/02. Advanced/14.00 - Customize metrics sent to the remote tools.md>):
>Explores how custom metrics sent to the remote tools can be integrated and fully customized with tags.<br>
>- [HowTo: customize diginsight log streams row content](<src/docs/02. Advanced/15.00 - Customize diginsight log streams row content.md>):
>Explores how diginsight row content can be fully customized.<br>
>- [HowTo: troubleshoot the startup sequence](<src/docs/02. Advanced/16.00 - Troubleshoot the startup sequence.md>):
>Explores how full observability can be enabled on static methods and the startup sequence.<br>
>- [HowTo: use class aware configurations to support comonent level or class level configurations](<src/docs/02. Advanced/18.00 - Use class aware configurations to support comonent level or class level configurations.md>):
>Explores how class aware configurations can be used to implement feature flags or configurations that can be enabled with class level granularity.<br>

Team and contributors:
>- [About](<src/docs/05. About/about.md>): __Diginsight__ is a __team__ of __friends__ (__engineers__, __scientists__...) with passion for __technology__, __experimenting ideas__ and __excellence__. <br>


<br>

# Summary
Diginsight telemetry is a comprehensive suite of .NET packages 
designed to __provide automatic observability for .NET applications__. 
It enables developers to gain deep insights into the __full application lifecycle__, from startup to shutdown, including static methods and injection sequences. 

By leveraging standard `ILogger<>` and `System.Diagnostics` activity classes, 
Diginsight telemetry __integrates seamlessly with existing logging systems__ and __supports OpenTelemetry__ and the __W3C Trace Context Specification__.

Key features of Diginsight telemetry include:
- **Automatic Observability**: Capture detailed telemetry data without manual instrumentation.
- **Full Lifecycle Coverage**: Monitor application behavior from startup to shutdown.
- **Seamless Integration**: Works with existing logging systems and supports OpenTelemetry.
- **Dynamic Configuration**: Enable full observability on-demand for specific components or requests.
- **Performance Optimization**: Uses intelligent sampling, dynamic compilation, and other strategies to minimize performance impact and telemetry cost.

With Diginsight telemetry, __you can troubleshoot issues more effectively__, __understand application behavior__, and __ensure optimal performance__ in both test and production environments.

# Samples
You can start using diginsight telemetry by running the samples on the [telemetry.samples](https://github.com/diginsight/telemetry.samples) repository.

Article [HOWTO - Use Diginsight Samples](<src/docs/01. Concepts/15.00 - HowTo - Use Diginsight Samples.md>): explores how we can use diginsight samples to test and understand integration of Diginsight telemetry in our own projects.

![alt text](<src/docs/004.01 Diginsight samples solution.png>)

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
