#if EXPERIMENT_ATOMIFY
using Newtonsoft.Json;

namespace Diginsight.Atomify;

/// <summary>
/// Represents a JSON array composer that emits JSON through a <see cref="JsonWriter" />.
/// </summary>
public sealed class NewtonsoftJArrayComposer : JComposerBase, IJArrayComposer
{
    private readonly JsonWriter writer;

    /// <summary>
    /// Initializes a new instance of the <see cref="NewtonsoftJArrayComposer" /> class with a specified JSON writer.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    public NewtonsoftJArrayComposer(JsonWriter writer)
    {
        this.writer = writer;
        writer.WriteStartArray();
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public void End()
    {
        SetUsed();
        writer.WriteEndArray();
    }
}
#endif
