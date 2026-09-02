#if EXPERIMENT_ATOMIFY
using Newtonsoft.Json;

namespace Diginsight.Atomify;

/// <summary>
/// Represents a JSON object composer that emits JSON through a <see cref="JsonWriter" />.
/// </summary>
public sealed class NewtonsoftJObjectComposer : JComposerBase, IJObjectComposer
{
    private readonly JsonWriter writer;

    /// <summary>
    /// Initializes a new instance of the <see cref="NewtonsoftJObjectComposer" /> class with a specified JSON writer.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    public NewtonsoftJObjectComposer(JsonWriter writer)
    {
        this.writer = writer;
        writer.WriteStartObject();
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public void End()
    {
        SetUsed();
        writer.WriteEndObject();
    }
}
#endif
