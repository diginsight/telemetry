using System.Text;

namespace Diginsight.Strings;

public sealed class NonLoggable : ILoggable
{
    private readonly Type type;

    public bool IsDeep => false;
    public bool CanCycle => false;

    public NonLoggable(Type type)
    {
        this.type = type;
    }

    public void AppendTo(StringBuilder stringBuilder, LoggingContext loggingContext)
    {
        stringBuilder
            .AppendLogString(type, loggingContext, false)
            .Append('!');
    }
}
