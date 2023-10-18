using Common;
using EasySample;
using System;
using System.Diagnostics.Metrics;

// THIS IS A CHOICE, NOT AN ERROR: https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/docs/trace/extending-the-sdk#tracerproviderbuilder-extension-methods
namespace OpenTelemetry.Metrics;

public static class MeterExtensions
{
    public static Type T = typeof(MeterExtensions);
    public static TimerHistogram CreateTimer(this Meter meter, string name, string? unit = "ms", string? description = null)
    {
        using var scope = TraceLogger.BeginMethodScope(T, new { meter, name, unit, description });

        return new TimerHistogram(meter, name, unit, description);
    }
}
