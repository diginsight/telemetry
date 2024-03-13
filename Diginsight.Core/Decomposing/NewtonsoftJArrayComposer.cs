#if EXPERIMENT_DECOMPOSING
using Newtonsoft.Json;

namespace Diginsight.Decomposing;

public sealed class NewtonsoftJArrayComposer : JComposerBase, IJArrayComposer
{
    private readonly JsonWriter writer;

    public NewtonsoftJArrayComposer(JsonWriter writer)
    {
        this.writer = writer;
        writer.WriteStartArray();
    }

    public IJArrayComposer Item(Action<IJTokenComposer> makeValue)
    {
        IJTokenComposer inner = new NewtonsoftJTokenComposer(writer);
        makeValue(inner);
        if (!inner.IsUsed)
        {
            throw new InvalidOperationException("Property composer not used");
        }

        return this;
    }

    public void End()
    {
        SetUsed();
        writer.WriteEndArray();
    }
}
#endif
