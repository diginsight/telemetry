namespace Diginsight.Stringify;

/// <summary>
/// Represents a fluent appender for collection items.
/// </summary>
public sealed class ItemAppender
{
    private readonly StringifyContext stringifyContext;
    private readonly AllottedCounter counter;
    private readonly string separator;
    private bool isAlive;

    internal ItemAppender(StringifyContext stringifyContext, AllottedCounter counter, string separator, bool isAlive)
    {
        this.stringifyContext = stringifyContext;
        this.counter = counter;
        this.separator = separator;
        this.isAlive = isAlive;
    }

    /// <summary>
    /// Appends another collection item if the allotted limits have not been reached.
    /// </summary>
    /// <param name="itemValue">The item value.</param>
    /// <param name="atomic">A value indicating whether the value is appended atomically.</param>
    /// <param name="configureVariables">The action used to configure variable options.</param>
    /// <param name="configureMetaProperties">The action used to configure meta properties.</param>
    /// <returns>The item appender.</returns>
    public ItemAppender ThenItem(
        object? itemValue,
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
                .ComposeAndAppend(itemValue, atomic, configureVariables, configureMetaProperties);
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
