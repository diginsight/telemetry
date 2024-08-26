# INTRODUCTION 

__Diginsight v3__ introcuces the following key changes to diginsight telemetry><br>
- stronger integration with __standard .Net Diagnostics__ and __ILogger API__.
- stronger integration with __Opentelemetry (Traces, Metrics, logs)__ and __Azure Monitor__.
- __performance improvements__.
- Automatic (customizable) __rendering of method payloads and return values__.
- support to __dynamic configuration of log levels__.
- TBD: Automatic rendering of outbound calls (http, cosmosdb, ....).



# DIGINSIGHT SAMPLES GITHUB PROJECT

Diginsight `telemetry_samples` projects are available on the [Diginsight GitHub](repo) repository.

<!-- ![alt text](<01. telemetry_samples github repository.png> =500x) -->
<img src="01. telemetry_samples github repository.png" alt="alt text" width="600"/>


when cloning the repositories you will find the following __folder structure__:<br>
<img src="02. repositories folders structure.png" alt="alt text" width="600"/>

Opening the `Common.Diagnostics.Samples` solution you should be able to build and run the samples for Wpf, Aspnet and Blazor technologies<br>
Opening the `Common.Diagnostics.Samples.Debug` solution the samples will be built with __project reference__ to diginsight to allow you easily __step into and debug diginsight code__.<br>

NB. Common.Diagnostics.Samples.Debug solution can only be used when both `telemetry` and `telemetry_samples` repositories are cloned with their __default name__, under the __same parent folder__.

| Common.Diagnostics.Samples.sln solution  | Common.Diagnostics.Samples.Debug.sln solution |
|--|--|
| <img src="03. Samples Solution.png" alt="alt text" width="400"/> |  <img src="03a. Samples Debug Solution.png" alt="alt text" width="460"/>|



# Contribute
Contribute to the repository with your pull requests. 

- [ASP.NET Core](https://github.com/aspnet/Home)
- [Visual Studio Code](https://github.com/Microsoft/vscode)

# License
See the [LICENSE](LICENSE.md) file for license rights and limitations (MIT).
