namespace Diginsight.Stringify;

public sealed class NonStringifiable : IStringifiable
{
    private readonly Type type;

    bool IStringifiable.IsDeep => false;
    object? IStringifiable.Subject => null;

    public NonStringifiable(Type type)
    {
        this.type = type;
    }

    public void AppendTo(StringifyContext stringifyContext)
    {
        stringifyContext
            .ComposeAndAppendType(type)
            .AppendDirect('!');
    }
}
