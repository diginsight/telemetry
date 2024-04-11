namespace Diginsight.Strings;

public sealed class NonLogStringable : ILogStringable
{
    private readonly Type type;

    bool ILogStringable.IsDeep => false;
    bool ILogStringable.CanCycle => false;

    public NonLogStringable(Type type)
    {
        this.type = type;
    }

    public void AppendTo(AppendingContext appendingContext)
    {
        appendingContext
            .ComposeAndAppendType(type)
            .AppendDirect('!');
    }
}
