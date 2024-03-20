using Microsoft.Extensions.Logging;
using OpenTelemetry;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics;

public sealed class CustomDurationMetricProcessor : BaseProcessor<Activity>
{
    private readonly ILogger logger;
    private readonly ICustomDurationMetricProcessorSettings? settings;

    public CustomDurationMetricProcessor(
        ILogger<CustomDurationMetricProcessor> logger,
        ICustomDurationMetricProcessorSettings? settings = null
    )
    {
        this.logger = logger;
        this.settings = settings;
    }

    public override void OnEnd(Activity activity)
    {
        try
        {
            double duration = activity.Duration.TotalMilliseconds;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            bool ShouldRecord(Instrument instrument) => settings?.ShouldRecord(activity, instrument) ?? true;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            Tag[] ExtractTags(Instrument instrument)
            {
                Tag[] tags = activity.GetCustomDurationMetricTags();
                return settings is not null ? tags.Concat(settings.ExtractTags(activity, instrument)).ToArray() : tags;
            }

            switch (activity.GetCustomProperty(ActivityCustomPropertyNames.CustomDurationMetric))
            {
                case Histogram<double> metric:
                    if (ShouldRecord(metric))
                    {
                        metric.Record(duration, ExtractTags(metric));
                    }
                    break;

                case Histogram<long> metric:
                    if (ShouldRecord(metric))
                    {
                        metric.Record((long)duration, ExtractTags(metric));
                    }
                    break;

                case null:
                    break;

                default:
                    throw new InvalidOperationException("Invalid duration metric in activity");
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Unhandled exception while recording custom duration metric of activity {ActivityName}", activity.OperationName);
        }
    }
}
