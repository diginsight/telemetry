namespace Diginsight.Stringify;

/// <summary>
/// Represents a fluent appender for member name-value pairs.
/// </summary>
public sealed class MemberAppender
{
    private readonly StringifyContext stringifyContext;
    private readonly AllottedCounter counter;
    private readonly string separator;
    private bool isAlive;

    internal MemberAppender(StringifyContext stringifyContext, AllottedCounter counter, string separator, bool isAlive)
    {
        this.stringifyContext = stringifyContext;
        this.counter = counter;
        this.separator = separator;
        this.isAlive = isAlive;
    }

    /// <summary>
    /// Appends another member if the allotted limits have not been reached.
    /// </summary>
    /// <param name="memberName">The member name.</param>
    /// <param name="memberValue">The member value.</param>
    /// <param name="atomic">A value indicating whether the value is appended atomically.</param>
    /// <param name="configureVariables">The action used to configure variable options.</param>
    /// <param name="configureMetaProperties">The action used to configure meta properties.</param>
    /// <returns>The member appender.</returns>
    public MemberAppender ThenMember(
        string memberName,
        object? memberValue,
        bool? atomic = null,
        Action<StringifyVariableConfiguration>? configureVariables = null,
        Action<IDictionary<string, object?>>? configureMetaProperties = null
    )
    {
        if (!isAlive)
        {
            return this;
        }

        stringifyContext.AppendDirect(separator);

        try
        {
            counter.Decrement();
            stringifyContext.ThrowIfTimeIsOver();
            isAlive = true;

            stringifyContext
                .AppendDirect(memberName)
                .AppendDirect(StringifyTokens.Value)
                .ComposeAndAppend(memberValue, atomic, configureVariables, configureMetaProperties);
        }
        catch (MaxAllottedShortCircuit)
        {
            stringifyContext.AppendEllipsis();
            isAlive = false;
        }

        return this;
    }

    /// <summary>
    /// Returns the underlying stringify context.
    /// </summary>
    /// <returns>The stringify context.</returns>
    public StringifyContext End() => stringifyContext;
}
