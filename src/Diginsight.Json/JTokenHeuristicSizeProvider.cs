using Diginsight.Runtime;
using Newtonsoft.Json.Linq;

namespace Diginsight.Json;

public sealed class JTokenHeuristicSizeProvider : IHeuristicSizeProvider
{
    public static readonly IHeuristicSizeProvider Instance = new JTokenHeuristicSizeProvider();

    private JTokenHeuristicSizeProvider() { }

    public bool TryGetSizeHeuristically(object obj, HeuristicSizeGetter innerGet, out HeuristicSizeResult result)
    {
        switch (obj)
        {
            case JValue jv:
                result = ~innerGet(jv.Value);
                return true;

            case JArray ja:
                result = ~innerGet((IReadOnlyCollection<JToken>)[ ..ja.Children() ]);
                return true;

            case JObject jo:
                result = ~innerGet((IReadOnlyCollection<JProperty>)[ ..jo.Properties() ]);
                return true;

            case JProperty jp:
                result = ~(innerGet(jp.Name) + innerGet(jp.Value));
                return true;

            case JConstructor jc:
                result = ~(innerGet(jc.Name) + innerGet((IReadOnlyCollection<JToken>)[ ..jc.Children() ]));
                return true;

            default:
                result = default;
                return false;
        }
    }
}
