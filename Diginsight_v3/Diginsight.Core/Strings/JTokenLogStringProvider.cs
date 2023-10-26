using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace Diginsight.Strings;

internal sealed class JTokenLogStringProvider : ILogStringProvider
{
    public ILogStringable? TryAsLogStringable(object obj)
    {
        return obj is JToken jt ? new LogStringableJToken(jt) : null;
    }

    private sealed class LogStringableJToken : ILogStringable, IJTokenVisitor<StringBuilder, (StringBuilder, LoggingContext)>
    {
        private readonly JToken root;

        public bool IsDeep => root is JObject or JArray or JConstructor or JRaw;

        public bool CanCycle => false;

        public LogStringableJToken(JToken root)
        {
            this.root = root;
        }

        public void AppendTo(StringBuilder stringBuilder, LoggingContext loggingContext)
        {
            root.Accept(this, (stringBuilder.Append(LogStringTokens.LiteralBegin), loggingContext)).Append(LogStringTokens.LiteralEnd);
        }

        public StringBuilder Visit(JArray jarray, (StringBuilder, LoggingContext) arg)
        {
            var (stringBuilder, loggingContext) = arg;

            using IDisposable? _0 = loggingContext.IncrementDepth(jarray != root, out bool isMaxDepth);
            if (isMaxDepth)
            {
                return stringBuilder.Append(LogStringTokens.Deep);
            }

            stringBuilder.Append('[');
            VisitCore();
            stringBuilder.Append(']');

            return stringBuilder;

            void VisitCore()
            {
                using IEnumerator<JToken> enumerator = jarray.Children().GetEnumerator();
                if (!enumerator.MoveNext())
                    return;

                AllottingCounter counter = loggingContext.CountCollectionItems();
                try
                {
                    void AppendItem()
                    {
                        counter.Decrement();
                        enumerator.Current!.Accept(this, (stringBuilder, loggingContext));
                    }

                    AppendItem();
                    while (enumerator.MoveNext())
                    {
                        stringBuilder.Append(',');
                        AppendItem();
                    }
                }
                catch (MaxAllottedShortCircuit)
                {
                    stringBuilder.Append(LogStringTokens.Ellipsis);
                }
            }
        }

        public StringBuilder Visit(JConstructor jconstructor, (StringBuilder, LoggingContext) arg)
        {
            var (stringBuilder, loggingContext) = arg;

            using IDisposable? _0 = loggingContext.IncrementDepth(jconstructor != root, out bool isMaxDepth);
            if (isMaxDepth)
            {
                return stringBuilder.Append(LogStringTokens.Deep);
            }

            stringBuilder.Append($"new {jconstructor.Name}(");
            VisitCore();
            stringBuilder.Append(')');

            return stringBuilder;

            void VisitCore()
            {
                using IEnumerator<JToken> enumerator = jconstructor.Children().GetEnumerator();
                if (!enumerator.MoveNext())
                    return;

                AllottingCounter counter = loggingContext.CountCollectionItems();
                try
                {
                    void AppendItem()
                    {
                        counter.Decrement();
                        enumerator.Current!.Accept(this, (stringBuilder, loggingContext));
                    }

                    AppendItem();
                    while (enumerator.MoveNext())
                    {
                        stringBuilder.Append(',');
                        AppendItem();
                    }
                }
                catch (MaxAllottedShortCircuit)
                {
                    stringBuilder.Append(LogStringTokens.Ellipsis);
                }
            }
        }

        public StringBuilder Visit(JObject jobject, (StringBuilder, LoggingContext) arg)
        {
            var (stringBuilder, loggingContext) = arg;

            using IDisposable? _0 = loggingContext.IncrementDepth(jobject != root, out bool isMaxDepth);
            if (isMaxDepth)
            {
                return stringBuilder.Append(LogStringTokens.Deep);
            }

            stringBuilder.Append('{');
            VisitCore();
            stringBuilder.Append('}');

            return stringBuilder;

            void VisitCore()
            {
                using IEnumerator<JProperty> enumerator = jobject.Properties().GetEnumerator();
                if (!enumerator.MoveNext())
                    return;

                AllottingCounter counter = loggingContext.CountDictionaryItems();
                try
                {
                    void AppendItem()
                    {
                        counter.Decrement();
                        enumerator.Current!.Accept(this, (stringBuilder, loggingContext));
                    }

                    AppendItem();
                    while (enumerator.MoveNext())
                    {
                        stringBuilder.Append(',');
                        AppendItem();
                    }
                }
                catch (MaxAllottedShortCircuit)
                {
                    stringBuilder.Append(LogStringTokens.Ellipsis);
                }
            }
        }

        public StringBuilder Visit(JProperty jproperty, (StringBuilder, LoggingContext) arg)
        {
            var (stringBuilder, loggingContext) = arg;

            stringBuilder
                .Append(new JValue(jproperty.Name).ToString(Formatting.None))
                .Append(':');

            return jproperty.Value.Accept(this, (stringBuilder, loggingContext));
        }

        public StringBuilder Visit(JValue jvalue, (StringBuilder, LoggingContext) arg)
        {
            var (stringBuilder, _) = arg;
            return stringBuilder.Append(jvalue.ToString(Formatting.None));
        }
    }
}
