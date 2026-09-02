namespace Diginsight.Stringify;

/// <summary>
/// Represents a subject together with per-call stringification modifiers.
/// </summary>
/// <seealso cref="StringifyContext.ComposeAndAppend" />
public sealed class StringifyModifier : IStringifyModifier
{
    /// <summary>
    /// Gets the subject being stringified.
    /// </summary>
    public object? Subject { get; }
    /// <summary>
    /// Gets a value indicating whether the subject is appended atomically.
    /// </summary>
    public bool? Atomic { get; init; }
    /// <summary>
    /// Gets the action used to configure variable options.
    /// </summary>
    public Action<StringifyVariableConfiguration>? ConfigureVariables { get; init; }
    /// <summary>
    /// Gets the action used to configure meta properties.
    /// </summary>
    public Action<IDictionary<string, object?>>? ConfigureMetaProperties { get; init; }
    /// <summary>
    /// Gets the maximum allotted stringification time.
    /// </summary>
    public Expiration? MaxTime { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StringifyModifier" /> class.
    /// </summary>
    /// <param name="subject">The subject to stringify.</param>
    public StringifyModifier(object? subject)
    {
        Subject = subject;
    }
}
