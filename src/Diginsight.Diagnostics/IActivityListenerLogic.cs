using System.Diagnostics;

namespace Diginsight.Diagnostics;

/// <summary>
/// Represents an interface for activity listener logic.
/// </summary>
/// <remarks>
/// The members of this interface mirror some callback properties of the standard <see cref="ActivityListener" /> class.
/// This allows the listener logic to be expressed as a single object whose methods can be wired to an <see cref="ActivityListener" /> instance.
/// </remarks>
public interface IActivityListenerLogic
{
    /// <summary>
    /// Handles activity start notifications.
    /// </summary>
    /// <param name="activity">The started activity.</param>
    /// <seealso cref="ActivityListener.ActivityStarted" />
    void ActivityStarted(Activity activity)
#if NET || NETSTANDARD2_1_OR_GREATER
    { }
#else
        ;
#endif

    /// <summary>
    /// Handles activity stop notifications.
    /// </summary>
    /// <param name="activity">The stopped activity.</param>
    /// <seealso cref="ActivityListener.ActivityStopped" />
    void ActivityStopped(Activity activity)
#if NET || NETSTANDARD2_1_OR_GREATER
    { }
#else
        ;
#endif

    /// <summary>
    /// Samples an activity creation request.
    /// </summary>
    /// <param name="creationOptions">The activity creation options.</param>
    /// <returns>The activity sampling result.</returns>
    /// <seealso cref="ActivityListener.Sample" />
    ActivitySamplingResult Sample(ref ActivityCreationOptions<ActivityContext> creationOptions)
#if NET || NETSTANDARD2_1_OR_GREATER
        => ActivitySamplingResult.PropagationData;
#else
        ;
#endif
}
