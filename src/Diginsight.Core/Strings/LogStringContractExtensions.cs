using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace Diginsight.Strings;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class LogStringContractExtensions
{
    public static LogStringTypeContract<T> GetOrAdd<T>(this LogStringTypeContractAccessor contractAccessor)
    {
        return (LogStringTypeContract<T>)contractAccessor.GetOrAdd(typeof(T));
    }

    public static LogStringTypeContractAccessor GetOrAdd(
        this LogStringTypeContractAccessor contractAccessor, Type type, Action<LogStringTypeContract> configureContract
    )
    {
        LogStringTypeContract contract = contractAccessor.GetOrAdd(type);
        configureContract(contract);
        return contractAccessor;
    }

    public static LogStringTypeContractAccessor GetOrAdd<T>(
        this LogStringTypeContractAccessor contractAccessor, Action<LogStringTypeContract<T>> configureContract
    )
    {
        LogStringTypeContract<T> contract = contractAccessor.GetOrAdd<T>();
        configureContract(contract);
        return contractAccessor;
    }

    public static LogStringTypeContract GetOrAdd(
        this LogStringTypeContract typeContract, string memberName, Action<LogStringMemberContract> configureContract
    )
    {
        LogStringMemberContract memberContract = typeContract.GetOrAdd(memberName);
        configureContract(memberContract);
        return typeContract;
    }

    public static LogStringTypeContract GetOrAdd(
        this LogStringTypeContract typeContract, MemberInfo member, Action<LogStringMemberContract> configureContract
    )
    {
        LogStringMemberContract memberContract = typeContract.GetOrAdd(member);
        configureContract(memberContract);
        return typeContract;
    }

    public static LogStringTypeContract<T> GetOrAdd<T, TMember>(
        this LogStringTypeContract<T> typeContract, Expression<Func<T, TMember>> expression, Action<LogStringMemberContract<TMember>> configureContract
    )
    {
        LogStringMemberContract<TMember> memberContract = typeContract.GetOrAdd(expression);
        configureContract(memberContract);
        return typeContract;
    }
}
