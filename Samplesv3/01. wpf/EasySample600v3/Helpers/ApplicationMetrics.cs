using OpenTelemetry.Metrics;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySample
{
    public abstract class CustomMetrics
    {
        public Meter Meter => new(ObservabilityName);
        public abstract string ObservabilityName { get; }
        public virtual (string, MetricStreamConfiguration)[] Views => Array.Empty<(string, MetricStreamConfiguration)>();
    }
    public static class MetricConfigurations
    {
        public static MetricStreamConfiguration SlowDurationHistogram = new ExplicitBucketHistogramConfiguration()
        {
            Boundaries = new double[] { 0, 100, 250, 500, 1000, 2500, 5000, 7500, 10000, 30000 },
        };

        public static MetricStreamConfiguration FastDurationHistogram = new ExplicitBucketHistogramConfiguration()
        {
            Boundaries = new double[] { 0, 1, 2.5, 5, 25, 100, 250, 500, 1000, 5000 },
        };

        public static MetricStreamConfiguration MediumSizeHistogram = new ExplicitBucketHistogramConfiguration()
        {
            Boundaries = new double[] { 0, 100, 500, 1000, 5000, 10_000, 50_000, 100_000, 250_000, 500_000, 1_000_000 },
        };

        public static MetricStreamConfiguration LargeSizeHistogram = new ExplicitBucketHistogramConfiguration()
        {
            Boundaries = new double[] { 0, 500, 1000, 5000, 10_000, 50_000, 100_000, 250_000, 500_000, 1_000_000, 5_000_000 },
        };
    }
    public class ApplicationMetrics : CustomMetrics
    {
        public static readonly ApplicationMetrics Instance = new();

        public override string ObservabilityName => App.Current.GetType().Assembly.GetName().Name;
        public static string StaticObservabilityName => App.Current.GetType().Assembly.GetName().Name;

        public override (string, MetricStreamConfiguration)[] Views => new[]{
            ("push_data_object_size", MetricConfigurations.MediumSizeHistogram),
        };

        public readonly TimerHistogram WatticsPushDataDuration;
        public readonly Counter<int> Sites;
        public readonly Counter<int> Equipments;
        public readonly Counter<int> SitesOnBlackList;
        public readonly Counter<int> EquipmentsOnBlackList;
        public readonly Histogram<double> WatticsPushDataSize; // ms/kbyte

        private ApplicationMetrics()
        {
            WatticsPushDataDuration = Meter.CreateTimer("push_data_duration");
            Sites = Meter.CreateCounter<int>("sites_count");
            Equipments = Meter.CreateCounter<int>("equipment_count");
            SitesOnBlackList = Meter.CreateCounter<int>("sites_on_blacklist_count");
            EquipmentsOnBlackList = Meter.CreateCounter<int>("equipment_on_blacklist_count");
            WatticsPushDataSize = Meter.CreateHistogram<double>("push_data_object_size", "vbytes");
        }
    }
}
