using System.Text;

namespace Diginsight.Strings;

public interface ILogStringComposer
{
    void Append(
        object? obj,
        StringBuilder stringBuilder,
        Action<LogStringThresholdConfiguration>? configureThresholds = null,
        Action<IDictionary<string, object?>>? configureMetaProperties = null
    );

    LogStringComposerBuilder PrepareClone();
}
