# Introduction 
__Diginsight telemetry__ is a set .Net packages that that provides __automatic__ __observability__ for dotnet applications.<br> 
In particular, __the full application flow__ is made available to __local text based streams__ such as __traditional file logs__, the __Console Log__ or the __Azure Streaming Log__ and also to remote analysis tools such as __Azure Monitor__ and __Prometheus__/__Grafana__.

Diginsight allows __observability__ of the __full application lifecycle__, including __static methods__, __injection sequences__ and the __application startup__ and __shutdown sequences__ where configuration problems and much complexity are often hidden.

__Diginsight telemetry__ is produced by standard __ILogger<>__ and __System.Diagnostic activity__ classes so it integrates (without replacing) other logging systems telemetry. Also, __diginsight telemetry__ fully integrated with __Opentelemetry__ and the __W3C Trace Context__ Specification so __traceids__ are preserved across process invocations of a distributed system.

__Diginsight telemetry__ targets __all dotnet framework versions__ starting from __netstandard2.0__.<br>Samples are available on [telemetry.samples](https://github.com/diginsight/telemetry.samples) repository to demonstrate use of telemetry on __.net 4.8__ up to [__blazor webassembly__,]__.net6__ and __.net8+__ assemblies.
<br><br>

# License
See the [LICENSE](<LICENSE>) file for license rights and limitations (MIT).
