using System.Diagnostics;

namespace Diginsight.Stringify;

public class StringifyMemberContract : IStringifyMemberContract
{
    public static readonly IStringifyMemberContract Empty = new StringifyMemberContract();

    private readonly Type? memberType;

    protected Type? stringifierType;
    protected object[]? stringifierArgs;

    public bool? Included { get; set; }

    public string? Name { get; set; }

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

    public object[] StringifierArgs
    {
        get => stringifierArgs ??= [ ];
        set => stringifierArgs = value;
    }

    public int? Order { get; set; }

    private StringifyMemberContract()
    {
        memberType = null;
    }

    private protected StringifyMemberContract(Type memberType)
    {
        this.memberType = memberType;
    }

    internal static StringifyMemberContract For(Type memberType)
    {
        return (StringifyMemberContract)Activator.CreateInstance(typeof(StringifyMemberContract<>).MakeGenericType(memberType))!;
    }

    public StringifyMemberContract WithCustomTypeContract(Action<StringifyTypeContract> configureContract)
    {
        StringifyTypeContract typeContract = StringifyTypeContract.For(memberType ?? throw new UnreachableException("Dummy member contract"));
        configureContract(typeContract);

        stringifierType = typeof(CustomMemberwiseStringifier);
        stringifierArgs = [ typeContract ];

        return this;
    }
}

public sealed class StringifyMemberContract<T> : StringifyMemberContract
{
    public StringifyMemberContract()
        : base(typeof(T)) { }

    public StringifyMemberContract<T> WithCustomTypeContract(Action<StringifyTypeContract<T>> configureContract)
    {
        StringifyTypeContract<T> typeContract = new ();
        configureContract(typeContract);

        stringifierType = typeof(CustomMemberwiseStringifier);
        stringifierArgs = [ typeContract ];

        return this;
    }
}
