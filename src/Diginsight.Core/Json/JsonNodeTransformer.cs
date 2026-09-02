using System.Text.Json.Nodes;

namespace Diginsight.Json;

/// <summary>
/// Represents a base <see cref="IJsonNodeVisitor{TResult, TArg}" /> that transforms a JSON node tree, reporting whether any node was changed.
/// </summary>
/// <typeparam name="TArg">The type of the argument passed to the transformation.</typeparam>
/// <remarks>
/// Each virtual method returns the (possibly new) node together with a flag indicating whether the transformation produced a change.
/// When nothing changes, the original instance is returned unchanged to avoid unnecessary allocations.
/// </remarks>
public abstract class JsonNodeTransformer<TArg> : IJsonNodeVisitor<(JsonNode jnode, bool changed), TArg>
#if NET9_0_OR_GREATER
    where TArg : allows ref struct
#endif
{
    /// <inheritdoc />
    public virtual (JsonNode jnode, bool changed) Visit(JsonArray jarray, TArg arg)
    {
        (IEnumerable<JsonNode?> jnodes, bool changed) = Visit(jarray.AsEnumerable(), arg);
        return changed ? (new JsonArray([ ..jnodes ]), true) : (jarray, false);
    }

    /// <inheritdoc />
    public virtual (JsonNode jnode, bool changed) Visit(JsonObject jobject, TArg arg)
    {
        (IEnumerable<KeyValuePair<string, JsonNode?>> jproperties, bool changed) = Visit(jobject.AsEnumerable(), arg);
        return changed ? (new JsonObject(jproperties), true) : (jobject, false);
    }

    /// <inheritdoc />
    public virtual (JsonNode jnode, bool changed) Visit(JsonValue jvalue, TArg arg)
    {
        return (jvalue, false);
    }

    /// <summary>
    /// Visits the specified JSON property, transforming its value.
    /// </summary>
    /// <param name="jproperty">The JSON property to visit.</param>
    /// <param name="arg">The argument passed to the transformation.</param>
    /// <returns>The transformed property and a value indicating whether it was changed.</returns>
    public virtual (KeyValuePair<string, JsonNode?> jproperty, bool changed) Visit(KeyValuePair<string, JsonNode?> jproperty, TArg arg)
    {
        (JsonNode? output, bool changed) = jproperty.Value is { } input ? input.Accept(this, arg) : (null, false);
        return (KeyValuePair.Create(jproperty.Key, output), changed);
    }

    /// <summary>
    /// Visits the specified sequence of JSON nodes, transforming each element.
    /// </summary>
    /// <param name="jnodes">The JSON nodes to visit.</param>
    /// <param name="arg">The argument passed to the transformation.</param>
    /// <returns>The transformed nodes and a value indicating whether any of them was changed.</returns>
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

    /// <summary>
    /// Visits the specified sequence of JSON properties, transforming each element.
    /// </summary>
    /// <param name="jproperties">The JSON properties to visit.</param>
    /// <param name="arg">The argument passed to the transformation.</param>
    /// <returns>The transformed properties and a value indicating whether any of them was changed.</returns>
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
