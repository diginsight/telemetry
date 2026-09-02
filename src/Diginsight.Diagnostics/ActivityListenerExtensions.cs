using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics;

/// <summary>
/// Provides extension methods for creating <see cref="ActivityListener" /> instances.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ActivityListenerExtensions
{
    /// <summary>
    /// Converts an activity listener registration to an activity listener.
    /// </summary>
    /// <param name="registration">The activity listener registration.</param>
    /// <returns>The created activity listener.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ActivityListener ToActivityListener(this IActivityListenerRegistration registration)
    {
        return registration.Logic.ToActivityListener(registration.ShouldListenTo);
    }

    /// <summary>
    /// Converts activity listener logic and a source predicate to an activity listener.
    /// </summary>
    /// <param name="logic">The activity listener logic.</param>
    /// <param name="shouldListenTo">The predicate used to determine whether an activity source should be listened to.</param>
    /// <returns>The created activity listener.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ActivityListener ToActivityListener(this IActivityListenerLogic logic, Func<ActivitySource, bool> shouldListenTo)
    {
        return new ActivityListener()
        {
            ActivityStarted = logic.ActivityStarted,
            ActivityStopped = logic.ActivityStopped,
            Sample = logic.Sample,
            ShouldListenTo = shouldListenTo,
        };
    }
}
