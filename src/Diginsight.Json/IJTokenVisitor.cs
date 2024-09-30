using Newtonsoft.Json.Linq;

namespace Diginsight.Json;

public interface IJTokenVisitor<out TResult, in TArg>
{
    TResult Visit(JArray jarray, TArg arg);

    TResult Visit(JConstructor jconstructor, TArg arg);

    TResult Visit(JObject jobject, TArg arg);

    TResult Visit(JProperty jproperty, TArg arg);

#if NET || NETSTANDARD2_1_OR_GREATER
    TResult Visit(JRaw jraw, TArg arg) => Visit((JValue)jraw, arg);
#endif

    TResult Visit(JValue jvalue, TArg arg);
}
