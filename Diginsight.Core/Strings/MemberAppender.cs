namespace Diginsight.Strings;

public ref struct MemberAppender
{
    private readonly AppendingContext appendingContext;
    private readonly AllottingCounter counter;
    private bool isAlive;

    internal MemberAppender(AppendingContext appendingContext, AllottingCounter counter, bool isAlive)
    {
        this.appendingContext = appendingContext;
        this.counter = counter;
        this.isAlive = isAlive;
    }

    public MemberAppender ThenMember(
        string memberName,
        object? memberValue,
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
                .AppendDirect(sb => sb.Append(memberName))
                .AppendPunctuation(LogStringTokens.Value)
                .ComposeAndAppend(memberValue, incrementDepth, configureVariables, configureMetaProperties);
        }
        catch (MaxAllottedShortCircuit)
        {
            appendingContext.AppendPunctuation(LogStringTokens.Ellipsis);
            isAlive = false;
        }

        return this;
    }

    public AppendingContext End() => appendingContext;
}
