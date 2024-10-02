#if EXPERIMENT_ATOMIFY
using System.Text.Json;

namespace Diginsight.Atomify;

public sealed class SystemJArrayComposer : JComposerBase, IJArrayComposer
{
    private readonly Utf8JsonWriter writer;

    public SystemJArrayComposer(Utf8JsonWriter writer)
    {
        this.writer = writer;
        writer.WriteStartArray();
    }

    public IJArrayComposer Item(Action<IJTokenComposer> makeValue)
    {
        IJTokenComposer inner = new SystemJTokenComposer(writer);
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
