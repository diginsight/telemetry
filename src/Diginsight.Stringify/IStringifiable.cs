namespace Diginsight.Stringify;

/// <summary>
/// Represents an object that can append its string representation to a stringify context.
/// </summary>
public interface IStringifiable
{
    /// <summary>
    /// Gets a value indicating whether the stringifiable value contributes to nesting depth.
    /// </summary>
    bool IsDeep
#if NET || NETSTANDARD2_1_OR_GREATER
        => true;
#else
    {
        get;
    }
#endif

    /// <summary>
    /// Gets the subject being stringified.
    /// </summary>
    object? Subject { get; } // TODO XMLDOC Explain when can or should be null, and when not

    /// <summary>
    /// Appends the string representation to the specified stringify context.
    /// </summary>
    /// <param name="stringifyContext">The stringify context.</param>
    void AppendTo(StringifyContext stringifyContext);
}
