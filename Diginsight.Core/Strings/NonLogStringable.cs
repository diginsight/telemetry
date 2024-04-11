namespace Diginsight.Strings;

public sealed class NonLogStringable : ILogStringable
{
    private readonly Type type;

    bool ILogStringable.IsDeep => false;
    object? ILogStringable.Subject => null;

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
