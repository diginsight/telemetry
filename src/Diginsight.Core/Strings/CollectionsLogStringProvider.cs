﻿using System.Collections;
using System.Diagnostics;

namespace Diginsight.Strings;

internal sealed class CollectionsLogStringProvider : ILogStringProvider
{
    public ILogStringable? TryToLogStringable(object obj)
    {
        if (obj is IDictionary dict)
        {
            return new LogStringableDictionary(dict);
        }

        Type type = obj.GetType();
        if (type.IsIEnumerableOfKeyValuePair(out Type? tKey, out Type? tValue))
        {
            return (ILogStringable)typeof(LogStringableKvpCollection<,>)
                .MakeGenericType(tKey, tValue)
                .GetConstructors()[0]
                .Invoke([ obj ]);
        }

        if (type.IsIEnumerable(out Type? tInner0))
        {
            return (ILogStringable)typeof(LogStringableGenericCollection<>)
                .MakeGenericType(tInner0)
                .GetConstructors()[0]
                .Invoke([ obj ]);
        }

        if (type.IsIAsyncEnumerable(out Type? tInner1))
        {
            return (ILogStringable)typeof(LogStringableGenericAsyncCollection<>)
                .MakeGenericType(tInner1)
                .GetConstructors()[0]
                .Invoke([ obj ]);
        }

        if (obj is IEnumerable coll)
        {
            return new LogStringableCollection(coll);
        }

        return null;
    }

    private abstract class LogStringableCollectionBase<T> : ILogStringable
        where T : notnull
    {
        protected readonly T subject;

#if !(NET || NETSTANDARD2_1_OR_GREATER)
        bool ILogStringable.IsDeep => true;
#endif
        object ILogStringable.Subject => subject;

        protected abstract char BeginDelim { get; }
        protected abstract char EndDelim { get; }

        protected LogStringableCollectionBase(T subject)
        {
            this.subject = subject;
        }

        public void AppendTo(AppendingContext appendingContext)
        {
            object? collectionLength = subject is Array ? GetLengths() : GetCount();
            appendingContext.ComposeAndAppendType(subject.GetType(), collectionLength);

            appendingContext.AppendDelimited(BeginDelim, EndDelim, AppendToCore);
        }

        protected abstract int? GetCount();

        protected abstract int[] GetLengths();

        protected abstract void AppendToCore(AppendingContext appendingContext);
    }

    private sealed class LogStringableDictionary : LogStringableCollectionBase<IDictionary>
    {
        protected override char BeginDelim => LogStringTokens.MapBegin;
        protected override char EndDelim => LogStringTokens.MapEnd;

        public LogStringableDictionary(IDictionary subject)
            : base(subject) { }

        protected override int? GetCount() => subject.Count;

        protected override int[] GetLengths() => throw new UnreachableException("Unexpected array");

        protected override void AppendToCore(AppendingContext appendingContext)
        {
            IDictionaryEnumerator enumerator = subject.GetEnumerator();

            try
            {
                appendingContext
                    .AppendEnumerator(
                        enumerator,
                        static (ac, e) =>
                        {
                            ac
                                .ComposeAndAppend(e.Key)
                                .AppendDirect(LogStringTokens.Value)
                                .ComposeAndAppend(e.Value);
                        },
                        appendingContext.CountDictionaryItems()
                    );
            }
            finally
            {
                (enumerator as IDisposable)?.Dispose();
            }
        }
    }

    private sealed class LogStringableKvpCollection<TKey, TValue> : LogStringableCollectionBase<IEnumerable<KeyValuePair<TKey, TValue>>>
    {
        protected override char BeginDelim => LogStringTokens.MapBegin;
        protected override char EndDelim => LogStringTokens.MapEnd;

        public LogStringableKvpCollection(IEnumerable<KeyValuePair<TKey, TValue>> subject)
            : base(subject) { }

        protected override int? GetCount()
        {
            return subject.TryGetNonEnumeratedCount(out int count) ? count : null;
        }

        protected override int[] GetLengths()
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            return [ ((Array)subject).Length ];
        }

