using System.Diagnostics.Metrics;

namespace Diginsight.Diagnostics;

public interface ISpanDurationMetricRecorder : IActivityListenerLogic
{
    bool IsSpanDurationMetric(Instrument instrument);
}
