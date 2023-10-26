using System.Text;

namespace Diginsight.Strings;

public ref struct ItemAppender
{
    private readonly StringBuilder stringBuilder;
    private readonly LoggingContext loggingContext;
    private readonly AllottingCounter counter;
    private bool isAlive;

    internal ItemAppender(StringBuilder stringBuilder, LoggingContext loggingContext, AllottingCounter counter, bool isAlive)
    {
        this.stringBuilder = stringBuilder;
        this.loggingContext = loggingContext;
        this.counter = counter;
        this.isAlive = isAlive;
    }

    public ItemAppender ThenItem(
        object? itemValue,
        bool incrementDepth = true,
        Action<LogStringThresholdConfiguration>? configureThresholds = null,
        Action<IDictionary<string, object?>>? configureMetaProperties = null
    )
    {
        if (!isAlive)
        {
            return this;
        }

        stringBuilder.Append(LogStringTokens.Separator2);

        try
        {
            counter.Decrement();
            isAlive = true;

            stringBuilder
                .AppendLogString(itemValue, loggingContext, incrementDepth, configureThresholds, configureMetaProperties);
        }
        catch (MaxAllottedShortCircuit)
        {
            stringBuilder.Append(LogStringTokens.Ellipsis);
            isAlive = false;
        }

        return this;
    }

    public StringBuilder End() => stringBuilder;
}
