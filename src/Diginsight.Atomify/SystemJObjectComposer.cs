#if EXPERIMENT_ATOMIFY
using System.Text.Json;

namespace Diginsight.Atomify;

public sealed class SystemJObjectComposer : JComposerBase, IJObjectComposer
{
    private readonly Utf8JsonWriter writer;

    public SystemJObjectComposer(Utf8JsonWriter writer)
    {
        this.writer = writer;
        writer.WriteStartObject();
    }

    public IJObjectComposer Property(string name, Action<IJTokenComposer> makeValue)
    {
        writer.WritePropertyName(name);

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
        writer.WriteEndObject();
    }
}
#endif
