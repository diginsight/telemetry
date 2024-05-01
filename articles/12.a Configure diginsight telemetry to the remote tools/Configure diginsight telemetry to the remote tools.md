# HowTo: Configure diginsight telemetry to the remote tools

## INTRODUCTION 
__diginsight__ is a very thin layer built on __.Net System.Diagnostics__ Activity API and __ILogger__ API.<br>
In particular, sending __.Net System.Diagnostics__ and __ILogger__ telemetry to remote tools by means of __OpenTelemetry__ and/or __Prometheus__ results in sending the full __diginsight application flow__ to them.

This article discusses how Diginsight telemetry can be sento to remote analysis tools such as __Azure Monitor__ or __Grafana__ by means of OpenTelemetry.<br>
Also, the article shows how such telemetry can be easily analyzed on __Azure Monitor__ tools such as the __Transaction Search__ and __Transaction Detail__, the Azure Monitor __Metrics__ and __Logs__ or __Azure Monitor Dashboards__.<br>

The code snippets below are available as working samples within the [telemetry_samples](https://github.com/diginsight/telemetry_samples) repository.

Article [HOWTO - Use Diginsight Samples.md](<../04. HowTo Use Diginsight Samples/HOWTO - Use Diginsight Samples.md>): explores how we can use diginsight samples to test and understand integration of Diginsight telemetry in our own projects.
