using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace Diginsight.Stringify;

/// <summary>
/// Provides extension methods for configuring stringify contracts.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class StringifyContractExtensions
{
    /// <param name="contractAccessor">The StringifyTypeContractAccessor instance.</param>
    extension(StringifyTypeContractAccessor contractAccessor)
    {
        /// <summary>
        /// Gets an existing type contract or adds a new one.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <returns>The configured type contract.</returns>
        public StringifyTypeContract<T> GetOrAdd<T>()
        {
            return (StringifyTypeContract<T>)contractAccessor.GetOrAdd(typeof(T));
        }

        /// <summary>
        /// Gets an existing type contract or adds a new one.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="configureContract">The action used to configure the type contract.</param>
        /// <returns>The contract accessor, for chaining.</returns>
        public StringifyTypeContractAccessor GetOrAdd(
            Type type, Action<StringifyTypeContract> configureContract
        )
        {
            StringifyTypeContract contract = contractAccessor.GetOrAdd(type);
            configureContract(contract);
            return contractAccessor;
        }

        /// <summary>
        /// Gets an existing type contract or adds a new one.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="configureContract">The action used to configure the type contract.</param>
        /// <returns>The contract accessor, for chaining.</returns>
        public StringifyTypeContractAccessor GetOrAdd<T>(
            Action<StringifyTypeContract<T>> configureContract
        )
        {
            StringifyTypeContract<T> contract = contractAccessor.GetOrAdd<T>();
            configureContract(contract);
            return contractAccessor;
        }
    }

    /// <param name="typeContract">The StringifyTypeContract instance.</param>
    extension(StringifyTypeContract typeContract)
    {
        /// <summary>
        /// Gets an existing member contract or adds a new one.
        /// </summary>
        /// <param name="memberName">The member name.</param>
        /// <param name="configureContract">The action used to configure the member contract.</param>
        /// <returns>The type contract, for chaining.</returns>
        public StringifyTypeContract GetOrAdd(
            string memberName, Action<StringifyMemberContract> configureContract
        )
        {
            StringifyMemberContract memberContract = typeContract.GetOrAdd(memberName);
            configureContract(memberContract);
            return typeContract;
        }

        /// <summary>
        /// Gets an existing member contract or adds a new one.
        /// </summary>
        /// <param name="member">The member.</param>
        /// <param name="configureContract">The action used to configure the member contract.</param>
        /// <returns>The type contract, for chaining.</returns>
        public StringifyTypeContract GetOrAdd(
            MemberInfo member, Action<StringifyMemberContract> configureContract
        )
        {
            StringifyMemberContract memberContract = typeContract.GetOrAdd(member);
            configureContract(memberContract);
            return typeContract;
        }
    }

    /// <summary>
    /// Gets an existing member contract or adds a new one.
    /// </summary>
    /// <typeparam name="T">The type.</typeparam>
    /// <typeparam name="TMember">The member type.</typeparam>
    /// <param name="typeContract">The type contract.</param>
    /// <param name="expression">The member access expression.</param>
    /// <param name="configureContract">The action used to configure the member contract.</param>
    /// <returns>The type contract, for chaining.</returns>
    public static StringifyTypeContract<T> GetOrAdd<T, TMember>(
        this StringifyTypeContract<T> typeContract, Expression<Func<T, TMember>> expression, Action<StringifyMemberContract<TMember>> configureContract
    )
    {
        StringifyMemberContract<TMember> memberContract = typeContract.GetOrAdd(expression);
        configureContract(memberContract);
        return typeContract;
    }
}
