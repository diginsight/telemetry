namespace Diginsight.Equality;

public interface IComparerEquatableDescriptor
{
    Type ComparerType { get; }
    string? ComparerMember { get; }
    object?[] ComparerArgs { get; }
}