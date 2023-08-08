# INTRODUCTION 



__Common.Diagnostics__ is a .Net Standard component that provides readable log with __application execution flow__ to __.Net Log providers__ such as __Log4Net, Serilog or Application Insights, Console, EventLog and Debug__ __DotNet Log providers__.<br>
<br>
Basic execution flow is gathered by means of __compiler generated information__ (eg. `[CallerMemberName]` attribute); __String interpolation handlers__ and __delegate overloads__ are leveraged to remove impact of disabled logs entries.<br>
<br>
Start and completion of code sections are gathered by means of `using` statements to create __Method or Named scopes__.<br>
Traces are written to __standard .Net log providers__ so that applications can keep using their diagnostics system and standard logs are integrated into the execution flow gathered by __Common.Diagnostics__.<br><br>

Common.Diagnostics is supported by __any .Net Framework version__ supporting .Net Standard 2.0, __any .Net Log provider__ and __any Sistem.Diagnostic listener__.<br><br>
Examples are provided for __.NetCore 3.1+ and .Net Framework 4.6.2+ (including  .Net Framework 6.0)__ and __Blazor WebAssembly__.<br>
Examples show sending telemetry to  __Log4Net, Serilog or Application Insights, Console, EventLog and Debug__ __DotNet Log providers__.<br>
Also, examples show sending telemetry __Log4Net, Serilog, Console, Event Log and Application Insights__ and any other __Systen Diagnostics listeners__.
<br><br>


# GETTING STARTED
Steps to use Common.Diagnostics:
1.	Add a package reference to the package __Common.Diagnostics.1.0.\*.\*.nupkg__
2.	Add log providers in the __ConfigureLogging()__ callback and __InitTraceLogger()__ method
```c#
	.ConfigureLogging((context, loggingBuilder) =>
	{
		loggingBuilder.ClearProviders();

		var options = new Log4NetProviderOptions();
		options.Log4NetConfigFileName = "log4net.config";
		var log4NetProvider = new Log4NetProvider(options);
		loggingBuilder.AddDiginsightFormatted(log4NetProvider, configuration);

		var telemetryConfiguration = new TelemetryConfiguration(appInsightKey);
		var appinsightOptions = new ApplicationInsightsLoggerOptions();
		var tco = Options.Create<TelemetryConfiguration>(telemetryConfiguration);
		var aio = Options.Create<ApplicationInsightsLoggerOptions>(appinsightOptions);
		loggingBuilder.AddDiginsightFormatted(new ApplicationInsightsLoggerProvider(tco, aio), configuration);

		loggingBuilder.AddFilter<ApplicationInsightsLoggerProvider>("", LogLevel.Debug);
	}).Build();

	Host.InitTraceLogger();
```
in the previous section __standard log4net provider__ and __standard ApplicationInsight provider__ are configured to receive the execution flow, according to __standard .net logging configuration__.

3.	Add telemetry to your code with __BeginMethodScope(), BeginNamedScope()__ and __ILogger Statements__:
```c#
	- using var scope = _logger.BeginMethodScope(); // defines a method scope by means of an ILogger instance (class type is taken by the ILogger instance)
	- using var scope = _logger.BeginNamedScope("scopeName"); // defines a named scope within a method scope (eg. to describe loop code sections or async method callbacks).

	- using var innerScope = _logger.BeginMethodScope(new { configuration = configuration .GetLogString()}); // defines a method scope where method parameters are specified 
```
use the __scope variable__ to add trace messages to the method scope or the named scope
```c#
	// log statements within a scope
	- scope.LogTrace("this is a Trace trace");
	- scope.LogDebug("this is a Debug trace");
	- scope.LogInformation("this is a Information trace");
	- scope.LogWarning("this is a Warning trace");
	- scope.LogError("this is a error trace");
	- scope.LogCritical("this is a critical trace");
	- scope.LogException(ex);
```
use __standard ILogger statements__ or __TraceLogger static methods__ to add trace messages to the application flow when a scope variable instance is not available.

```c#
	// standard Ilogger statements:
	- _logger.LogTrace("this is a Trace trace");
	- _logger.LogDebug("this is a Debug trace");
	- _logger.LogInformation("this is a Information trace");
	- _logger.LogWarning("this is a Warning trace");
	- _logger.LogError("this is a error trace");
	- _logger.LogCritical("this is a critical trace");
	- _logger.LogException(ex);

	// log statements with TraceLogger static methods:
	- TraceLogger.LogTrace("this is a Trace trace");
	- TraceLogger.LogDebug("this is a Debug trace");
	- TraceLogger.LogInformation("this is a Information trace");
	- TraceLogger.LogWarning("this is a Warning trace");
	- TraceLogger.LogError("this is a error trace");
	- TraceLogger.LogCritical("this is a critical trace");
	- TraceLogger.LogException(ex);
```
in this case log traces are added to the most inner scope, for the current thread.

