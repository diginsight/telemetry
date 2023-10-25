using OpenTelemetry;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Diagnostics;
using System.Text;

namespace Common
{
    public sealed class DurationMetricProcessor : BaseProcessor<Activity>
    {
        private static readonly Histogram<double> SpanDurationMetric = ObservabilityDefaults.Meter.CreateHistogram<double>("span_duration", "ms");

        public override void OnStart(Activity activity) { }

        public override void OnEnd(Activity activity)
        {
            double duration = activity.Duration.TotalMilliseconds;
            TagList tags = new()
            {
                new Tag("span_name", activity.OperationName),
                new Tag("status", activity.Status.ToString())
            };
            tags.Concat(activity.TagObjects);
            SpanDurationMetric.Record(duration, tags);
            switch (activity.GetCustomProperty(ActivityCustomPropertyNames.DurationMetric))
            {
                case Histogram<double> durationMetric:
                    durationMetric.Record(duration, activity.GetDurationMetricTags());
                    break;

                case Histogram<long> durationMetric:
                    durationMetric.Record((long)duration, activity.GetDurationMetricTags());
                    break;

                case null:
                    break;

                default:
                    throw new InvalidOperationException("Invalid duration metric in activity");
            }
        }
    }
}
