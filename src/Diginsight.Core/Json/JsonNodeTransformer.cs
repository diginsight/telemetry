using System.Text.Json.Nodes;

namespace Diginsight.Json;

public abstract class JsonNodeTransformer<TArg> : IJsonNodeVisitor<(JsonNode jnode, bool changed), TArg>
#if NET9_0_OR_GREATER
    where TArg : allows ref struct
#endif
{
    public virtual (JsonNode jnode, bool changed) Visit(JsonArray jarray, TArg arg)
    {
        (IEnumerable<JsonNode?> jnodes, bool changed) = Visit(jarray.AsEnumerable(), arg);
        return changed ? (new JsonArray([ ..jnodes ]), true) : (jarray, false);
    }

    public virtual (JsonNode jnode, bool changed) Visit(JsonObject jobject, TArg arg)
    {
        (IEnumerable<KeyValuePair<string, JsonNode?>> jproperties, bool changed) = Visit(jobject.AsEnumerable(), arg);
        return changed ? (new JsonObject(jproperties), true) : (jobject, false);
    }

    public virtual (JsonNode jnode, bool changed) Visit(JsonValue jvalue, TArg arg)
    {
        return (jvalue, false);
    }

    public virtual (KeyValuePair<string, JsonNode?> jproperty, bool changed) Visit(KeyValuePair<string, JsonNode?> jproperty, TArg arg)
    {
        (JsonNode? output, bool changed) = jproperty.Value is { } input ? input.Accept(this, arg) : (null, false);
        return (KeyValuePair.Create(jproperty.Key, output), changed);
    }

    public virtual (IEnumerable<JsonNode?> jnodes, bool changed) Visit(IEnumerable<JsonNode?> jnodes, TArg arg)
    {
        ICollection<JsonNode?> outputs = [ ];
        bool anyChanged = false;
        foreach (JsonNode? input in jnodes)
        {
            if (input is null)
            {
                outputs.Add(null);
                continue;
            }

            (JsonNode output, bool changed) = input.Accept(this, arg);
            outputs.Add(output);
            if (!anyChanged)
                anyChanged = changed;
        }

        return (outputs, anyChanged);
    }

    public virtual (IEnumerable<KeyValuePair<string, JsonNode?>> jproperties, bool changed) Visit(
        IEnumerable<KeyValuePair<string, JsonNode?>> jproperties, TArg arg
    )
    {
        ICollection<KeyValuePair<string, JsonNode?>> outputs = [ ];
        bool anyChanged = false;
        foreach (KeyValuePair<string, JsonNode?> input in jproperties)
        {
            (KeyValuePair<string, JsonNode?> output, bool changed) = Visit(input, arg);
            outputs.Add(output);
            if (!anyChanged)
                anyChanged = changed;
        }

        return (outputs, anyChanged);
    }
}