For performance optimization reasons, you may want to use the Log* overrides that work with delegates instead of using the Log* overrides that work with strings

```c#
	// standard Ilogger statements:
	- _logger.LogTrace(() => "this is a Trace trace");
	- _logger.LogDebug(() => "this is a Debug trace");
	- _logger.LogInformation(() => "this is a Information trace");
	- _logger.LogWarning(() => "this is a Warning trace");
	- _logger.LogError(() => "this is a error trace");
	- _logger.LogCritical(() => "this is a critical trace");
	- _logger.LogException(() => ex);

	// log statements with TraceLogger static methods:
	- TraceLogger.LogTrace(() => "this is a Trace trace");
	- TraceLogger.LogDebug(() => "this is a Debug trace");
	- TraceLogger.LogInformation(() => "this is a Information trace");
	- TraceLogger.LogWarning(() => "this is a Warning trace");
	- TraceLogger.LogError(() => "this is a error trace");
	- TraceLogger.LogCritical(() => "this is a critical trace");
	- TraceLogger.LogException(() => ex);
```

<br>
<br>
# TELEMETRY PROVIDERS

The image below shows an example of diginsight telemetry rendered to a log4net log provider for a wpf smart client application:
![alt text](/images/v2/01.%20log4net%20trace.jpg "Diginsight telemetry to log4net log provider")

Similarly, the image below shows the result of rendering telemetry to the console of a web api on an azure kubernetes services container:
![alt text](/images/v2/01.1%20aks%20console%20trace.jpg "Diginsight telemetry to a web api running on AKS container").

an analogous result can be obtained rendering the application flow on the browser console log of a Blazor WebAssembly application:
![alt text](/images/v2/01.2%20blazor%20console%20trace.jpg "Diginsight telemetry of a Blazor WebAssemply application to the browser console log").

The __relevant information of all these flows__ can be collected into centralized storage such as an __Application Insight__ resource or Azure Log Analytics workspace.

The following image shows the information level traces on an ApplicationInsights trace repository:  
![alt text](/images/v2/01.3%20Diginsight%20Application%20Flow%20to%20ApplicationInsight%20trace.jpg "Diginsight telemetry to Application Insight Trace").

where application exceptions traced with __TraceException()__ can be analized into the Exceptions view:
![alt text](/images/v2/01.4%20Diginsight%20Exceptions%20to%20ApplicationInsight.jpg "Diginsight Exceptions into Application Insight Exceptions view").


# ADDITIONAL INFORMATION

## Starting Telemetry
Starting telemetry is a matter of configuring the .Net log providers that are suitable for our application.

```c#
	.ConfigureLogging((context, loggingBuilder) =>
	{
		loggingBuilder.ClearProviders();

		var options = new Log4NetProviderOptions();
		options.Log4NetConfigFileName = "log4net.config";
		var log4NetProvider = new Log4NetProvider(options);
		loggingBuilder.AddDiginsightFormatted(log4NetProvider, configuration);

		var telemetryConfiguration = new TelemetryConfiguration(appInsightKey);
		var appinsightOptions = new ApplicationInsightsLoggerOptions();
		var tco = Options.Create<TelemetryConfiguration>(telemetryConfiguration);
		var aio = Options.Create<ApplicationInsightsLoggerOptions>(appinsightOptions);
		loggingBuilder.AddDiginsightFormatted(new ApplicationInsightsLoggerProvider(tco, aio), configuration);

		loggingBuilder.AddFilter<ApplicationInsightsLoggerProvider>("", LogLevel.Debug);
	}).Build();

	Host.InitTraceLogger();
```

notice that the provider is added with the statement 
```c#
		loggingBuilder.AddDiginsightFormatted(log4NetProvider, configuration);
```
this adds the `Log4NetProvider` as the inner provider of a diginsight `TraceLoggerFormatProvider`.<br>
`TraceLoggerFormatProvider` role is to receive trace entries, keep track of the nesting level for the current thread and eventually format a string for the inner provider.

The image below shows the `TraceLoggerFormatProvider` receiving trace entries from .net ILogger interfaces and formatting them for an inner provider:
![alt text](/images/v2/01.5%20Diginsight%20TraceLoggerFormatProvider%20with%20nested%20provider.jpg "TraceLoggerFormatProvider with an inner provider receiving traces from .net ILogger interfaces")

