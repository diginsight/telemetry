using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Diginsight.Stringify;

/// <summary>
/// Provides extension methods for appending structured content to stringify contexts.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class StringifyContextExtensions
{
    /// <param name="stringifyContext">The StringifyContext instance.</param>
    extension(StringifyContext stringifyContext)
    {
        /// <summary>
        /// Appends the &quot;ellipsis&quot; token.
        /// </summary>
        /// <returns>The stringify context.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StringifyContext AppendEllipsis()
        {
            return stringifyContext.AppendDirect(StringifyTokens.Ellipsis);
        }

        /// <summary>
        /// Appends the &quot;deep&quot; token.
        /// </summary>
        /// <returns>The stringify context.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StringifyContext AppendDeep()
        {
            return stringifyContext.AppendDirect(StringifyTokens.Deep);
        }

        /// <summary>
        /// Appends the &quot;error&quot; token.
        /// </summary>
        /// <returns>The stringify context.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StringifyContext AppendError()
        {
            return stringifyContext.AppendDirect(StringifyTokens.Error);
        }

        /// <summary>
        /// Appends the &quot;null&quot; token.
        /// </summary>
        /// <returns>The stringify context.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StringifyContext AppendNull()
        {
            return stringifyContext.AppendDirect(StringifyTokens.Null);
        }

        /// <summary>
        /// Appends items from an enumerator until it ends or a configured limit is reached.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="enumerator">The enumerator.</param>
        /// <param name="appendCurrent">The action that appends the current enumerator value.</param>
        /// <param name="counter">The allotted counter.</param>
        /// <param name="separator">The separator.</param>
        /// <returns>The stringify context.</returns>
        public StringifyContext AppendEnumerator<T>(
            T enumerator,
            Action<StringifyContext, T> appendCurrent,
            AllottedCounter counter,
            string separator = StringifyTokens.Separator2
        )
            where T : IEnumerator
        {
            try
            {
                bool first = true;
                bool over = false;

                bool? MoveNext()
                {
                    try
                    {
                        return enumerator.MoveNext();
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                }

                void AppendSeparator()
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        stringifyContext.AppendDirect(separator);
                    }
                }

                while (!over)
                {
                    switch (MoveNext())
                    {
                        case null:
                            AppendSeparator();
                            stringifyContext.AppendError();
                            over = true;
                            break;

                        case false:
                            over = true;
                            break;

                        case true:
                            AppendSeparator();
                            counter.Decrement();
                            stringifyContext.ThrowIfTimeIsOver();
                            appendCurrent(stringifyContext, enumerator);
                            break;
                    }
                }
            }
            catch (MaxAllottedShortCircuit)
            {
                stringifyContext.AppendEllipsis();
            }

            return stringifyContext;
        }

        /// <summary>
        /// Appends delimited content.
        /// </summary>
        /// <param name="beginDelim">The beginning delimiter.</param>
        /// <param name="endDelim">The ending delimiter.</param>
        /// <param name="appendContent">The action that appends content.</param>
        /// <param name="configureVariables">The action used to configure variable options.</param>
        /// <param name="configureMetaProperties">The action used to configure meta properties.</param>
        /// <returns>The stringify context.</returns>
        public StringifyContext AppendDelimited(
            char beginDelim,
            char endDelim,
            Action<StringifyContext> appendContent,
            Action<StringifyVariableConfiguration>? configureVariables = null,
            Action<IDictionary<string, object?>>? configureMetaProperties = null
        )
        {
            stringifyContext.AppendDirect(beginDelim);

            using (stringifyContext.WithVariablesSafe(configureVariables))
            using (stringifyContext.WithMetaPropertiesSafe(configureMetaProperties))
            {
                appendContent(stringifyContext);
            }

            return stringifyContext.AppendDirect(endDelim);
        }

        /// <summary>
        /// Appends map content with type information.
        /// </summary>
        /// <param name="mapType">The map type.</param>
        /// <param name="appendContent">The action that appends content.</param>
        /// <param name="configureVariables">The action used to configure variable options.</param>
        /// <param name="configureMetaProperties">The action used to configure meta properties.</param>
        /// <returns>The stringify context.</returns>
        public StringifyContext AppendMap(
            Type mapType,
            Action<StringifyContext> appendContent,
            Action<StringifyVariableConfiguration>? configureVariables = null,
            Action<IDictionary<string, object?>>? configureMetaProperties = null
        )
        {
            return stringifyContext
                .ComposeAndAppendType(mapType)
                .AppendDelimited(
                    StringifyTokens.MapBegin,
                    StringifyTokens.MapEnd,
                    appendContent,
                    configureVariables,
                    configureMetaProperties
                );
        }

        /// <summary>
        /// Appends collection content with type information.
        /// </summary>
        /// <param name="collectionType">The collection type.</param>
        /// <param name="appendContent">The action that appends content.</param>
        /// <param name="count">The collection count, if applicable.</param>
        /// <param name="configureVariables">The action used to configure variable options.</param>
        /// <param name="configureMetaProperties">The action used to configure meta properties.</param>
        /// <returns>The stringify context.</returns>
        public StringifyContext AppendCollection(
            Type collectionType,
            Action<StringifyContext> appendContent,
            int? count = null,
            Action<StringifyVariableConfiguration>? configureVariables = null,
            Action<IDictionary<string, object?>>? configureMetaProperties = null
        )
        {
            return stringifyContext
                .ComposeAndAppendType(collectionType, count)
                .AppendDelimited(
                    StringifyTokens.CollectionBegin,
                    StringifyTokens.CollectionEnd,
                    appendContent,
                    configureVariables,
                    configureMetaProperties
                );
        }

        /// <summary>
        /// Composes and appends the first member in a fluent member append sequence.
        /// </summary>
        /// <param name="memberName">The member name.</param>
        /// <param name="memberValue">The member value.</param>
        /// <param name="separator">The separator.</param>
        /// <param name="atomic">A value indicating whether the value is appended atomically.</param>
        /// <param name="configureVariables">The action used to configure variable options.</param>
        /// <param name="configureMetaProperties">The action used to configure meta properties.</param>
        /// <returns>The member appender.</returns>
        public MemberAppender ComposeAndAppendMember(
            string memberName,
            object? memberValue,
            string separator = StringifyTokens.Separator2,
            bool? atomic = null,
            Action<StringifyVariableConfiguration>? configureVariables = null,
            Action<IDictionary<string, object?>>? configureMetaProperties = null
        )
        {
            AllottedCounter counter = stringifyContext.CountMemberwiseProperties();

            bool isAlive;
            try
            {
                counter.Decrement();
                stringifyContext.ThrowIfTimeIsOver();
                isAlive = true;

                stringifyContext
                    .AppendDirect(memberName)
                    .AppendDirect(StringifyTokens.Value)
                    .ComposeAndAppend(memberValue, atomic, configureVariables, configureMetaProperties);
            }
            catch (MaxAllottedShortCircuit)
            {
                stringifyContext.AppendEllipsis();
                isAlive = false;
            }

            return new MemberAppender(stringifyContext, counter, separator, isAlive);
        }

        /// <summary>
        /// Composes and appends the first item in a fluent item append sequence.
        /// </summary>
        /// <param name="itemValue">The item value.</param>
        /// <param name="separator">The separator.</param>
        /// <param name="atomic">A value indicating whether the value is appended atomically.</param>
        /// <param name="configureVariables">The action used to configure variable options.</param>
        /// <param name="configureMetaProperties">The action used to configure meta properties.</param>
        /// <returns>The item appender.</returns>
        public ItemAppender ComposeAndAppendItem(
            object? itemValue,
            string separator = StringifyTokens.Separator2,
            bool? atomic = null,
            Action<StringifyVariableConfiguration>? configureVariables = null,
            Action<IDictionary<string, object?>>? configureMetaProperties = null
        )
        {
            AllottedCounter counter = stringifyContext.CountCollectionItems();

            bool isAlive;
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

            return new ItemAppender(stringifyContext, counter, separator, isAlive);
        }

        /// <summary>
        /// Applies temporary variable configuration when a configuration action is provided.
        /// </summary>
        /// <param name="configureVariables">The action used to configure variable options.</param>
        /// <returns>A disposable that restores the previous variable configuration when disposed, or <c>null</c> when no configuration action is provided.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [return: NotNullIfNotNull(nameof(configureVariables))]
        public IDisposable? WithVariablesSafe(Action<StringifyVariableConfiguration>? configureVariables)
        {
            return configureVariables is null ? null : stringifyContext.WithVariables(configureVariables);
        }

        /// <summary>
        /// Applies temporary meta properties when a configuration action is provided.
        /// </summary>
        /// <param name="configureMetaProperties">The action used to configure meta properties.</param>
        /// <returns>A disposable that restores the previous meta properties when disposed, or <c>null</c> when no configuration action is provided.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [return: NotNullIfNotNull(nameof(configureMetaProperties))]
        public IDisposable? WithMetaPropertiesSafe(Action<IDictionary<string, object?>>? configureMetaProperties)
        {
            return configureMetaProperties is null ? null : stringifyContext.WithMetaProperties(configureMetaProperties);
        }

        /// <summary>
        /// Increments the current stringification depth.
        /// </summary>
        /// <param name="condition">A value indicating whether the depth should be incremented.</param>
        /// <param name="isMaxDepth">When this method returns, contains a value indicating whether the maximum depth was reached.</param>
        /// <returns>A disposable that restores the previous depth when disposed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDisposable? IncrementDepth(bool condition, out bool isMaxDepth)
        {
            if (condition)
            {
                return stringifyContext.IncrementDepth(out isMaxDepth);
            }
            else
            {
                isMaxDepth = false;
                return null;
            }
        }

        /// <summary>
        /// Creates a counter for collection items.
        /// </summary>
        /// <returns>The allotted counter.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AllottedCounter CountCollectionItems()
        {
            return AllottedCounter.Count(stringifyContext.VariableConfiguration.EffectiveMaxCollectionItemCount);
        }

        /// <summary>
        /// Creates a counter for dictionary items.
        /// </summary>
        /// <returns>The allotted counter.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AllottedCounter CountDictionaryItems()
        {
            return AllottedCounter.Count(stringifyContext.VariableConfiguration.EffectiveMaxDictionaryItemCount);
        }

        /// <summary>
        /// Creates a counter for memberwise properties.
        /// </summary>
        /// <returns>The allotted counter.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AllottedCounter CountMemberwiseProperties()
        {
            return AllottedCounter.Count(stringifyContext.VariableConfiguration.EffectiveMaxMemberwisePropertyCount);
        }
    }
}
