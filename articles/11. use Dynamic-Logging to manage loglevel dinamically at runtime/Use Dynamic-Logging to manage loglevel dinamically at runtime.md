# HowTo: Use Dynamic Logging to manage loglevel dinamically, at runtime

## INTRODUCTION 
__diginsight telemetry__ supports __dynamic logging__ to hot switch the minimum log level (e.g. from Information or Warning to Debug or Trace levels) of any log category.<br>

To minimize telemetry cost and performance impact, telemetry sent to the remote tools is normally limited to LogLevels __Critical__, __Warning__ or __Information__.<br>

The image below shows a tipical Logging section configuration for a production environment:
![alt text](<000.01 example configuration for production environment.png>)

__Using Diginsight telemetry__ on a Web API or Web Application, the __log level__ for any category __can be overridden at runtime, for a single call, by means of suitable http request headers__.

## ADDITIONAL INFORMATION 

The image below shows the logstream for a web API where only __Critical__, __Warning__ and __Information__ and levels are enabled.

While the application is running the streaming log shows only limited information about the application execution flow.

![alt text](<001.01 default application streaming log.png>)

In case we need to troubleshoot a specific application call flow, it is possible to reporduce the call specifying different levels for some categories

![alt text](<001.02 Postman call with overloaded log levels.png>)

This will result in the full application flow being shown for the specific call:
![alt text](<001.03 Call application flow obtained with Dynamic Logging.png>)

> this way every call application flow can be easily __isolated__ and __analized__ on a live server, that is processing other calls at the same time.

