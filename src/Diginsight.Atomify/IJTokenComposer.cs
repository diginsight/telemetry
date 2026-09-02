#if EXPERIMENT_ATOMIFY
namespace Diginsight.Atomify;

/// <summary>
/// Represents an interface for composing JSON tokens.
/// </summary>
public interface IJTokenComposer : IJComposer
{
    /// <summary>
    /// Starts composing a JSON object token.
    /// </summary>
    /// <returns>The composer for the JSON object token.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the token composer has already been used.</exception>
    IJObjectComposer Object();

    /// <summary>
    /// Starts composing a JSON array token.
    /// </summary>
    /// <returns>The composer for the JSON array token.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the token composer has already been used.</exception>
    IJArrayComposer Array();

    /// <summary>
    /// Composes a JSON value token from the specified value.
    /// </summary>
    /// <param name="value">The value to compose.</param>
    /// <exception cref="InvalidOperationException">Thrown when the token composer has already been used.</exception>
    void Value(object value);
}
#endif
