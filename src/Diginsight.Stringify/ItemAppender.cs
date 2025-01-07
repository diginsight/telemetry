namespace Diginsight.Stringify;

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

    public StringifyContext End() => stringifyContext;
}
