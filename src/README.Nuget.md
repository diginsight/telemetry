# INTRODUCTION 
__Diginsight telemetry__ provides __automatic__ __observability__ for dotnet applications.<br> 
In particular, __the full application flow__ is made available to __local text based streams__ such as __traditional file logs__, the __Console Log__ or the __Azure Streaming Log__ and also to remote analysis tools such as __Azure Monitor__ and __Prometheus__/__Grafana__.

__Diginsight telemetry__ is produced by standard __ILogger<>__ and __System.Diagnostic activity__ classes so it integrates (without replacing) other logging systems telemetry. Also, __diginsight telemetry__ fully integrated with __Opentelemetry__ and the __W3C Trace Context__ Specification so __traceids__ are preserved across process invocations of a distributed system.

__Diginsight telemetry__ targets __all dotnet framework versions__ starting from __netstandard2.0__.<br>Samples are available on [telemetry_samples](https://github.com/diginsight/telemetry_samples) repository to demonstrate use of telemetry on __.net 4.8__ up to [__blazor webassembly__,]__.net6__ and __.net8+__ assemblies.
<br><br>
  
Articles:
- [Diginsight telemetry documentation](https://diginsight.github.io/telemetry/): explains diginsight telemetry concepts and how it extends __ILogger<>__ and __System.Diagnostics__ API.
- [HOWTO - Send telemetry to the local text based streams](https://diginsight.github.io/telemetry/src/docs/01.%20Concepts/01.00%20-%20Configure%20diginsight%20telemetry%20to%20the%20local%20text%20based%20streams.html): explores how to send diginsight telemetry to the local text based streams (eg. a local file or a AKS console log).
- [HOWTO - Send telemetry to the remote analysis tools](https://diginsight.github.io/telemetry/src/docs/02.%20Advanced/09.00%20-%20Configure%20diginsight%20telemetry%20to%20the%20remote%20tools.html): explores how to send diginsight telemetry to the remote analysis tools (eg. Azure monitor or Prometheus and Grafana).
- [HOWTO - Use diginsight telemetry with no impact on Application performance an telemetry cost](https://diginsight.github.io/telemetry/src/docs/02.%20Advanced/10.00%20-%20Maximize%20application%20performance%20and%20minimize%20telemetry%20cost%20with%20diginsight.html): explores how we can do this ensuring no impact on application performance.

