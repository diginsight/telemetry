using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace Diginsight.Equality;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class EqualityContractExtensions
{
    public static EqualityTypeContract<T> GetOrAdd<T>(this EqualityTypeContractAccessor contractAccessor)
    {
        return (EqualityTypeContract<T>)contractAccessor.GetOrAdd(typeof(T));
    }

    public static EqualityTypeContractAccessor GetOrAdd(
        this EqualityTypeContractAccessor contractAccessor, Type type, Action<EqualityTypeContract> configureContract
    )
    {
        EqualityTypeContract contract = contractAccessor.GetOrAdd(type);
        configureContract(contract);
        return contractAccessor;
    }

    public static EqualityTypeContractAccessor GetOrAdd<T>(
        this EqualityTypeContractAccessor contractAccessor, Action<EqualityTypeContract<T>> configureContract
    )
    {
        EqualityTypeContract<T> contract = contractAccessor.GetOrAdd<T>();
        configureContract(contract);
        return contractAccessor;
    }

    public static EqualityTypeContract GetOrAdd(
        this EqualityTypeContract typeContract, string memberName, Action<EqualityMemberContract> configureContract
    )
    {
        EqualityMemberContract memberContract = typeContract.GetOrAdd(memberName);
        configureContract(memberContract);
        return typeContract;
    }

    public static EqualityTypeContract GetOrAdd(
        this EqualityTypeContract typeContract, MemberInfo member, Action<EqualityMemberContract> configureContract
    )
    {
        EqualityMemberContract memberContract = typeContract.GetOrAdd(member);
        configureContract(memberContract);
        return typeContract;
    }

    public static EqualityTypeContract<T> GetOrAdd<T, TMember>(
        this EqualityTypeContract<T> typeContract, Expression<Func<T, TMember>> expression, Action<EqualityMemberContract<TMember>> configureContract
    )
    {
        EqualityMemberContract<TMember> memberContract = typeContract.GetOrAdd(expression);
        configureContract(memberContract);
        return typeContract;
    }
}
