# INTRODUCTION 

__Common.Diagnostics__  makes the application flow fully observable, still without compromises on performance.<br>
<br>
This makes troubleshooting easier!<br>
Also, reverse enrineering is made easier, when changing or migrating applications and documentation is not available about the application behaviour. <br>
<br>
In this article we'll explore how we can make our application flow fully observable.<br>
<br>
With article "HOWTO - Avoid performance impacts using diginsight telemetry" we'll go deep into diginsight strategies and best practices to avoid impacts on the application performance.<br>
<br>

# LOG METHODS EXECUTION

`using` statements can be applied to define __Method or Named scopes__.<br>
Exposing a method execution to diginsight is as simple as shown below:
```c#
private async Task ctlMain_InitializedAsync(object sender, EventArgs e)
{
    <span style="background-color: yellow;">using var scope = logger.BeginMethodScope(new { sender, e });</span>

    try
    {
        var tenantId = default(string);
        var clientId = default(string);
```

the __BeginMethodScope()__ extension method ensures that method name, class name and other useful information about the execution flow are gathered and used upon need.<br>
<br>
the above statement produces the following application log flow:<br>
![alt text](/images/v3/01.%20BeginMethodScope.jpg "Diginsight telemetry Method scope")

similar code can be used to expose a named scope within an existing method:
```c#
private void SaveExecuted(object sender, ExecutedRoutedEventArgs e)
{
    using var scope = logger.BeginMethodScope();

    var inputBox = new InputBoxControl()
    {
        Title = "Save configuration...",
        Label = "Choose a configuration name:",
        Text = "Samplename"
    };

    inputBox.OnOK += (sender, e) =>
    {
        using var scope = logger.BeginNamedScope("OnOK");

        ...
    };
```

the __BeginNamedScope()__ provides name 'OnOK' to the code section enclosed by the using statement.<br>
<br>
the above statement produces the following application log flow:<br>
![alt text](/images/v3/02.%20BeginNamedScope.jpg "Diginsight telemetry Named scope")<br>

in this image it OnOK code section is shown as a part of method 'SaveExecuted'.<br>

<br>

# USE STATIC METHODS WHERE ILogger logger IS NOT AVAILABLE
when ILogger logger member is not available you can use static methods instead of ILogger extension methods.

the above MethodScope can be defined with the following syntax:
```c#
private async Task ctlMain_InitializedAsync(object sender, EventArgs e)
{
    using var scope = TraceLogger.BeginMethodScope(T, new { sender, e });

    try
    {
        var tenantId = default(string);
        var clientId = default(string);
```

the above NamedScope can be defined with the following syntax:
```c#
private void SaveExecuted(object sender, ExecutedRoutedEventArgs e)
{
    using var scope = TraceLogger.BeginMethodScope(T);

    var inputBox = new InputBoxControl()
    {
        Title = "Save configuration...",
        Label = "Choose a configuration name:",
        Text = "Samplename"
    };

    inputBox.OnOK += (sender, e) =>
    {
        using var scope = TraceLogger.BeginNamedScope(T, "OnOK");

        ...
    };
```

# LOG PARAMETER VALUES
Parameter values can be logged together with their names with the following syntax:

```c#
public async Task<UserProfileResponse> FindUserByEmailAddressAsync(string emailAddress, CacheContext cacheContext = null)
{
    using var scope = logger.BeginMethodScope(new { emailAddress, cacheContext = cacheContext?.GetLogString() });

    try
    {
        return await userProfileClient.FindUserByEmailAddressAsync(emailAddress, cacheContext);
    }
    catch (ApplicationException ape)
    {
        logger.LogException(ape);
        throw;
    }
}
```
in this case method FindUserByEmailAddressAsync is logged together with its parameter names and parameter values.
Parameter names and values are shown inline with the method START line, as shown below:
![alt text](/images/v3/03.%20ParametersLog.jpg "Diginsight telemetry parameters log")<br>

Simple types such as strings, numeric etc are shown properly; for complex types __.GetLogString()__ extension method can be used.

In the provided example, __GetLogString()__ produces the following description for the `cacheContext` parameter:
```c#
cacheContext:{CacheContext:{Enabled:True,MaxAge:600}}
```

such descriptions can be provided by the object itself, by means of `ISupportLogString` interface or by the outer application, by means of `IProvideLogString` interface.

```c#
public class CacheContext : ICacheContext, ISupportLogString
{
    public bool Enabled { get; set; }
    public int? MaxAge { get; set; }
    public int? AbsoluteExpiration { get; set; }
    public int? SlidingExpiration { get; set; }
    public Type InterfaceType { get; set ; }

    public string ToLogString()
    {
        var logString = $"{{{nameof(CacheContext)}:{{Enabled:{this.Enabled},MaxAge:{this.MaxAge},AbsoluteExpiration:{this.AbsoluteExpiration},SlidingExpiration:{this.SlidingExpiration}}}";
        return logString;
    }
}
```
<br><br>
# LOG MESSAGES AND VARIABLES

within a method or a named scope, messages and variables can be logged with their severity

You can use the __scope variable__ to add trace messages to the method scope or the named scope
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
You can use __standard ILogger statements__ or __TraceLogger static methods__ to add trace messages to the application flow when a scope variable instance is not available.

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
in this case log traces are added to the inner most scope, for the current thread.<br><br>
In the latter options, you cannot be sure that the trace is performed in the same method where the scope variable is defined; for this reason, output messages will be prefixed with a leading ellipsis as shown below:<br>
![alt text](/images/v3/04.%20LeadingEllipses.jpg "Diginsight telemetry leading ellipses")<br>
<br>

# LOG THE STARTUP SEQUENCE AND ANY RELEVANT APPLICATION FLOW DETAIL

## Log the startup Sequence
The startup sequence often hides complex logic that is very difficult to troubleshoot.<br>
<br>
diginsight reproduce the application flow since the `Program.Main` application start.<br> 
Where dependency injection ILogger variables are not available, you can define Method sections and named sections by means of the static overloads as shown above.

the following image shows the application flow of an aspnet core application startup sequence, including the `CreateHostBuilder` method and its callbacks `ConfigureAppConfiguration` and `ConfigureServices`

![alt text](/images/v3/05.%20StartupSequenceLog.jpg "Diginsight telemetry startup sequence log")<br>

Many complex details such as configuration errors and connection failures can be hidden here and very difficult to troubleshoot.
Diginsight shows any detail here so that any later application failure can be more easily understood. 

# USE THE DIGINSIGHT LOG TO OBSERVE APPLICATION BEHAVIOURS
Many details can be understood from diginsight application flow.<br><br>
A few example below:
1. it is easy to understand if the application is executing `redundant calls` or within the overall application flow
2. it is easy to understand if a particular internal call is producing a `relevant latency`.
3. it is easy to understand if a call to an external method is involving a `big payload size`

The image below shows a call to method `FindUserByEmailAddressAsync` with a latency of __50 seconds__: this makes evident that the invoked service has problem to be understood. 

![alt text](/images/v3/06.%20HighLatencyCall.jpg "Diginsight telemetry startup sequence log")<br>

The example below shows a call is shown where the returned payload is higher than 1MB:
![alt text](/images/v3/07.%20HighPayloadCall.jpg "Diginsight telemetry startup sequence log")<br>
if the method is invoked frequently, this may cause a scalability problem.


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
