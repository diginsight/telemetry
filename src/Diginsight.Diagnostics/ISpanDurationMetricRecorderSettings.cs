using System.Diagnostics;

namespace Diginsight.Diagnostics;

public interface ISpanDurationMetricRecorderSettings
{
    bool? ShouldRecord(Activity activity);

    Tags ExtractTags(Activity activity);
}
