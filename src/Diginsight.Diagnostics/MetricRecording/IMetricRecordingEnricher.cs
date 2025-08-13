using System.Diagnostics;

namespace Diginsight.Diagnostics;

public interface IMetricRecordingEnricher
{
    Tags ExtractTags(Activity activity);
}
