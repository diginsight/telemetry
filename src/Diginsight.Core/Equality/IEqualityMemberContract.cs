namespace Diginsight.Equality;

public interface IEqualityMemberContract : IEqualityContract
{
    int? Order { get; }

    bool? Included { get; }
}
