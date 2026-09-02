#if EXPERIMENT_ATOMIFY
using System.Text.Json;

namespace Diginsight.Atomify;

/// <summary>
/// Represents a JSON array composer that emits JSON through a <see cref="Utf8JsonWriter" />.
/// </summary>
public sealed class SystemJArrayComposer : JComposerBase, IJArrayComposer
{
    private readonly Utf8JsonWriter writer;

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemJArrayComposer" /> class with a specified JSON writer.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    public SystemJArrayComposer(Utf8JsonWriter writer)
    {
        this.writer = writer;
        writer.WriteStartArray();
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public void End()
    {
        SetUsed();
        writer.WriteEndArray();
    }
}
#endif
