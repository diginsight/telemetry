namespace Diginsight.Strings;

public sealed class NonLogStringable : ILogStringable
{
    private readonly Type type;

    public bool IsDeep => false;
    public bool CanCycle => false;

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
