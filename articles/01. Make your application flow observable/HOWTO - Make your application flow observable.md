# INTRODUCTION 

__Common.Diagnostics__  makes the application flow fully observable, still __without compromises on performance__.<br>
<br>
This makes troubleshooting easier!<br>
Also, reverse enrineering is made easier, when changing or migrating applications and documentation is not available about the application behaviour. <br>
<br>
In this article we'll explore __how we can make our application flow fully observable__.<br>
<br>
With article:<br>
[HOWTO - Avoid performance impacts using diginsight telemetry.md](HOWTO%20-%20Avoid%20performance%20imacts%20using%20diginsight%20telemetry.md)<br>
we'll go deep into diginsight strategies and best practices to avoid impacts on the application performance.<br>
<br>

# LOG METHODS EXECUTION

Simple `using` statements can be used to define __Method or Named scopes__.<br>
Exposing a method execution to diginsight is as simple as shown below:
```c#
private async Task ctlMain_InitializedAsync(object sender, EventArgs e)
{
    using var scope = logger.BeginMethodScope(new { sender, e }); 
    // scope variable writes method START and END and keeps track of nesting level

    try
    {
        var tenantId = default(string);
        var clientId = default(string);
```

__BeginMethodScope()__ extension method ensures that method name, class name and other useful information about the execution flow are gathered and used upon need.<br>
<br>
The above statement produces the following application log flow where `ctlMain_InitializedAsync` is made evident:<br>
![Alt text](<01. BeginMethodScope.jpg> "Diginsight telemetry Method scope")

Similar code can be used to expose a __named scope__ within an existing method:
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
        // named scope 'OnOK' is defined here

        ...
    };
