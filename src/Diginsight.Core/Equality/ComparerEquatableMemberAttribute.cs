namespace Diginsight.Equality;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class ComparerEquatableMemberAttribute : EquatableMemberAttribute
{
    private object?[]? comparerArgs;

    protected override EqualityBehavior? Behavior => EqualityBehavior.Comparer;

    public Type ComparerType { get; }

    public string? ComparerMember { get; }

    public object?[] ComparerArgs
    {
        get => comparerArgs ??= [ ];
        set => comparerArgs = value;
    }

    public ComparerEquatableMemberAttribute(Type comparerType)
    {
        ComparerType = comparerType;
    }

    public ComparerEquatableMemberAttribute(Type comparerType, string comparerMember)
    {
        ComparerType = comparerType;
        ComparerMember = comparerMember;
    }

    public override IEquatableMemberDescriptor ToMemberDescriptor() =>
        new ComparerEquatableDescriptor(ComparerType, ComparerMember, ComparerArgs, OrderCore);
}
