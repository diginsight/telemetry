using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace Diginsight.Stringify;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class StringifyContractExtensions
{
    public static StringifyTypeContract<T> GetOrAdd<T>(this StringifyTypeContractAccessor contractAccessor)
    {
        return (StringifyTypeContract<T>)contractAccessor.GetOrAdd(typeof(T));
    }

    public static StringifyTypeContractAccessor GetOrAdd(
        this StringifyTypeContractAccessor contractAccessor, Type type, Action<StringifyTypeContract> configureContract
    )
    {
        StringifyTypeContract contract = contractAccessor.GetOrAdd(type);
        configureContract(contract);
        return contractAccessor;
    }

    public static StringifyTypeContractAccessor GetOrAdd<T>(
        this StringifyTypeContractAccessor contractAccessor, Action<StringifyTypeContract<T>> configureContract
    )
    {
        StringifyTypeContract<T> contract = contractAccessor.GetOrAdd<T>();
        configureContract(contract);
        return contractAccessor;
    }

    public static StringifyTypeContract GetOrAdd(
        this StringifyTypeContract typeContract, string memberName, Action<StringifyMemberContract> configureContract
    )
    {
        StringifyMemberContract memberContract = typeContract.GetOrAdd(memberName);
        configureContract(memberContract);
        return typeContract;
    }

    public static StringifyTypeContract GetOrAdd(
        this StringifyTypeContract typeContract, MemberInfo member, Action<StringifyMemberContract> configureContract
    )
    {
        StringifyMemberContract memberContract = typeContract.GetOrAdd(member);
        configureContract(memberContract);
        return typeContract;
    }

    public static StringifyTypeContract<T> GetOrAdd<T, TMember>(
        this StringifyTypeContract<T> typeContract, Expression<Func<T, TMember>> expression, Action<StringifyMemberContract<TMember>> configureContract
    )
    {
        StringifyMemberContract<TMember> memberContract = typeContract.GetOrAdd(expression);
        configureContract(memberContract);
        return typeContract;
    }
}
