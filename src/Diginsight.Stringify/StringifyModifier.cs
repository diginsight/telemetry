namespace Diginsight.Stringify;

public sealed class StringifyModifier : IStringifyModifier
{
    public object? Subject { get; }
    public bool? Atomic { get; init; }
    public Action<StringifyVariableConfiguration>? ConfigureVariables { get; init; }
    public Action<IDictionary<string, object?>>? ConfigureMetaProperties { get; init; }
    public Expiration? MaxTime { get; init; }

    public StringifyModifier(object? subject)
    {
        Subject = subject;
    }
}
