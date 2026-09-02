#if EXPERIMENT_ATOMIFY
using System.Text.Json;

namespace Diginsight.Atomify;

/// <summary>
/// Represents a JSON object composer that emits JSON through a <see cref="Utf8JsonWriter" />.
/// </summary>
public sealed class SystemJObjectComposer : JComposerBase, IJObjectComposer
{
    private readonly Utf8JsonWriter writer;

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemJObjectComposer" /> class with a specified JSON writer.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    public SystemJObjectComposer(Utf8JsonWriter writer)
    {
        this.writer = writer;
        writer.WriteStartObject();
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public void End()
    {
        SetUsed();
        writer.WriteEndObject();
    }
}
#endif
