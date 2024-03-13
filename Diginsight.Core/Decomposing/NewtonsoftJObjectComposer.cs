#if EXPERIMENT_DECOMPOSING
using Newtonsoft.Json;

namespace Diginsight.Decomposing;

public sealed class NewtonsoftJObjectComposer : JComposerBase, IJObjectComposer
{
    private readonly JsonWriter writer;

    public NewtonsoftJObjectComposer(JsonWriter writer)
    {
        this.writer = writer;
        writer.WriteStartObject();
    }

    public IJObjectComposer Property(string name, Action<IJTokenComposer> makeValue)
    {
        writer.WritePropertyName(name);

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
        writer.WriteEndObject();
    }
}
#endif