from now on it is just a metter of adding Method Scopes, named scopes and Trace statements to your code to get the real application flow.<br>

## Instrumenting a Method Scope or a Named Scope
Just write a `using` statement with extensions methods `BeginMethodScope()` and `BeginNamedScope()` to obtain a method or a named __scope variable__.<br>
You can write traces by means of the __scope variable__ or by means of __standard ILogger statements__ or __TraceLogger static methods__.

![alt text](/images/v2/01.6%20Diginsight%20logging%20statements.jpg "Instrimenting code with diginsight")

For every scope variable, diginsight can keep track of the nesting level for the current thread.
When __writing traces with the scope variable__ traces are written at the scope nesting level.

When __writing traces with the ILogger interface__ or the __static methods__ the trace will be written at the nesting level of the __most inner scope, for the curren thread__.

In this case, the log statement can be in a different method than the one where the scope variable is defined (at a higher nesting level).
To indicate this such traces are __prefixed with an ellipsis (...)__.

The following image shows the result of the preceding section, where prefix ellipses are visible for traces from _logger variable or TraceLogger static methods:
![alt text](/images/v2/01.7%20Diginsight%20logging%20output.jpg "Trace output")


## Tracing method parameters, variables and return values
When calling extensions methods `BeginMethodScope()` and `BeginNamedScope()` the method name is obtained by compiler generated information.<br>
You can __provide method parameters__ to the application flow by means of an __unnamed class__ in the __object payload parameter__.<br>
At the same way you can describe __variable values__ using the LogDebug overload with the __object payload parameter__.
also the __return value of a method scope__ can be tracked by means of the `scope.Result` value:

The following image shows a method scope where parameters and variabes are tracked with the __object payload parameter__ and the return value is tracked with the `scope.Result` value
![alt text](/images/v2/01.8%20write%20parameters,%20variables%20and%20method%20result.jpg "Trace output").

The following image shows trace output of such traces:
![alt text](/images/v2/01.9%20write%20parameters,%20variables%20and%20method%20result%20-%20output.jpg "Trace output").

## Configure Trace Providers
__Common.Diagnostics__ relies on standard tracing for .net so you can use the standard __.net Logging configuration section__.

The following image shows an example configuration section that specifies different trace levels for Log4Net and ApplicationInsight providers:

![alt text](/images/v2/01.10%20providers%20configuration.jpg "Trace output").

We mentioned that the application flow is obtained with a `TraceLoggerFormatProvider` with the real provider nested into it as an __inner provider__.<br>
Aliases are defined for `TraceLoggerFormatProvider` to allow provider specific configuration of the trace level.<br>
In the picture above we are using  `DiginsightFormattedLog4Net` to configure tracelevel when using Log4Net inner provider and `DiginsightFormattedApplicationInsights` to configure tracelevel when using ApplicationInsight inner provider.

In particular, in the shown example, `Debug` level is specified for __Log4Net__ and `Information` level is specified for __ApplicationInsight__.

__Additional configuration__ is available __at provider level__, to specify the exact information that should be rendered with the execution flow.
As an example it is possible to enable/disable rendering of the nesting level and the exact pieces of inforation that should be formatted into every trace line.

The following example specifies that the console provider used by a __blazor webassembly application__ should render the application flow without the trace source (the assembly name) to save space on the console window.<br>
![alt text](/images/v2/01.11%20providers%20configuration%20-%20additional.jpg "Trace output")<br>
On the other side, source information (and probably process and machine name, ip address etc) may be useful when sending telemetry to a central store such as __Application Insight__.   


The table below shows the configuration values that are availabe, __at provider level__:

| configuration value   | description           |
|-----------------------|:----------------------|
| TimestampFormat |(def."HH:mm:ss.fff") specifies the timestamp format for every trace entry|
| FlushOnWrite |(def. false) if true, a flush is performed at every write |
| ShowNestedFlow |(def. false) if true, spaced are used to show call nesting on the application flow |
| TraceMessageFormat |Format for standard trace messages <br>(def. '[{now}] {source} {category} {tidpid} - {logLevel} - {lastLineDeltaPadded} {deltaPadded} {nesting} {messageNesting}{message}') |
| TraceMessageFormatStart |Format scope start messages <br>(def. '[{now}] {source} {category} {tidpid} - {logLevel} - {lastLineDeltaPadded} {deltaPadded} {nesting} {messageNesting}{message}') |
| TraceMessageFormatStop |Format scope stop messages <br>(def. '[{now}] {source} {category} {tidpid} - {logLevel} - {lastLineDeltaPadded} {deltaPadded} {nesting} {messageNesting}{message}{result}') |
---------------

