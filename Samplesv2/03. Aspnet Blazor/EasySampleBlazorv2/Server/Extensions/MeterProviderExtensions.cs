using EasySampleBlazorv2.Server;
using System.Diagnostics.Metrics;
using System.Reflection;
//using ABB.Ability.ELSP.Common.OpenTelemetry.Metrics.Custom;

namespace OpenTelemetry.Metrics; 

public static class MeterProviderExtensions
{
    public static MeterProviderBuilder AddViews(this MeterProviderBuilder builder, params (string, MetricStreamConfiguration)[] views)
    {
        foreach (var (instrumentName, config) in views)
        {
            builder.AddView(instrumentName, config);
        }

        return builder;
    }

    public static MeterProviderBuilder AddViews(this MeterProviderBuilder builder, params Func<Instrument, MetricStreamConfiguration?>[] views)
    {
        foreach (var view in views)
        {
            builder.AddView(view);
        }

        return builder;
    }

    public static MeterProviderBuilder AddViews(this MeterProviderBuilder builder, params (string, string)[] views)
    {
        foreach (var (instrumentName, name) in views)
        {
            builder.AddView(instrumentName, name);
        }

        return builder;
    }

    public static MeterProviderBuilder AddMetrics<T>(this MeterProviderBuilder builder)
        where T : CustomMetrics
    {
        T customMetrics = (T)typeof(T).GetField("Instance", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
        builder.AddMeter(customMetrics.ObservabilityName);
        builder.AddViews(customMetrics.Views);
        return builder;
    }
}
