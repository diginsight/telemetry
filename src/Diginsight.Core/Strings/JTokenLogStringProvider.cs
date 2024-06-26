﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Diginsight.Strings;

internal sealed class JTokenLogStringProvider : ILogStringProvider
{
    public ILogStringable? TryToLogStringable(object obj)
    {
        return obj is JToken jt ? new LogStringableJToken(jt) : null;
    }

    private sealed class LogStringableJToken : ILogStringable, IJTokenVisitor<AppendingContext, AppendingContext>
    {
        private readonly JToken root;

        bool ILogStringable.IsDeep => root is JObject or JArray or JConstructor or JRaw;
        object? ILogStringable.Subject => null;

        public LogStringableJToken(JToken root)
        {
            this.root = root;
        }

        public void AppendTo(AppendingContext appendingContext)
        {
            appendingContext.AppendDelimited(
                LogStringTokens.LiteralBegin,
                LogStringTokens.LiteralEnd,
                ac => { root.Accept(this, ac); }
            );
        }

        public AppendingContext Visit(JArray jarray, AppendingContext appendingContext)
        {
            appendingContext.AppendDirect('[');
            using (IEnumerator<JToken> enumerator = jarray.Children().GetEnumerator())
            {
                appendingContext.AppendEnumerator(
                    enumerator,
                    (ac, e) => { e.Current.Accept(this, ac); },
                    appendingContext.CountCollectionItems(),
                    ","
                );
            }
            appendingContext.AppendDirect(']');

            return appendingContext;
        }

        public AppendingContext Visit(JConstructor jconstructor, AppendingContext appendingContext)
        {
            appendingContext.AppendDirect($"new {jconstructor.Name}(");
            using (IEnumerator<JToken> enumerator = jconstructor.Children().GetEnumerator())
            {
                appendingContext.AppendEnumerator(
                    enumerator,
                    (ac, e) => { e.Current.Accept(this, ac); },
                    appendingContext.CountCollectionItems(),
                    ","
                );
            }
            appendingContext.AppendDirect(')');

            return appendingContext;
        }

        public AppendingContext Visit(JObject jobject, AppendingContext appendingContext)
        {
            appendingContext.AppendDirect('{');
            using (IEnumerator<JProperty> enumerator = jobject.Properties().GetEnumerator())
            {
                appendingContext.AppendEnumerator(
                    enumerator,
                    (ac, e) => { e.Current.Accept(this, ac); },
                    appendingContext.CountDictionaryItems(),
                    ","
                );
            }
            appendingContext.AppendDirect('}');

            return appendingContext;
        }

        public AppendingContext Visit(JProperty jproperty, AppendingContext appendingContext)
        {
            appendingContext
                .AppendDirect(new JValue(jproperty.Name).ToString(Formatting.None))
                .AppendDirect(':');

            return jproperty.Value.Accept(this, appendingContext);
        }

        public AppendingContext Visit(JValue jvalue, AppendingContext appendingContext)
        {
            return appendingContext.AppendDirect(jvalue.ToString(Formatting.None));
        }
    }
}
