using System.Text;

namespace Diginsight.Strings;

public interface ILogStringComposer
{
    void ComposeTo(
        object? obj,
        StringBuilder stringBuilder,
        Action<LogStringVariableConfiguration>? configureVariables = null,
        Action<IDictionary<string, object?>>? configureMetaProperties = null
    );

    LogStringComposerBuilder PrepareClone();
}
