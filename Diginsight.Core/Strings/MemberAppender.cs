namespace Diginsight.Strings;

public sealed class MemberAppender
{
    private readonly AppendingContext appendingContext;
    private readonly AllottingCounter counter;
    private readonly string separator;
    private bool isAlive;

    internal MemberAppender(AppendingContext appendingContext, AllottingCounter counter, string separator, bool isAlive)
    {
        this.appendingContext = appendingContext;
        this.counter = counter;
        this.separator = separator;
        this.isAlive = isAlive;
    }

    public MemberAppender ThenMember(
        string memberName,
        object? memberValue,
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
                .AppendDirect(memberName)
                .AppendDirect(LogStringTokens.Value)
                .ComposeAndAppend(memberValue, incrementDepth, atomic, configureVariables, configureMetaProperties);
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
