using System.Text;

namespace Diginsight.Strings;

public interface ILogStringable
{
    bool IsDeep { get; }
    bool CanCycle { get; }

    void AppendTo(StringBuilder stringBuilder, LoggingContext loggingContext);
}
