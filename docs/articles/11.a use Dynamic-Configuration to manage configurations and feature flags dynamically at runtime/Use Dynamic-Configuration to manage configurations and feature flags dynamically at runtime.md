# Use Dynamic-Configuration to manage configurations and feature flags dynamically at runtime

## INTRODUCTION

__Diginsight telemetry__ supports __dynamic configuration__ to hot switch configuration values, based on http headers.<br>
As an example, lets assume that a Web API or Web Application implements a __feature flag__ such as the following:

```json
"FeatureFlags": {
  "TraceRequestBody": false,
  "TraceResponseBody": false
}
```
The developer can __register such a configuration option__ so that __it can be overridden at runtime, for a single call, by means of  the `Dynamic-Configuration` http request header__.

## ADDITIONAL INFORMATION

The snippet below, shows the `FeatureFlags` configuration option class of the `SampleWebApi` project in Diginsight `telemetry_samples repository`:
```c#
public class FeatureFlagOptions : IDynamicallyPostConfigurable
{
    public bool TraceRequestBody { get; set; }
    public bool TraceResponseBody { get; set; }

}
```
If the developer registers it with the `.PostConfigureClassAwareFromHttpRequestHeaders<FeatureFlagOptions>()` method
as shown below:
![alt text](<001.01a FeatureFlagOptions registration for HTTP headers.png>)


than the options will be overridable by means of the __`Dynamic-Configuration` http request header__.

In such case, when calling the API with __`Dynamic-Configuration` http request header__ as shown below.

![alt text](<000.02e Postman call overriding Feature flags.png>)

The `TraceRequestBody` and `TraceResponseBody` configuration options will be overriden from the http request headers.

The image below shows the application log where in the first call the options are loaded from the `appSettings.json` configuration file.
in the second call, the same options are overridden by the incoming `Dinamic-Configuration` headers.

![alt text](<000.03a Log showing dynamic configuation override.png>)