format strings can use the following placeholders:
| placeholder   | description           |
|-----------------------|:----------------------|
| Now	| The traceentry timestamp
| processName	| The processname
| source	| The trace entry source (eg. the source assembly)
| category	| The trace entry category (eg. the source class)
| tidpid	| Thread id and process id
| sourceLevel	| Source level
| logLevel	| Log level
| nesting	| Nesting level (as a number)
| messageNesting	| Message Nesting as spaces
| message	| The trace entry real message
| lastLineDelta	| Time delta since last trace line
| lastLineDeltaPadded	| Time delta since last trace line (with padding)
| delta	| Time delta scope start (method start)
| deltaPadded	| Time delta scope start (with patting)
| result	| Result (only for TraceMessageFormatStop)

# GetLogString(), ISupportLogString and IProvideLogString
When logging method parameters, variables and return values you can use the `GetLogString()` extension method.<br>
For primitive types, GetLogString() renders the full object value.<br>
For Arrays, Dictionaries and collections GetLogString() shows the number of items and the first items in the list (the list is truncated according to some configuration values)<br>
For other types, GetLogString() produces a string with the object short type name.<br>
<br>
You can provide log strings for your types by means of `ISupportLogString` interface as shown in figure below.

![alt text](/images/06._Class_with_ISupportLogString.jpg "Debug trace with nesting")
<!-- 
<img src="/diginsight/telemetry/blob/master/images/06._Class_with_ISupportLogString.jpg?raw=true" 
	alt="Debug trace with nesting"
	title="Debug trace with nesting" 
	style="border: 1px solid black;" />
-->

For objects from external libraries You can provide log strings registering a provider with `IProvideLogString` interace as shown below:

![alt text](/images/07._Application_instance_with_IProvideLogString.jpg "Debug trace with nesting")
<!-- 
<img src="/diginsight/telemetry/blob/master/images/07._Application_instance_with_IProvideLogString.jpg?raw=true" 
	alt="Debug trace with nesting"
	title="Debug trace with nesting" 
	style="border: 1px solid black;" />
-->

The image below shows the EasySample where logstrings are provided for Window and Button object instances.

![alt text](/images/08._Application_trace_with_custom_logstrings_from_IProvideLogString.jpg "Debug trace with nesting")
<!-- 
<img src="/diginsight/telemetry/blob/master/images/08._Application_trace_with_custom_logstrings_from_IProvideLogString.jpg?raw=true" 
	alt="Debug trace with nesting"
	title="Debug trace with nesting" 
	style="border: 1px solid black;" />
-->

# Performance considerations
Rendering the application flow there is a risc to impact application performance.
For this reason when using diginsight it is important to follow normal guidelines of general good sense:

- limit using Method Scopes (and logging statements in general) on strict loops 
- limit using Method Scopes (and logging statements in general) on deeply recursive methods 
- avoid sending Trace and Debug level telemetry to providers that write to the network or other io-bound resources (eg. Application Insight) 

Diginsight telemetry takes some important precautions to avoid cluttering resources and reduce use of CPU and memory:
- use compiler generated information and avoid use of reflection when gathering the application flow
- gather application flow as pointers into TraceEntry structures and avoid composing and formatting log strings that are not used
- use string interpolation handlers and delegates to elimitate impact of disabled log entries
- use string.format() statement to compose log strings and avoid string.replace() and concatenation composing and formatting log strings
- support a default limit for the length of the strings that are written to the log that can be changed by configuration and overloaded by the developer on any specific log statement<br>
this ensures that when trying (inadvertitely) to log variables with large data all the data is rendered to the log. 
  
other strategies can be implemented in the future to further control perfomrance impact and still allow complete visibility of application flow for debugging, troubleshooting and reverse engineering purposes.


# Previous versions and support for System Diagnostics Trace Listeners
Current version of Diginsight telemetry provide support for Both __.Net Log providers__ and DotNet __Systen Diagnostics listeners__.<br>
The current document focused on using telemetry with __.Net Log providers__.<br><br>

[README.v1.md](README.v1.md) describes how to use Diginsight telemetry with standard DotNet __Systen Diagnostics listeners__.
<br><br>

# Build and Test 
Clone the repository, open and build solution Common.Diagnostics.sln. 
run EasySample and open the log file in your **\Log** folder.

# Contribute
Contribute to the repository with your pull requests. 

- [ASP.NET Core](https://github.com/aspnet/Home)
- [Visual Studio Code](https://github.com/Microsoft/vscode)

# License
See the [LICENSE](LICENSE.md) file for license rights and limitations (MIT).
