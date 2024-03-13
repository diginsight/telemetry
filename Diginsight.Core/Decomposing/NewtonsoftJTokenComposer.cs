#if EXPERIMENT_DECOMPOSING
using Newtonsoft.Json;

namespace Diginsight.Decomposing;

public sealed class NewtonsoftJTokenComposer : JComposerBase, IJTokenComposer
{
    private readonly JsonWriter writer;

    public NewtonsoftJTokenComposer(JsonWriter writer)
    {
        this.writer = writer;
    }

    public IJObjectComposer Object()
    {
        SetUsed();
        return new NewtonsoftJObjectComposer(writer);
    }

    public IJArrayComposer Array()
    {
        SetUsed();
        return new NewtonsoftJArrayComposer(writer);
    }

    public void Value(object value)
    {
        SetUsed();
        writer.WriteValue(value);
    }
}
#endif
