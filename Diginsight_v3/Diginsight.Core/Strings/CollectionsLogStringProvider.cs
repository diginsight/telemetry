using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Diginsight.Strings;

internal sealed class CollectionsLogStringProvider : ILogStringProvider
{
    public bool TryAsLoggable(object obj, [NotNullWhen(true)] out ILoggable? loggable)
    {
        if (obj is IDictionary dict)
        {
            loggable = new LoggableDictionary(dict);
            return true;
        }

        if (IsIEnumerableOfKeyValuePair(obj.GetType(), out Type? tKey, out Type? tValue))
        {
            loggable = (ILoggable)typeof(LoggableKVPCollection<,>)
                .MakeGenericType(tKey, tValue)
                .GetConstructors()[0]
                .Invoke(new[] { obj });
            return true;
        }

        if (obj is IEnumerable coll)
        {
            loggable = new LoggableCollection(coll);
            return true;
        }

        loggable = null;
        return false;
    }

    private static bool IsIEnumerableOfKeyValuePair(Type type, [NotNullWhen(true)] out Type? tKey, [NotNullWhen(true)] out Type? tValue)
    {
        foreach (Type interfaceType in type.GetInterfaces())
        {
            if (!interfaceType.IsGenericType || interfaceType.GetGenericTypeDefinition() != typeof(IEnumerable<>))
                continue;
            Type innerType = interfaceType.GetGenericArguments()[0];
            if (!innerType.IsKeyValuePair(out tKey, out tValue))
                continue;

            return true;
        }

        tKey = null;
        tValue = null;
        return false;
    }

    private abstract class LoggableCollectionBase<T> : ILoggable
        where T : notnull
    {
        protected readonly T subject;

        public bool IsDeep => true;
        public bool CanCycle => true;

        protected abstract char BeginToken { get; }
        protected abstract char EndToken { get; }

        protected LoggableCollectionBase(T subject)
        {
            this.subject = subject;
        }

        public void AppendTo(StringBuilder stringBuilder, LoggingContext loggingContext)
        {
            try
            {
                loggingContext.Append(subject.GetType(), stringBuilder, false);

                //using IDisposable? _0 = loggingContext.AddSeen(subject);

                stringBuilder.Append(BeginToken);
                AppendToCore(stringBuilder, loggingContext);
                stringBuilder.Append(EndToken);
            }
            catch (AlreadySeenShortCircuit)
            {
                stringBuilder.Append(LogStringTokens.Cycle);
            }
        }

        protected abstract void AppendToCore(StringBuilder stringBuilder, LoggingContext loggingContext);
    }

    private sealed class LoggableDictionary : LoggableCollectionBase<IDictionary>
    {
        protected override char BeginToken => LogStringTokens.MapBegin;
        protected override char EndToken => LogStringTokens.MapEnd;

        public LoggableDictionary(IDictionary subject)
            : base(subject) { }

        protected override void AppendToCore(StringBuilder stringBuilder, LoggingContext loggingContext)
        {
            IDictionaryEnumerator enumerator = subject.GetEnumerator();
            if (!enumerator.MoveNext())
                return;

            AllottingCounter counter = loggingContext.CountDictionaryItems();
            try
            {
                void AppendEntry()
                {
                    counter.Decrement();
                    stringBuilder
                        .AppendLogString(enumerator.Key, loggingContext)
                        .Append(LogStringTokens.Value)
                        .AppendLogString(enumerator.Value, loggingContext);
                }

                AppendEntry();
                while (enumerator.MoveNext())
                {
                    stringBuilder.Append(LogStringTokens.Separator2);
                    AppendEntry();
                }
            }
            catch (MaxAllottedShortCircuit)
            {
                stringBuilder.Append(LogStringTokens.Ellipsis);
            }
            finally
            {
                (enumerator as IDisposable)?.Dispose();
            }
        }
    }

    private sealed class LoggableKVPCollection<TKey, TValue> : LoggableCollectionBase<IEnumerable<KeyValuePair<TKey, TValue>>>
    {
        protected override char BeginToken => LogStringTokens.MapBegin;
        protected override char EndToken => LogStringTokens.MapEnd;

        public LoggableKVPCollection(IEnumerable<KeyValuePair<TKey, TValue>> subject)
            : base(subject) { }

        protected override void AppendToCore(StringBuilder stringBuilder, LoggingContext loggingContext)
        {
            using IEnumerator<KeyValuePair<TKey, TValue>> enumerator = subject.GetEnumerator();
            if (!enumerator.MoveNext())
                return;

            AllottingCounter counter = loggingContext.CountDictionaryItems();
            try
            {
                void AppendEntry()
                {
                    counter.Decrement();
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                    (TKey key, TValue value) = enumerator.Current;
#else
                    TKey key = enumerator.Current.Key;
                    TValue value = enumerator.Current.Value;
#endif
                    stringBuilder
                        .AppendLogString(key, loggingContext)
                        .Append(LogStringTokens.Value)
                        .AppendLogString(value, loggingContext);
                }

                AppendEntry();
                while (enumerator.MoveNext())
                {
                    stringBuilder.Append(LogStringTokens.Separator2);
                    AppendEntry();
                }
            }
            catch (MaxAllottedShortCircuit)
            {
                stringBuilder.Append(LogStringTokens.Ellipsis);
            }
        }
    }

    private sealed class LoggableCollection : LoggableCollectionBase<IEnumerable>
    {
        protected override char BeginToken => '[';
        protected override char EndToken => ']';

        public LoggableCollection(IEnumerable subject)
            : base(subject) { }

        protected override void AppendToCore(StringBuilder stringBuilder, LoggingContext loggingContext)
        {
            IEnumerator enumerator = subject.GetEnumerator();
            if (!enumerator.MoveNext())
                return;

            AllottingCounter counter = loggingContext.CountCollectionItems();
            try
            {
                void AppendItem()
                {
                    counter.Decrement();
                    stringBuilder.AppendLogString(enumerator.Current, loggingContext);
                }

                AppendItem();
                while (enumerator.MoveNext())
                {
                    stringBuilder.Append(LogStringTokens.Separator2);
                    AppendItem();
                }
            }
            catch (MaxAllottedShortCircuit)
            {
                stringBuilder.Append(LogStringTokens.Ellipsis);
            }
            finally
            {
                (enumerator as IDisposable)?.Dispose();
            }
        }
    }
}
