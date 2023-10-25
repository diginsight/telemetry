using System.Diagnostics.CodeAnalysis;
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

    public bool TryAsLogStringable(object obj, [NotNullWhen(true)] out ILogStringable? logStringable)
    {
        switch (obj)
        {
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            case ITuple tuple:
                logStringable = new LogStringableTuple(tuple);
                return true;
#endif

            case StringBuilder sb:
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                logStringable = new LogStringableStringBuilder(sb);
#else
                logStringable = new DirectLogStringable(sb);
#endif
                return true;

            case Regex:
                logStringable = new DirectLogStringable(obj, "/{0}/");
                return true;

            case Uri:
                logStringable = new DirectLogStringable(obj, LogStringTokens.LiteralBegin + "{0}" + LogStringTokens.LiteralEnd);
                return true;

#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            case Index or Range:
                logStringable = new DirectLogStringable(obj);
                return true;
#endif

            case DateTime or DateTimeOffset or TimeSpan
#if NET6_0_OR_GREATER
                or DateOnly or TimeOnly
#endif
                :
                logStringable = new DirectLogStringable(obj, LogStringTokens.LiteralBegin + "{0:O}" + LogStringTokens.LiteralEnd);
                return true;

            case Guid:
                logStringable = new DirectLogStringable(obj, LogStringTokens.LiteralBegin + "{0:D}" + LogStringTokens.LiteralEnd);
                return true;

            case Delegate del:
                logStringable = new LogStringableDelegate(del, this);
                return true;
        }

        Type type = obj.GetType();
        if (type.IsKeyValuePair(out Type? tKey, out Type? tValue))
        {
            logStringable = (ILogStringable)typeof(LogStringableKeyValuePair<,>)
                .MakeGenericType(tKey, tValue)
                .GetConstructors()[0]
                .Invoke(new[] { obj });
            return true;
        }

        logStringable = null;
        return false;
    }

#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    private sealed class LogStringableTuple : ILogStringable
    {
        private readonly ITuple tuple;

        public bool IsDeep => true;
        public bool CanCycle => false;

        public LogStringableTuple(ITuple tuple)
        {
            this.tuple = tuple;
        }

        public void AppendTo(StringBuilder stringBuilder, LoggingContext loggingContext)
        {
            stringBuilder.Append(LogStringTokens.TupleBegin);
            AllottingCounter counter = loggingContext.CountTupleItems();

            try
            {
                void AppendItem(int i)
                {
                    counter.Decrement();
                    stringBuilder.AppendLogString(tuple[i], loggingContext);
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

        public void AppendTo(StringBuilder stringBuilder, LoggingContext loggingContext)
        {
            stringBuilder.Append(sb);
        }
    }
#endif

    private sealed class LogStringableDelegate : ILogStringable
    {
        private readonly Delegate del;
        private readonly BasicLogStringProvider owner;

        public bool IsDeep => true;
        public bool CanCycle => true;

        public LogStringableDelegate(Delegate del, BasicLogStringProvider owner)
        {
            this.del = del;
            this.owner = owner;
        }

        public void AppendTo(StringBuilder stringBuilder, LoggingContext loggingContext)
        {
            stringBuilder.Append('λ');
            owner.memberLogStringProvider.Append(del.Method.GetParameters(), stringBuilder, loggingContext);
            stringBuilder.Append(':');
            owner.memberLogStringProvider.Append(del.Method.ReturnType, stringBuilder, loggingContext);
        }
    }

    private sealed class LogStringableKeyValuePair<TKey, TValue> : ILogStringable
    {
        private readonly KeyValuePair<TKey, TValue> kvp;

        public bool IsDeep => true;
        public bool CanCycle => false;

        public LogStringableKeyValuePair(KeyValuePair<TKey, TValue> kvp)
        {
            this.kvp = kvp;
        }

        public void AppendTo(StringBuilder stringBuilder, LoggingContext loggingContext)
        {
            stringBuilder
                .AppendLogString(kvp.GetType(), loggingContext, false)
                .Append(LogStringTokens.MapBegin)
                .AppendLogString(kvp.Key, loggingContext)
                .Append(LogStringTokens.Value)
                .AppendLogString(kvp.Value, loggingContext)
                .Append(LogStringTokens.MapEnd);
        }
    }
}
