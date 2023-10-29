# INTRODUCTION 
Common.Diagnostics is a .Net package that provides readable log with application execution flow to .Net Log providers such as Log4Net, Serilog or Application Insights, Console, EventLog and Debug .Net Log providers.  
This makes the application flow fully observable, still without compromises on performance.  
  
Articles:
- [Diginsight telemetry readme](https://github.com/diginsight/telemetry/tree/main#readme): explains how diginsight telemetry extends ILogger<> api.
- [HOWTO - Make your application flow observable](https://github.com/diginsight/telemetry/blob/main/HOWTO%20-%20Make%20your%20application%20flow%20observable.md): explores how to use diginsight to fully expose our application exeution flow.
- [HOWTO - Avoid performance impacts using diginsight telemetry](https://github.com/diginsight/telemetry/blob/main/HOWTO%20-%20Avoid%20performance%20imacts%20using%20diginsight%20telemetry.md): explores how we can do this ensuring no impact on application performance.

add telemetry to your methods with the following instruction 

```c#
	- using var scope = _logger.BeginMethodScope(); // defines a method scope by means of an ILogger instance (class type is taken by the ILogger instance)
	- using var scope = _logger.BeginNamedScope("scopeName"); // defines a named scope within a method scope (eg. to describe loop code sections or async method callbacks).

	- using var innerScope = _logger.BeginMethodScope(new { configuration = configuration .GetLogString()}); // defines a method scope where method parameters are specified 
```

write information to the listeners with the following instructions

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
or standard ILogger<> methods
```c#
	// standard Ilogger statements:
	- _logger.LogTrace("this is a Trace trace");
	- _logger.LogDebug("this is a Debug trace");
	- _logger.LogInformation("this is a Information trace");
	- _logger.LogWarning("this is a Warning trace");
	- _logger.LogError("this is a error trace");
	- _logger.LogCritical("this is a critical trace");
	- _logger.LogException(ex);
```


Common.Diagnostics component is supported on .Net Framework 4.6.2+ and .Net Core 3.0+.
Visit [telemetry][] for more information.
[telemetry]: https://github.com/diginsight/telemetry/
