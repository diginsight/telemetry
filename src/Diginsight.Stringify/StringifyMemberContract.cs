using Diginsight.Analyzers;
using System.Diagnostics;

namespace Diginsight.Stringify;

/// <summary>
/// Represents configurable stringification rules for a member.
/// </summary>
[NonSealed]
public class StringifyMemberContract : IStringifyMemberContract
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StringifyMemberContract" /> class.
    /// </summary>
    public static readonly IStringifyMemberContract Empty = new StringifyMemberContract();

    private readonly Type? memberType;

    /// <summary>
    /// Gets the stringifier type.
    /// </summary>
    protected Type? stringifierType;
    /// <summary>
    /// Gets the stringifier args.
    /// </summary>
    protected object[]? stringifierArgs;

    /// <summary>
    /// Gets a value indicating whether the type or member is included in stringification.
    /// </summary>
    public bool? Included { get; set; }

    /// <summary>
    /// Gets the output member name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets the custom stringifier type.
    /// </summary>
    public Type? StringifierType
    {
        get => stringifierType;
        set
        {
            if (value is not null && !typeof(IStringifier).IsAssignableFrom(value))
            {
                throw new ArgumentException($"Type '{value.Name}' is not assignable to {nameof(IStringifier)}");
            }

            stringifierType = value;
        }
    }

    /// <summary>
    /// Gets the custom stringifier constructor arguments.
    /// </summary>
    public object[] StringifierArgs
    {
        get => stringifierArgs ??= [ ];
        set => stringifierArgs = value;
    }

    /// <summary>
    /// Gets the member ordering value.
    /// </summary>
    public int? Order { get; set; }

    private StringifyMemberContract()
    {
        memberType = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StringifyMemberContract" /> class.
    /// </summary>
    /// <param name="memberType">The member type.</param>
    private protected StringifyMemberContract(Type memberType)
    {
        this.memberType = memberType;
    }

    internal static StringifyMemberContract For(Type memberType)
    {
        return (StringifyMemberContract)Activator.CreateInstance(typeof(StringifyMemberContract<>).MakeGenericType(memberType))!;
    }

    /// <summary>
    /// Configures this member to use a custom memberwise type contract.
    /// </summary>
    /// <param name="configureContract">The action used to configure the contract.</param>
    /// <returns>The member contract.</returns>
    public StringifyMemberContract WithCustomTypeContract(Action<StringifyTypeContract> configureContract)
    {
        StringifyTypeContract typeContract = StringifyTypeContract.For(memberType ?? throw new UnreachableException("Dummy member contract"));
        configureContract(typeContract);

        stringifierType = typeof(CustomMemberwiseStringifier);
        stringifierArgs = [ typeContract ];

        return this;
    }
}

/// <summary>
/// Represents configurable stringification rules for a member.
/// </summary>
/// <typeparam name="T">The type.</typeparam>
public sealed class StringifyMemberContract<T> : StringifyMemberContract
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StringifyMemberContract" /> class.
    /// </summary>
    public StringifyMemberContract()
        : base(typeof(T)) { }

    /// <summary>
    /// Configures this member to use a custom memberwise type contract.
    /// </summary>
    /// <typeparam name="T">The type.</typeparam>
    /// <param name="configureContract">The action used to configure the contract.</param>
    /// <returns>The member contract.</returns>
    public StringifyMemberContract<T> WithCustomTypeContract(Action<StringifyTypeContract<T>> configureContract)
    {
        StringifyTypeContract<T> typeContract = new ();
        configureContract(typeContract);

        stringifierType = typeof(CustomMemberwiseStringifier);
        stringifierArgs = [ typeContract ];

        return this;
    }
}
