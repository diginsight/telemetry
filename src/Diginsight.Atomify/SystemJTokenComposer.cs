#if EXPERIMENT_ATOMIFY
using System.Text.Json;

namespace Diginsight.Atomify;

public sealed class SystemJTokenComposer : JComposerBase, IJTokenComposer
{
    private readonly Utf8JsonWriter writer;

    public SystemJTokenComposer(Utf8JsonWriter writer)
    {
        this.writer = writer;
    }

    public IJObjectComposer Object()
    {
        SetUsed();
        return new SystemJObjectComposer(writer);
    }

    public IJArrayComposer Array()
    {
        SetUsed();
        return new SystemJArrayComposer(writer);
    }

    public void Value(object value)
    {
        SetUsed();
        JsonSerializer.Serialize(writer, value);
    }
}
#endif
