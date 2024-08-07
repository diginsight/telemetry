# GETTING STARTED (with Diginsight v3)

## INTRODUCTION
With __version 3__ diginsight streamlines __OpenTelemetry integration__ embracing standard notation for activity tracing with __dotnet System Diagnostic API__.

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

Article [HOWTO - Use Diginsight Samples](<https://github.com/diginsight/telemetry/blob/main/docs/articles/04. HowTo Use Diginsight Samples/HOWTO - Use Diginsight Samples.md>): explores how we can use diginsight samples to test and understand integration of Diginsight telemetry in our own projects.


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
![alt text](<003.01a Code span with diginsight.png>)

Please, note that in this case the method payload is passed to `StartMethodActivity` by means of a __delegate notation__ so that the payload class allocation can be avoided when __logging__ or __payload rendering__ is disabled.

## STEP 04 - Enable OpenTelemetry and send data to the remote tools
With few changes to the startup sequence, __telemetry can be sent to the remote tools__.
Telemetry to the local tools is less expensive, m4ore efficient, well protected and often it is not even persisted.
So, telemetry to the local tools can include verbose data with the maximum level of information.<br>

Telemetry to the remote tools is more expensive (in cost and performance) so it will normally include only critical and warning non verbose information.

In our samples we enable openteemetry by means of the __AddObservability()__ extension method that essentially: 
- Configures __Opentelemetry options__
- Registers __Opentelemetry logging provider__
- Configures __tracing to the remote tools__
- Configures __metrics  to the remote tools__

![alt text](<004.00 AddObservability Extension method.png>)

details about opentelemetry configuration is available here:
[HowTo: Configure diginsight telemetry to the remote tools](<articles/13.a Configure diginsight telemetry to the remote tools/Configure diginsight telemetry to the remote tools.md>).<br>

# Build and Test 
Clone the repository, open and build solution Common.Diagnostics.sln. 
run EasySample and open the log file in your **\Log** folder.

# Contribute
Contribute to the repository with your pull requests. 

- [ASP.NET Core](https://github.com/aspnet/Home)
- [Visual Studio Code](https://github.com/Microsoft/vscode)

# License
See the [LICENSE](LICENSE.md) file for license rights and limitations (MIT).
