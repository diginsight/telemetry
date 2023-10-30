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

    private sealed class LogStringableJToken : ILogStringable, IJTokenVisitor<StringBuilder, (StringBuilder, AppendingContext)>
    {
        private readonly JToken root;

        public bool IsDeep => root is JObject or JArray or JConstructor or JRaw;

        public bool CanCycle => false;

        public LogStringableJToken(JToken root)
        {
            this.root = root;
        }

        public void AppendTo(StringBuilder stringBuilder, AppendingContext appendingContext)
        {
            root.Accept(this, (stringBuilder.Append(LogStringTokens.LiteralBegin), appendingContext)).Append(LogStringTokens.LiteralEnd);
        }

        public StringBuilder Visit(JArray jarray, (StringBuilder, AppendingContext) arg)
        {
            var (stringBuilder, appendingContext) = arg;

            using IDisposable? _0 = appendingContext.IncrementDepth(jarray != root, out bool isMaxDepth);
            if (isMaxDepth)
            {
                return stringBuilder.Append(LogStringTokens.Deep);
            }

            stringBuilder.Append('[');
            using (IEnumerator<JToken> enumerator = jarray.Children().GetEnumerator())
            {
                stringBuilder.AppendEnumerator(
                    enumerator,
                    e => { e.Current.Accept(this, (stringBuilder, appendingContext)); },
                    appendingContext.CountCollectionItems(),
                    appendingContext,
                    ","
                );
            }
            stringBuilder.Append(']');

            return stringBuilder;
        }

        public StringBuilder Visit(JConstructor jconstructor, (StringBuilder, AppendingContext) arg)
        {
            var (stringBuilder, appendingContext) = arg;

            using IDisposable? _0 = appendingContext.IncrementDepth(jconstructor != root, out bool isMaxDepth);
            if (isMaxDepth)
            {
                return stringBuilder.Append(LogStringTokens.Deep);
            }

            stringBuilder.Append($"new {jconstructor.Name}(");
            using (IEnumerator<JToken> enumerator = jconstructor.Children().GetEnumerator())
            {
                stringBuilder.AppendEnumerator(
                    enumerator,
                    e => { e.Current!.Accept(this, (stringBuilder, appendingContext)); },
                    appendingContext.CountCollectionItems(),
                    appendingContext,
                    ","
                );
            }
            stringBuilder.Append(')');

            return stringBuilder;
        }

        public StringBuilder Visit(JObject jobject, (StringBuilder, AppendingContext) arg)
        {
            var (stringBuilder, appendingContext) = arg;

            using IDisposable? _0 = appendingContext.IncrementDepth(jobject != root, out bool isMaxDepth);
            if (isMaxDepth)
            {
                return stringBuilder.Append(LogStringTokens.Deep);
            }

            stringBuilder.Append('{');
            using (IEnumerator<JProperty> enumerator = jobject.Properties().GetEnumerator())
            {
                stringBuilder.AppendEnumerator(
                    enumerator,
                    e => { e.Current!.Accept(this, (stringBuilder, appendingContext)); },
                    appendingContext.CountDictionaryItems(),
                    appendingContext,
                    ","
                );
            }
            stringBuilder.Append('}');

            return stringBuilder;
        }

        public StringBuilder Visit(JProperty jproperty, (StringBuilder, AppendingContext) arg)
        {
            var (stringBuilder, appendingContext) = arg;

            stringBuilder
                .Append(new JValue(jproperty.Name).ToString(Formatting.None))
                .Append(':');

            return jproperty.Value.Accept(this, (stringBuilder, appendingContext));
        }

        public StringBuilder Visit(JValue jvalue, (StringBuilder, AppendingContext) arg)
        {
            var (stringBuilder, _) = arg;
            return stringBuilder.Append(jvalue.ToString(Formatting.None));
        }
    }
}
