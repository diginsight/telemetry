using System.Diagnostics;

namespace Diginsight.Diagnostics;

/// <summary>
/// Represents an interface for activity listener registration.
/// </summary>
public interface IActivityListenerRegistration
{
    /// <summary>
    /// Gets the activity listener logic.
    /// </summary>
    IActivityListenerLogic Logic { get; }

    /// <summary>
    /// Determines whether the listener should listen to the specified activity source.
    /// </summary>
    /// <param name="activitySource">The activity source.</param>
    /// <returns><c>true</c> if the listener should listen to the activity source; otherwise, <c>false</c>.</returns>
    /// <seealso cref="ActivityListener.ShouldListenTo" />
    bool ShouldListenTo(ActivitySource activitySource);
}
