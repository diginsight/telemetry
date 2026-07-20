using System.Text.Json.Nodes;

namespace Diginsight.Json;

public interface IJsonNodeVisitor<out TResult, in TArg>
#if NET9_0_OR_GREATER
    where TResult : allows ref struct
    where TArg : allows ref struct
#endif
{
    TResult Visit(JsonArray jarray, TArg arg);

    TResult Visit(JsonObject jobject, TArg arg);

    TResult Visit(JsonValue jvalue, TArg arg);
}
