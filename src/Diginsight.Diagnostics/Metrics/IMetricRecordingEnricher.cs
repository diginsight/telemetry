using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Diginsight.Diagnostics;

public interface IMetricRecordingEnricher
{
    Tags ExtractTags(Activity activity, Instrument instrument);
}
