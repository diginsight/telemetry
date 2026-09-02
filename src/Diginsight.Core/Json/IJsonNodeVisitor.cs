using System.Text.Json.Nodes;

namespace Diginsight.Json;

/// <summary>
/// Represents a visitor that processes <see cref="JsonNode" /> instances and produces a result.
/// </summary>
/// <typeparam name="TResult">The type of the result produced by the visit.</typeparam>
/// <typeparam name="TArg">The type of the argument passed to the visit.</typeparam>
public interface IJsonNodeVisitor<out TResult, in TArg>
#if NET9_0_OR_GREATER
    where TResult : allows ref struct
    where TArg : allows ref struct
#endif
{
    /// <summary>
    /// Visits the specified JSON array.
    /// </summary>
    /// <param name="jarray">The JSON array to visit.</param>
    /// <param name="arg">The argument passed to the visit.</param>
    /// <returns>The result of visiting the array.</returns>
    TResult Visit(JsonArray jarray, TArg arg);

    /// <summary>
    /// Visits the specified JSON object.
    /// </summary>
    /// <param name="jobject">The JSON object to visit.</param>
    /// <param name="arg">The argument passed to the visit.</param>
    /// <returns>The result of visiting the object.</returns>
    TResult Visit(JsonObject jobject, TArg arg);

    /// <summary>
    /// Visits the specified JSON value.
    /// </summary>
    /// <param name="jvalue">The JSON value to visit.</param>
    /// <param name="arg">The argument passed to the visit.</param>
    /// <returns>The result of visiting the value.</returns>
    TResult Visit(JsonValue jvalue, TArg arg);
}
