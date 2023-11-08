namespace Diginsight.Strings;

public sealed class ItemAppender
{
    private readonly AppendingContext appendingContext;
    private readonly AllottingCounter counter;
    private readonly string separator;
    private bool isAlive;

    internal ItemAppender(AppendingContext appendingContext, AllottingCounter counter, string separator, bool isAlive)
    {
        this.appendingContext = appendingContext;
        this.counter = counter;
        this.separator = separator;
        this.isAlive = isAlive;
    }

    public ItemAppender ThenItem(
        object? itemValue,
        bool incrementDepth = true,
        bool? atomic = null,
        Action<LogStringVariableConfiguration>? configureVariables = null,
        Action<IDictionary<string, object?>>? configureMetaProperties = null
    )
    {
        if (!isAlive)
        {
            return this;
        }

        appendingContext.AppendDirect(separator);

        try
        {
            counter.Decrement();
            appendingContext.ThrowIfTimeIsOver();
            isAlive = true;

            appendingContext
                .ComposeAndAppend(itemValue, incrementDepth, atomic, configureVariables, configureMetaProperties);
        }
        catch (MaxAllottedShortCircuit)
        {
            appendingContext.AppendEllipsis();
            isAlive = false;
        }

        return this;
    }

    public AppendingContext End() => appendingContext;
}
