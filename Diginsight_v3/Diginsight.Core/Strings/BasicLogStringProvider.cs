using System.Text;
using System.Text.RegularExpressions;
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
using System.Runtime.CompilerServices;
#endif

namespace Diginsight.Strings;

internal sealed class BasicLogStringProvider : ILogStringProvider
{
    private readonly IMemberLogStringProvider memberLogStringProvider;

    public BasicLogStringProvider(
        IMemberLogStringProvider memberLogStringProvider
    )
    {
        this.memberLogStringProvider = memberLogStringProvider;
    }

    public ILogStringable? TryAsLogStringable(object obj)
    {
        switch (obj)
        {
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            case ITuple tuple:
                return new LogStringableTuple(tuple);
#endif

            case StringBuilder sb:
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                return new LogStringableStringBuilder(sb);
#else
                return new DirectLogStringable(sb);
#endif

            case Regex:
                return new DirectLogStringable(obj, "/{0}/");

            case Uri:
                return new DirectLogStringable(obj, LogStringTokens.LiteralBegin + "{0}" + LogStringTokens.LiteralEnd);

#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            case Index or Range:
                return new DirectLogStringable(obj);
#endif

            case DateTime or DateTimeOffset or TimeSpan
#if NET6_0_OR_GREATER
                or DateOnly or TimeOnly
#endif
                :
                return new DirectLogStringable(obj, LogStringTokens.LiteralBegin + "{0:O}" + LogStringTokens.LiteralEnd);

            case Guid:
                return new DirectLogStringable(obj, LogStringTokens.LiteralBegin + "{0:D}" + LogStringTokens.LiteralEnd);

            case Delegate del:
                return new LogStringableDelegate(del, this);
        }

        Type type = obj.GetType();
        if (type.IsKeyValuePair(out Type? tKey, out Type? tValue))
        {
            return (ILogStringable)typeof(LogStringableKeyValuePair<,>)
                .MakeGenericType(tKey, tValue)
                .GetConstructors()[0]
                .Invoke(new[] { obj });
        }

        return null;
    }

#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    private sealed class LogStringableTuple : ILogStringable
    {
        private readonly ITuple tuple;

#if !(NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
        public bool IsDeep => true;
#endif
        public bool CanCycle => false;

        public LogStringableTuple(ITuple tuple)
        {
            this.tuple = tuple;
        }

        public void AppendTo(StringBuilder stringBuilder, AppendingContext appendingContext)
        {
            stringBuilder.Append(LogStringTokens.TupleBegin);
            AllottingCounter counter = appendingContext.CountTupleItems();

            try
            {
                void AppendItem(int i)
                {
                    counter.Decrement();
                    stringBuilder.AppendLogString(tuple[i], appendingContext);
                }

                AppendItem(0);
                for (int i = 1; i < tuple.Length; i++)
                {
                    stringBuilder.Append(LogStringTokens.Separator2);
                    AppendItem(i);
                }
            }
            catch (MaxAllottedShortCircuit)
            {
                stringBuilder.Append(LogStringTokens.Ellipsis);
            }

            stringBuilder.Append(LogStringTokens.TupleEnd);
        }
    }

    private sealed class LogStringableStringBuilder : ILogStringable
    {
        private readonly StringBuilder sb;

        public bool IsDeep => false;
        public bool CanCycle => false;

        public LogStringableStringBuilder(StringBuilder sb)
        {
            this.sb = sb;
        }

        public void AppendTo(StringBuilder stringBuilder, AppendingContext appendingContext)
        {
            stringBuilder.Append(sb);
        }
    }
#endif

    private sealed class LogStringableDelegate : ILogStringable
    {
        private readonly Delegate del;
        private readonly BasicLogStringProvider owner;

#if !(NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
        public bool IsDeep => true;
        public bool CanCycle => true;
#endif

        public LogStringableDelegate(Delegate del, BasicLogStringProvider owner)
        {
            this.del = del;
            this.owner = owner;
        }

        public void AppendTo(StringBuilder stringBuilder, AppendingContext appendingContext)
        {
            stringBuilder.Append('λ');
            owner.memberLogStringProvider.Append(del.Method.GetParameters(), stringBuilder, appendingContext);
            stringBuilder.Append(':');
            owner.memberLogStringProvider.Append(del.Method.ReturnType, stringBuilder, appendingContext);
        }
    }

    private sealed class LogStringableKeyValuePair<TKey, TValue> : ILogStringable
    {
        private readonly KeyValuePair<TKey, TValue> kvp;

#if !(NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
        public bool IsDeep => true;
#endif
        public bool CanCycle => false;

        public LogStringableKeyValuePair(KeyValuePair<TKey, TValue> kvp)
        {
            this.kvp = kvp;
        }

        public void AppendTo(StringBuilder stringBuilder, AppendingContext appendingContext)
        {
            stringBuilder
                .AppendLogString(kvp.GetType(), appendingContext, false)
                .Append(LogStringTokens.MapBegin)
                .AppendLogString(kvp.Key, appendingContext)
                .Append(LogStringTokens.Value)
                .AppendLogString(kvp.Value, appendingContext)
                .Append(LogStringTokens.MapEnd);
        }
    }
}