```

__BeginNamedScope()__ applies name 'OnOK' to the code section enclosed by the using statement.<br>
<br>
The above statement produces the following application log flow:<br>
![Alt text](<02. BeginNamedScope.jpg> "Diginsight telemetry Named scope")<br>


In this flow it __OnOK__ code section is shown as a part (or as en extension) of method __SaveExecuted__.<br>

<br>

# USE STATIC METHODS WHERE ILogger logger IS NOT AVAILABLE
When `ILogger logger` member is not available we can use static methods instead of ILogger extension methods.

The above `method scope` example can be defined with the following syntax:
```c#
private async Task ctlMain_InitializedAsync(object sender, EventArgs e)
{
    using var scope = TraceLogger.BeginMethodScope(T, new { sender, e });
    // scope variable writes method START and END and keeps track of nesting level

    try
    {
        var tenantId = default(string);
        var clientId = default(string);
```

The above `named scope` example can be defined with the following syntax:
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
        // named scope 'OnOK' is defined here

        ...
    };
```

# LOG PARAMETER VALUES
Parameter values can be logged together with their names with the following syntax:

```c#
public async Task<UserProfileResponse> FindUserByEmailAddressAsync(string emailAddress, CacheContext cacheContext = null)
{
    using var scope = logger.BeginMethodScope(new { emailAddress, cacheContext = cacheContext?.GetLogString() });
    // emailAddress and cacheContext parameters are logged with the above Method scope

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
in this case method `FindUserByEmailAddressAsync` is logged together with its parameter names and parameter values.<br>
Parameter names and values are shown inline within the method START line, as shown below:
![Alt text](<03. ParametersLog.jpg> "Diginsight telemetry parameters log")<br>

For Simple types such as strings, numeric etc values are shown properly.<br>
For complex types __`GetLogString()`__ extension method can be used.

In the provided example, __`GetLogString()`__ produces the following description for the `cacheContext` parameter:
```c#
cacheContext:{CacheContext:{Enabled:True,MaxAge:600}}
```

Such descriptions can be provided by the object itself, by means of `ISupportLogString` interface or by the outer application, by means of `IProvideLogString` interface.

The following code snippet shows a class providing its log string by means of `ISupportLogString` interface:
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
The following code snippet shows an application providing log strings for external classes by means of `IProvideLogString` interface
```c#
public partial class App : Application, IProvideLogString
    {
        static Type T = typeof(App);

        public App()
        {
            using (var sec = TraceManager.GetCodeSection(T))
            {
                LogStringExtensions.RegisterLogstringProvider(this);
                // RegisterLogstringProvider register a provider implementing IProvideLogString interface
            }
        }

        public string ToLogString(object t, HandledEventArgs arg)
        {
            // ToLogString implementation provides LogString implementation for incomin object instances 
            switch (t)
            {
                case Window w: arg.Handled = true; return ToLogStringInternal(w);
                default:
                    break;
            }
            return null;
        }
        public static string ToLogStringInternal(Window pthis)
        {
            // in this case, a LogString is provided for System Window objects
            string logString = $"{{Window:{{Name:{pthis.Name},ActualHeight:{pthis.ActualHeight},ActualWidth:{pthis.ActualWidth},AllowsTransparency:{pthis.AllowsTransparency},Background:{pthis.Background},Width:{pthis.Width},Height:{pthis.Height}}}}}";
            return logString;
        }
```
<br><br>
# LOG MESSAGES AND VARIABLES

Messages and variables can be logged with their severity within any method or a named scope.

We can use the __scope variable__ to add trace messages to the method scope or the named scope
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
We can use __standard ILogger statements__ or __TraceLogger static methods__ to add trace messages to the application flow when a scope variable instance is not available.

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
In this case log traces are added to the inner most scope, for the current thread.<br><br>
In the latter options, we cannot be sure that the trace is performed in the same method where the scope variable is defined; for this reason, output messages are prefixed with a __leading ellipsis__ as shown below:<br>
![Alt text](<04. LeadingEllipses.jpg> "Diginsight telemetry leading ellipses")<br>

Variables with their names can be logged with the following syntax:
```c#
	// log statements within a scope
	- scope.LogDebug(new { this.Identity, tenantId, clientId, clientSecret, keyVaultAddress });

	// standard Ilogger statements:
	- _logger.LogDebug(new { this.Identity, tenantId, clientId, clientSecret, keyVaultAddress });

	// log statements with TraceLogger static methods:
	- TraceLogger.LogDebug(new { this.Identity, tenantId, clientId, clientSecret, keyVaultAddress });
```
In this cases the veriables are rendered with their names, in the same way this happens when logging method parameters.

![Alt text](<08. LogObjectPayload-1.png> "Diginsight telemetry leading ellipses")<br>

Please note that variable names and values are taken directly by the LogDebug() payload object and the developer doesn't need to compose a string with them and keep it up to date.

<br>

# LOG THE STARTUP SEQUENCE AND ANY RELEVANT APPLICATION FLOW DETAIL

## Log the startup Sequence
The startup sequence of aspnet applications often hides complex logic that is very difficult to troubleshoot.<br>
<br>
Diginsight reproduces the application flow since the `Program.Main` application start.<br> 
Where dependency injection `ILogger` variables are not available, you can define `method scopes` and `named scopes` by means of the static `TraceLogger` overloads (see examples above).

The following image shows the application flow of an aspnet core application startup sequence, including the `CreateHostBuilder` method and its callbacks `ConfigureAppConfiguration` and `ConfigureServices`

![Alt text](<05. StartupSequenceLog.jpg> "Diginsight telemetry startup sequence log")<br>

Many complex details such as configuration errors and connection failures are often hidden here and troubleshooting for these phases can be very complex.<br>
Diginsight shows any detail here so that any later application failure can be more easily and quickly understood. 

# USE THE DIGINSIGHT LOG TO OBSERVE APPLICATION BEHAVIOURS
Many details can be observed from diginsight application flow.<br><br>
A few example below:
1. it is easy to understand if the application is executing `redundant calls` or within the overall application flow
2. it is easy to understand if a particular internal call is producing a `relevant latency`.
3. it is easy to understand if a call to an external method is involving a `big payload size`

The image below shows a call to method `FindUserByEmailAddressAsync` with a latency of __50 seconds__: this makes evident that the invoked service has problem to be understood. 

![Alt text](<06. HighLatencyCall.jpg> "Diginsight telemetry startup sequence log")<br>

The example below shows a call is shown where the returned payload is higher than 1MB:
![Alt text](<07. HighPayloadCall.jpg> "Diginsight telemetry startup sequence log")<br>
if the method is invoked frequently, this may cause a scalability problem.

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
