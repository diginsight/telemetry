namespace Diginsight.Stringify;

public interface IStringifyModifier
{
    object? Subject { get; }
    bool? Atomic { get; }
    Action<StringifyVariableConfiguration>? ConfigureVariables { get; }
    Action<IDictionary<string, object?>>? ConfigureMetaProperties { get; }
    Expiration? MaxTime { get; }
}
