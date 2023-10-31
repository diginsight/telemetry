using System.Collections;

namespace Diginsight.Strings;

// FIXME StringBuilderExtensions
public static class StringBuilderExtensions
{
    public static AppendingContext AppendEnumerator<T>(
        this AppendingContext appendingContext,
        T enumerator,
        Action<AppendingContext, T> appendCurrent,
        AllottingCounter counter,
        string separator = LogStringTokens.Separator2
    )
        where T : IEnumerator
    {
        if (!enumerator.MoveNext())
        {
            return appendingContext;
        }

        try
        {
            void AppendEntry()
            {
                counter.Decrement();
                appendingContext.ThrowIfTimeIsOver();
                appendCurrent(appendingContext, enumerator);
            }

            AppendEntry();
            while (enumerator.MoveNext())
            {
                appendingContext.AppendPunctuation(separator);
                AppendEntry();
            }
        }
        catch (MaxAllottedShortCircuit)
        {
            appendingContext.AppendPunctuation(LogStringTokens.Ellipsis);
        }

        return appendingContext;
    }

    public static AppendingContext AppendDelimited(
        this AppendingContext appendingContext,
        char beginDelim,
        char endDelim,
        Action<AppendingContext> appendContent,
        bool incrementDepth = true,
        Action<LogStringVariableConfiguration>? configureVariables = null,
        Action<IDictionary<string, object?>>? configureMetaProperties = null
    )
    {
        if (appendingContext.IsTimeOver)
        {
            return appendingContext.AppendPunctuation(LogStringTokens.Ellipsis);
        }

        appendingContext.AppendPunctuation(beginDelim);

        using (appendingContext.WithVariablesSafe(configureVariables))
        using (appendingContext.WithMetaPropertiesSafe(configureMetaProperties))
        using (appendingContext.IncrementDepth(incrementDepth, out bool isMaxDepth))
        {
            if (isMaxDepth)
            {
                appendingContext.AppendPunctuation(LogStringTokens.Deep);
            }
            else
            {
                appendContent(appendingContext);
            }
        }

        return appendingContext.AppendPunctuation(endDelim);
    }

    // FIXME StringBuilderExtensions.AppendMap
    public static AppendingContext AppendMap(
        this AppendingContext appendingContext,
        Type mapType,
        Action<AppendingContext> appendContent,
        bool incrementDepth = true,
        Action<LogStringVariableConfiguration>? configureVariables = null,
        Action<IDictionary<string, object?>>? configureMetaProperties = null
    )
    {
        //using (appendingContext.WithAtomic())
        {
            appendingContext.ComposeAndAppend(mapType, false);
        }

        return appendingContext.AppendDelimited(
            LogStringTokens.MapBegin,
            LogStringTokens.MapEnd,
            appendContent,
            incrementDepth,
            configureVariables,
            configureMetaProperties
        );
    }

    // FIXME StringBuilderExtensions.AppendCollection
    public static AppendingContext AppendCollection(
        this AppendingContext appendingContext,
        Type collectionType,
        Action<AppendingContext> appendContent,
        int? count = null,
        bool incrementDepth = true,
        Action<LogStringVariableConfiguration>? configureVariables = null,
        Action<IDictionary<string, object?>>? configureMetaProperties = null
    )
    {
        //using (appendingContext.WithAtomic())
        {
            appendingContext
                .ComposeAndAppend(
                    collectionType,
                    false,
                    configureMetaProperties: x => { x[MemberInfoLogStringProvider.CollectionLengthMetaProperty] = count; }
                );
        }

        return appendingContext.AppendDelimited(
            LogStringTokens.CollectionBegin,
            LogStringTokens.CollectionEnd,
            appendContent,
            incrementDepth,
            configureVariables,
            configureMetaProperties
        );
    }

    public static MemberAppender ComposeAndAppendMember(
        this AppendingContext appendingContext,
        string memberName,
        object? memberValue,
        bool incrementDepth = true,
        Action<LogStringVariableConfiguration>? configureVariables = null,
        Action<IDictionary<string, object?>>? configureMetaProperties = null
    )
    {
        AllottingCounter counter = appendingContext.CountMemberwiseProperties();

        bool isAlive;
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

        return new MemberAppender(appendingContext, counter, isAlive);
    }

    public static ItemAppender AppendItem(
        this AppendingContext appendingContext,
        object? itemValue,
        bool incrementDepth = true,
        Action<LogStringVariableConfiguration>? configureVariables = null,
        Action<IDictionary<string, object?>>? configureMetaProperties = null
    )
    {
        AllottingCounter counter = appendingContext.CountCollectionItems();

        bool isAlive;
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

        return new ItemAppender(appendingContext, counter, isAlive);
    }
}
