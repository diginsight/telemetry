namespace Diginsight.Equality;

internal sealed record ComparerEquatableDescriptor(Type ComparerType, string? ComparerMember, object?[] ComparerArgs, int? Order)
    : EquatableDescriptor(EqualityBehavior.Comparer, Order), IComparerEquatableObjectDescriptor, IComparerEquatableMemberDescriptor;
