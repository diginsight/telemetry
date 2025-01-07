using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Diginsight.Json;

internal sealed class JTokenNormalizer : JTokenTransformer<ValueTuple>
{
    public static readonly JTokenNormalizer Instance = new ();

    private static JToken ToFlat(JValue jvalue) => JsonConvert.DeserializeObject<string>(jvalue.ToString(Formatting.None));

    private JTokenNormalizer() { }

    public override (JToken jtoken, bool changed) Visit(JValue jvalue, ValueTuple arg)
    {
        switch (jvalue.Type)
        {
            case JTokenType.Bytes:
            case JTokenType.Date:
            case JTokenType.Guid:
            case JTokenType.TimeSpan:
            case JTokenType.Uri:
            {
                return (ToFlat(jvalue), true);
            }

            case JTokenType.Float:
            {
                if (
                    jvalue.Value is double dbl && (double.IsInfinity(dbl) || double.IsNaN(dbl))
                    ||
                    jvalue.Value is float flt && (float.IsInfinity(flt) || float.IsNaN(flt))
                )
                {
                    return (ToFlat(jvalue), true);
                }

                goto default;
            }

            case JTokenType.String:
            {
                if (jvalue.Value == null)
                {
                    return (JValue.CreateNull(), true);
                }

                goto default;
            }

            default:
            {
                return (jvalue, false);
            }
        }
    }
}
