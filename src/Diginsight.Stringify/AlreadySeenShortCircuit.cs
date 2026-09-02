namespace Diginsight.Stringify;

/// <summary>
/// Represents a short-circuit exception raised when stringification reaches an object that is already being rendered.
/// </summary>
public sealed class AlreadySeenShortCircuit : ShortCircuit
{
    /// <summary>
    /// Gets the subject being stringified.
    /// </summary>
    public object Subject { get; }
    /// <summary>
    /// Gets the difference between the current depth and the previous depth where the subject was seen.
    /// </summary>
    public int DepthDelta { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AlreadySeenShortCircuit" /> class.
    /// </summary>
    /// <param name="subject">The subject to stringify.</param>
    /// <param name="depthDelta">The depth delta.</param>
    public AlreadySeenShortCircuit(object subject, int depthDelta)
    {
        Subject = subject;
        DepthDelta = depthDelta;
    }
}
