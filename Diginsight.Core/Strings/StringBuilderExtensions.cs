using System.Collections;
using System.Runtime.CompilerServices;
using System.Text;

namespace Diginsight.Strings;

public static class StringBuilderExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StringBuilder ComposeAndAppend(
        this StringBuilder stringBuilder,
        object? obj,
        AppendingContext appendingContext,
        bool incrementDepth = true,
        Action<LogStringVariableConfiguration>? configureVariables = null,
        Action<IDictionary<string, object?>>? configureMetaProperties = null
    )
    {
        appendingContext.ComposeAndAppend(obj, stringBuilder, incrementDepth, configureVariables, configureMetaProperties);
        return stringBuilder;
    }

    public static StringBuilder AppendEnumerator<T>(
        this StringBuilder stringBuilder,
        T enumerator,
        Action<T> appendCurrent,
        AllottingCounter counter,
        AppendingContext appendingContext,
        string separator = LogStringTokens.Separator2
    )
        where T : IEnumerator
    {
        if (!enumerator.MoveNext())
        {
            return stringBuilder;
        }

        try
        {
            void AppendEntry()
            {
                counter.Decrement();
                appendingContext.ThrowIfTimeIsOver();
                appendCurrent(enumerator);
            }

            AppendEntry();
            while (enumerator.MoveNext())
            {
                stringBuilder.Append(separator);
                AppendEntry();
            }
        }
        catch (MaxAllottedShortCircuit)
        {
            stringBuilder.Append(LogStringTokens.Ellipsis);
        }

        return stringBuilder;
    }

    public static StringBuilder AppendDelimited(
        this StringBuilder stringBuilder,
        char beginDelim,
        char endDelim,
        AppendingContext appendingContext,
        Action<StringBuilder, AppendingContext> appendContent,
        bool incrementDepth = true,
        Action<LogStringVariableConfiguration>? configureVariables = null,
        Action<IDictionary<string, object?>>? configureMetaProperties = null
    )
    {
        if (appendingContext.IsTimeOver)
        {
            return stringBuilder.Append(LogStringTokens.Ellipsis);
        }

        stringBuilder.Append(beginDelim);

        using (appendingContext.WithVariablesSafe(configureVariables))
        using (appendingContext.WithMetaPropertiesSafe(configureMetaProperties))
        using (appendingContext.IncrementDepth(incrementDepth, out bool isMaxDepth))
        {
            if (isMaxDepth)
            {
                stringBuilder.Append(LogStringTokens.Deep);
            }
            else
            {
                appendContent(stringBuilder, appendingContext);
            }
        }

        return stringBuilder.Append(endDelim);
    }

    public static StringBuilder AppendMap(
        this StringBuilder stringBuilder,
        Type mapType,
        AppendingContext appendingContext,
        Action<StringBuilder, AppendingContext> appendContent,
        bool incrementDepth = true,
        Action<LogStringVariableConfiguration>? configureVariables = null,
        Action<IDictionary<string, object?>>? configureMetaProperties = null
    )
    {
        using (appendingContext.WithAtomic())
        {
            stringBuilder.ComposeAndAppend(mapType, appendingContext, false);
        }

        return stringBuilder.AppendDelimited(
            LogStringTokens.MapBegin,
            LogStringTokens.MapEnd,
            appendingContext,
            appendContent,
            incrementDepth,
            configureVariables,
            configureMetaProperties
        );
    }

    public static StringBuilder AppendCollection(
        this StringBuilder stringBuilder,
        Type collectionType,
        AppendingContext appendingContext,
        Action<StringBuilder, AppendingContext> appendContent,
        int? count = null,
        bool incrementDepth = true,
        Action<LogStringVariableConfiguration>? configureVariables = null,
        Action<IDictionary<string, object?>>? configureMetaProperties = null
    )
    {
        using (appendingContext.WithAtomic())
        {
            stringBuilder
                .ComposeAndAppend(
                    collectionType,
                    appendingContext,
                    false,
                    configureMetaProperties: x => { x[MemberInfoLogStringProvider.CollectionLengthMetaProperty] = count; }
                );
        }

        return stringBuilder.AppendDelimited(
            LogStringTokens.CollectionBegin,
            LogStringTokens.CollectionEnd,
            appendingContext,
            appendContent,
            incrementDepth,
            configureVariables,
            configureMetaProperties
        );
    }

    public static MemberAppender ComposeAndAppendMember(
        this StringBuilder stringBuilder,
        string memberName,
        object? memberValue,
        AppendingContext appendingContext,
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

            stringBuilder
                .Append(memberName)
                .Append(LogStringTokens.Value)
                .ComposeAndAppend(memberValue, appendingContext, incrementDepth, configureVariables, configureMetaProperties);
        }
        catch (MaxAllottedCountShortCircuit)
        {
            stringBuilder.Append(LogStringTokens.Ellipsis);
            isAlive = false;
        }

        return new MemberAppender(stringBuilder, appendingContext, counter, isAlive);
    }

    public static ItemAppender AppendItem(
        this StringBuilder stringBuilder,
        object? itemValue,
        AppendingContext appendingContext,
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

            stringBuilder
                .ComposeAndAppend(itemValue, appendingContext, incrementDepth, configureVariables, configureMetaProperties);
        }
        catch (MaxAllottedCountShortCircuit)
        {
            stringBuilder.Append(LogStringTokens.Ellipsis);
            isAlive = false;
        }

        return new ItemAppender(stringBuilder, appendingContext, counter, isAlive);
    }
}
