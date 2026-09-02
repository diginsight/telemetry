using Newtonsoft.Json.Linq;

namespace Diginsight.Json;

/// <summary>
/// Represents an interface for visiting JSON tokens.
/// </summary>
/// <typeparam name="TResult">The type of the visitor result.</typeparam>
/// <typeparam name="TArg">The type of the visitor argument.</typeparam>
public interface IJTokenVisitor<out TResult, in TArg>
#if NET9_0_OR_GREATER
    where TResult : allows ref struct
    where TArg : allows ref struct
#endif
{
    /// <summary>
    /// Visits a JSON array.
    /// </summary>
    /// <param name="jarray">The JSON array.</param>
    /// <param name="arg">The visitor argument.</param>
    /// <returns>The visitor result.</returns>
    TResult Visit(JArray jarray, TArg arg);

    /// <summary>
    /// Visits a JSON constructor.
    /// </summary>
    /// <param name="jconstructor">The JSON constructor.</param>
    /// <param name="arg">The visitor argument.</param>
    /// <returns>The visitor result.</returns>
    TResult Visit(JConstructor jconstructor, TArg arg);

    /// <summary>
    /// Visits a JSON object.
    /// </summary>
    /// <param name="jobject">The JSON object.</param>
    /// <param name="arg">The visitor argument.</param>
    /// <returns>The visitor result.</returns>
    TResult Visit(JObject jobject, TArg arg);

    /// <summary>
    /// Visits a JSON property.
    /// </summary>
    /// <param name="jproperty">The JSON property.</param>
    /// <param name="arg">The visitor argument.</param>
    /// <returns>The visitor result.</returns>
    TResult Visit(JProperty jproperty, TArg arg);

#if NET || NETSTANDARD2_1_OR_GREATER
    /// <summary>
    /// Visits a raw JSON value.
    /// </summary>
    /// <param name="jraw">The raw JSON value.</param>
    /// <param name="arg">The visitor argument.</param>
    /// <returns>The visitor result.</returns>
    TResult Visit(JRaw jraw, TArg arg) => Visit((JValue)jraw, arg);
#endif

    /// <summary>
    /// Visits a JSON value.
    /// </summary>
    /// <param name="jvalue">The JSON value.</param>
    /// <param name="arg">The visitor argument.</param>
    /// <returns>The visitor result.</returns>
    TResult Visit(JValue jvalue, TArg arg);
}
