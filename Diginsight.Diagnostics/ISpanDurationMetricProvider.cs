using System.Diagnostics.Metrics;

namespace Diginsight.Diagnostics;

public interface ISpanDurationMetricProvider
{
    Histogram<double> Metric { get; }
}
