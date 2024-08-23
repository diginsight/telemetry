namespace Diginsight.Equality;

public interface IEquatableMemberDescriptor : IEquatableDescriptor
{
    EqualityBehavior? Behavior { get; }

    int? Order { get; }

    IEquatableObjectDescriptor? TryToObjectDescriptor();
}
