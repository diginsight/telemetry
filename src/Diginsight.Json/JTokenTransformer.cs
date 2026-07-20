using Newtonsoft.Json.Linq;

namespace Diginsight.Json;

public abstract class JTokenTransformer<TArg> : IJTokenVisitor<(JToken jtoken, bool changed), TArg>
#if NET9_0_OR_GREATER
    where TArg : allows ref struct
#endif
{
    public virtual (JToken jtoken, bool changed) Visit(JArray jarray, TArg arg)
    {
        (IEnumerable<JToken> jtokens, bool changed) = Visit(jarray.Children(), arg);
        return changed ? (new JArray(jtokens), true) : (jarray, false);
    }

    public virtual (JToken jtoken, bool changed) Visit(JConstructor jconstructor, TArg arg)
    {
        (IEnumerable<JToken> jtokens, bool changed) = Visit(jconstructor.Children(), arg);
        return changed ? (new JConstructor(jconstructor.Name!, jtokens), true) : (jconstructor, false);
    }

    public virtual (JToken jtoken, bool changed) Visit(JObject jobject, TArg arg)
    {
        (IEnumerable<JToken> jtokens, bool changed) = Visit(jobject.Children(), arg);
        return changed ? (new JObject(jtokens), true) : (jobject, false);
    }

    public virtual (JToken jtoken, bool changed) Visit(JProperty jproperty, TArg arg)
    {
        (JToken subJtoken, bool subChanged) = jproperty.Value.Accept(this, arg);
        return subChanged ? (new JProperty(jproperty.Name, subJtoken), true) : (jproperty, false);
    }

    public virtual (JToken jtoken, bool changed) Visit(JValue jvalue, TArg arg)
    {
        return (jvalue, false);
    }

    public virtual (IEnumerable<JToken> jtokens, bool changed) Visit(IEnumerable<JToken> jtokens, TArg arg)
    {
        ICollection<JToken> outputs = [ ];
        bool anyChanged = false;
        foreach (JToken input in jtokens)
        {
            (JToken output, bool changed) = input.Accept(this, arg);
            outputs.Add(output);
            if (!anyChanged)
                anyChanged = changed;
        }

        return (outputs, anyChanged);
    }
}
