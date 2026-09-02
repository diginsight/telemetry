#if EXPERIMENT_ATOMIFY
using System.Text.Json;

namespace Diginsight.Atomify;

/// <summary>
/// Represents a JSON token composer that emits JSON through a <see cref="Utf8JsonWriter" />.
/// </summary>
public sealed class SystemJTokenComposer : JComposerBase, IJTokenComposer
{
    private readonly Utf8JsonWriter writer;

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemJTokenComposer" /> class with a specified JSON writer.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    public SystemJTokenComposer(Utf8JsonWriter writer)
    {
        this.writer = writer;
    }

    /// <inheritdoc />
    public IJObjectComposer Object()
    {
        SetUsed();
        return new SystemJObjectComposer(writer);
    }

    /// <inheritdoc />
    public IJArrayComposer Array()
    {
        SetUsed();
        return new SystemJArrayComposer(writer);
    }

    /// <inheritdoc />
    public void Value(object value)
    {
        SetUsed();
        JsonSerializer.Serialize(writer, value);
    }
}
#endif
