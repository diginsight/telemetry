using System.Text;

namespace Diginsight.Strings;

public ref struct MemberAppender
{
    private readonly StringBuilder stringBuilder;
    private readonly AppendingContext appendingContext;
    private readonly AllottingCounter counter;
    private bool isAlive;

    internal MemberAppender(StringBuilder stringBuilder, AppendingContext appendingContext, AllottingCounter counter, bool isAlive)
    {
        this.stringBuilder = stringBuilder;
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

        stringBuilder.Append(LogStringTokens.Separator2);

        try
        {
            counter.Decrement();
            isAlive = true;

            stringBuilder
                .Append(memberName)
                .Append(LogStringTokens.Value)
                .AppendLogString(memberValue, appendingContext, incrementDepth, configureVariables, configureMetaProperties);
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
