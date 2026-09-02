namespace Diginsight.Stringify;

/// <summary>
/// Represents a modifier that customizes how a subject is stringified.
/// </summary>
public interface IStringifyModifier
{
    /// <summary>
    /// Gets the subject being stringified.
    /// </summary>
    object? Subject { get; }
    /// <summary>
    /// Gets a value indicating whether the subject is appended atomically.
    /// </summary>
    bool? Atomic { get; }
    /// <summary>
    /// Gets the action used to configure variable options.
    /// </summary>
    Action<StringifyVariableConfiguration>? ConfigureVariables { get; }
    /// <summary>
    /// Gets the action used to configure meta properties.
    /// </summary>
    Action<IDictionary<string, object?>>? ConfigureMetaProperties { get; }
    /// <summary>
    /// Gets the maximum allotted stringification time.
    /// </summary>
    Expiration? MaxTime { get; }
}
