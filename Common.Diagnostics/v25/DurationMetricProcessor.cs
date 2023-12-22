using OpenTelemetry;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Diagnostics;
using System.Text;
using System.Linq;

namespace Common
{
    public sealed class DurationMetricProcessor : BaseProcessor<Activity>
    {
        private static readonly Histogram<double> SpanDurationMetric = ObservabilityDefaults.Meter.CreateHistogram<double>("span_duration", "ms");
        private static readonly Histogram<long> ResponseSizeMetric = ObservabilityDefaults.Meter.CreateHistogram<long>("http_response_length", unit: "bytes");

        public override void OnStart(Activity activity) { }

        public override void OnEnd(Activity activity)
        {
            double duration = activity.Duration.TotalMilliseconds;

            TagList tags = new()
            {
                new Tag("span_name", activity.OperationName),
                new Tag("status", activity.Status.ToString())
            };

            foreach(var tag in activity.TagObjects)
            {
                tags.Add(tag);
            }
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
