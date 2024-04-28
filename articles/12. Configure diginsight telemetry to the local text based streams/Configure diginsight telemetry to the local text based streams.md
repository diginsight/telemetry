# HowTo: Configure diginsight telemetry to the local text based streams

## INTRODUCTION 
You can ottain a __console log__ or __file log__ with diginsight by means of the steps shown below.<br>
The code snippets are available as working samples within the [telemetry_samples](https://github.com/diginsight/telemetry_samples) repository.

Article [HOWTO - Use Diginsight Samples.md](<../04. HowTo Use Diginsight Samples/HOWTO - Use Diginsight Samples.md>): explores how we can use diginsight samples to test and understand integration of Diginsight telemetry in our own projects.


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
![alt text](<004.05a Add instrumentation yo your code.png>)

## STEP 04 - run your code and look at the resulting application flow 
the image below shows result of execution of DoSomeWork method 
![alt text](<004.06 View application flow.png>)