        protected override void AppendToCore(AppendingContext appendingContext)
        {
            using IEnumerator<KeyValuePair<TKey, TValue>> enumerator = subject.GetEnumerator();

            appendingContext.AppendEnumerator(
                enumerator,
                static (ac, e) =>
                {
#if NET || NETSTANDARD2_1_OR_GREATER
                    (TKey key, TValue value) = e.Current;
#else
                    TKey key = e.Current.Key;
                    TValue value = e.Current.Value;
#endif
                    ac
                        .ComposeAndAppend(key)
                        .AppendDirect(LogStringTokens.Value)
                        .ComposeAndAppend(value);
                },
                appendingContext.CountDictionaryItems()
            );
        }
    }

    private sealed class LogStringableGenericCollection<T> : LogStringableCollectionBase<IEnumerable<T>>
    {
        protected override char BeginDelim => LogStringTokens.CollectionBegin;
        protected override char EndDelim => LogStringTokens.CollectionEnd;

        public LogStringableGenericCollection(IEnumerable<T> subject)
            : base(subject) { }

        protected override int? GetCount()
        {
            return subject.TryGetNonEnumeratedCount(out int count) ? count : null;
        }

        protected override int[] GetLengths()
        {
            return [ ((T[])subject).Length ];
        }

        protected override void AppendToCore(AppendingContext appendingContext)
        {
            using IEnumerator<T> enumerator = subject.GetEnumerator();

            appendingContext.AppendEnumerator(
                enumerator,
                static (ac, e) => { ac.ComposeAndAppend(e.Current); },
                appendingContext.CountCollectionItems()
            );
        }
    }

    private sealed class LogStringableGenericAsyncCollection<T> : ILogStringable
        where T : notnull
    {
        private readonly IAsyncEnumerable<T> subject;

#if !(NET || NETSTANDARD2_1_OR_GREATER)
        bool ILogStringable.IsDeep => true;
#endif
        object ILogStringable.Subject => subject;

        public LogStringableGenericAsyncCollection(IAsyncEnumerable<T> subject)
        {
            this.subject = subject;
        }

        public void AppendTo(AppendingContext appendingContext)
        {
            appendingContext.ComposeAndAppendType(subject.GetType());

            appendingContext.AppendDelimited(LogStringTokens.CollectionBegin, LogStringTokens.CollectionEnd, AppendToCore);
        }

        private void AppendToCore(AppendingContext appendingContext)
        {
            AppendToCoreAsync().GetAwaiter().GetResult();

            async Task AppendToCoreAsync()
            {
                await using IAsyncEnumerator<T> enumerator = subject.GetAsyncEnumerator();

                appendingContext.AppendEnumerator(
                    SyncWrap(enumerator),
                    static (ac, e) => { ac.ComposeAndAppend(e.Current); },
                    appendingContext.CountCollectionItems()
                );
            }
        }

        private static IEnumerator<T> SyncWrap(IAsyncEnumerator<T> enumerator)
        {
            while (enumerator.MoveNextAsync().GetAwaiter().GetResult())
            {
                yield return enumerator.Current;
            }
        }
    }

    private sealed class LogStringableCollection : LogStringableCollectionBase<IEnumerable>
    {
        protected override char BeginDelim => LogStringTokens.CollectionBegin;
        protected override char EndDelim => LogStringTokens.CollectionEnd;

        public LogStringableCollection(IEnumerable subject)
            : base(subject) { }

        protected override int? GetCount()
        {
            return subject is ICollection coll ? coll.Count : null;
        }

        protected override int[] GetLengths()
        {
            Array array = (Array)subject;
            return Enumerable.Range(0, array.Rank).Select(array.GetLength).ToArray();
        }

        protected override void AppendToCore(AppendingContext appendingContext)
        {
            IEnumerator enumerator = subject.GetEnumerator();

            try
            {
                appendingContext.AppendEnumerator(
                    enumerator,
                    static (ac, e) => { ac.ComposeAndAppend(e.Current); },
                    appendingContext.CountCollectionItems()
                );
            }
            finally
            {
                (enumerator as IDisposable)?.Dispose();
            }
        }
    }
}
