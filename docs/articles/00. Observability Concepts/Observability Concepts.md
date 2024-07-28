
# APPLICATION OBSERVABILITY CONCEPTS 
__Application observability__ is about aggregating, correlating and analyzing the following key elements:<br>
-  __Logs__ with application execution details and data.
-  __The requests and operations structure__ (sometimes also referred as __Activity, Traces or Spans__) with the structure of application calls, related to an event or exception condition.
-  __Metrics__: numeric values (such as latencies, payload sizes, frequencies) that can be aggregated and correlated with the operations structure and the logs.

The image below shows examples about the __3 observability elements__ on Azure Monitor Performance Management (APM) Tools:<br><br>
![alt text](<002.00 Opentelemetry elements.png>)<!-- /images/other/ -->

Diginsight __makes observability easy__ as:
- it __integrates the 3 observability elements__ (Log, Traces, Metrics) into high performance __text-based streams__ such as traditional `File logs`, the `Console log` or the `Azure Streaming log`.<br>
- it __publishes the 3 observability elements__ to `OpenTelemetry` and allowing application analysis by means of remote APM tools such as __Azure Monitor__ and __Grafana__.<br>
<br>
# ADDITIONAL INFORMATION 

Application flow observability is provided leveraging existing __.Net__ __ILogger__ and __System Diagnostics__ classes so that diginsight telemetry can be mixed and analyzed with other components telemetry, as long as they rely on the same standard framework classes.<br>
Observability for remote tools is provided by means of __OpenTelemetry__ so that telemetry data can be targeted to __Azure Monitor__ and also other analysis tools such as __Prometheus__/__Graphana__.

The following image shows diginsight metrics such as __span durations__  and __frequencies__ on a custom __Grafana__ dashboard receiving data by means of __Opentelemetry Prometheus__ stack.
![alt text](<001.00 Prometheus Grafana dashboard.png>)
<br>
<br>
Diginsight application flow rendering is:
- __consistent across tools__: every information or metric visible on the __local text based streams__ can be published and observed on the __remote analysis tools__ (eg. on Appinsight Transaction detail or Appinsight Metrics).
- __consistent with code__: the application flow is published with information about classes, method names and call nesting so the __'gap' from telemetry and code__ is shortened for __application developers__ and __site reliability engineers__.
![alt text](<001.01 Consistency across tools and code.png>)

- __consistent across applications__ application flow published in the same way for all applications. so it is __easily readable for peopble without background knowledge__ on the application logic.
![alt text](<001.02 Consistency across applications.png>) 
<br><br>

Diginsight __log layout__ and __automatic rendering__ for entities can be fully customized to ensure best readability of the application flow.

Paragraph [GETTING STARTED](<../../articles/00. GETTING STARTED/GETTING STARTED.md>) discusses basic steps we can follow to integrate diginsight telemetry.
