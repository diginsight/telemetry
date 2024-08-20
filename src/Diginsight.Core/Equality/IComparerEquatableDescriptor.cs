namespace Diginsight.Equality;

public interface IComparerEquatableDescriptor : IEquatableDescriptor
{
    Type ComparerType { get; }
    string? ComparerMember { get; }
    object?[] ComparerArgs { get; }
}
