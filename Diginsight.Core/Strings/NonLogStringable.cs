using System.Text;

namespace Diginsight.Strings;

// FIXME NonLogStringable
public sealed class NonLogStringable : ILogStringable
{
    private readonly Type type;

    public bool IsDeep => false;
    public bool CanCycle => false;

    public NonLogStringable(Type type)
    {
        this.type = type;
    }

    public void AppendTo(StringBuilder stringBuilder, AppendingContext appendingContext)
    {
        stringBuilder
            .ComposeAndAppend(type, appendingContext, false)
            .Append('!');
    }
}
