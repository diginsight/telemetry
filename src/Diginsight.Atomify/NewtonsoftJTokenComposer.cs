#if EXPERIMENT_ATOMIFY
using Newtonsoft.Json;

namespace Diginsight.Atomify;

/// <summary>
/// Represents a JSON token composer that emits JSON through a <see cref="JsonWriter" />.
/// </summary>
public sealed class NewtonsoftJTokenComposer : JComposerBase, IJTokenComposer
{
    private readonly JsonWriter writer;

    /// <summary>
    /// Initializes a new instance of the <see cref="NewtonsoftJTokenComposer" /> class with a specified JSON writer.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    public NewtonsoftJTokenComposer(JsonWriter writer)
    {
        this.writer = writer;
    }

    /// <inheritdoc />
    public IJObjectComposer Object()
    {
        SetUsed();
        return new NewtonsoftJObjectComposer(writer);
    }

    /// <inheritdoc />
    public IJArrayComposer Array()
    {
        SetUsed();
        return new NewtonsoftJArrayComposer(writer);
    }

    /// <inheritdoc />
    public void Value(object value)
    {
        SetUsed();
        writer.WriteValue(value);
    }
}
#endif
