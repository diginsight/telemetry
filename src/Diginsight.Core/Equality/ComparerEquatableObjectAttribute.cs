namespace Diginsight.Equality;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct)]
public sealed class ComparerEquatableObjectAttribute : EquatableObjectAttribute, IComparerEquatableObjectDescriptor
{
    private object?[]? comparerArgs;

    public override EqualityBehavior Behavior => EqualityBehavior.Comparer;

    public Type ComparerType { get; }

    public string? ComparerMember { get; }

    public object?[] ComparerArgs
    {
        get => comparerArgs ??= [ ];
        set => comparerArgs = value;
    }

    public ComparerEquatableObjectAttribute(Type comparerType)
    {
        ComparerType = comparerType;
    }

    public ComparerEquatableObjectAttribute(string comparerMember)
        : this(typeof(void), comparerMember) { }

    public ComparerEquatableObjectAttribute(Type comparerType, string comparerMember)
    {
        ComparerType = comparerType;
        ComparerMember = comparerMember;
    }
}
