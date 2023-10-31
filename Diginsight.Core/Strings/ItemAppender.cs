namespace Diginsight.Strings;

public ref struct ItemAppender
{
    private readonly AppendingContext appendingContext;
    private readonly AllottingCounter counter;
    private bool isAlive;

    internal ItemAppender(AppendingContext appendingContext, AllottingCounter counter, bool isAlive)
    {
        this.appendingContext = appendingContext;
        this.counter = counter;
        this.isAlive = isAlive;
    }

    public ItemAppender ThenItem(
        object? itemValue,
        bool incrementDepth = true,
        Action<LogStringVariableConfiguration>? configureVariables = null,
        Action<IDictionary<string, object?>>? configureMetaProperties = null
    )
    {
        if (!isAlive)
        {
            return this;
        }

        appendingContext.AppendPunctuation(LogStringTokens.Separator2);

        try
        {
            counter.Decrement();
            isAlive = true;

            appendingContext
                .ComposeAndAppend(itemValue, incrementDepth, configureVariables, configureMetaProperties);
        }
        catch (MaxAllottedCountShortCircuit)
        {
            appendingContext.AppendPunctuation(LogStringTokens.Ellipsis);
            isAlive = false;
        }

        return this;
    }

    public AppendingContext End() => appendingContext;
}
