# Use Dynamic-Configuration to manage configurations and feature flags dynamically at runtime

## INTRODUCTION

__Diginsight telemetry__ supports __dynamic configuration__ to hot switch configuration values, based on http headers.<br>
As an example, lets assume that a Web API or Web Application implements a __feature flag__ such as the following:

![alt text](<000.01a Example feature flags configuration options.png>)

The developer can __register such a configuration option__ so that __it can be overridden at runtime, for a single call, by means of  the `Dynamic-Configuration` http request header__.

## ADDITIONAL INFORMATION

The image below, shows the `FeatureFlags` configuration option class of the `SampleWebApi` project in Diginsight `telemetry_samples repository`:
![alt text](<001.01 FeatureFlagsOptions class example.png>)

If the developer registers it with the `.PostConfigureClassAwareFromHttpRequestHeaders<FeatureFlagOptions>()` method
as shown below:
![alt text](<001.01 FeatureFlagOptions registration for HTTP headers.png>)

than the option will be overridable by means of the __`Dynamic-Configuration` http request header__.

In such case, when calling the API with __`Dynamic-Configuration` http request header__ as shown below.

![alt text](<000.02c Postman call overriding Feature flags.png>)

The `PermissionCheckEnabled` and `TraceResponseBody` configuration options will be overriden from the http request headers.

The image below shows the application log where in the first call the configured configuration options are shown.
in the second call, the same options are overridden by the incoming headers.

![alt text](<000.03 Log showing dynamic configuation override.png>)