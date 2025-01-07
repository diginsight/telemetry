using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics;

public static class ActivityListenerExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ActivityListener ToActivityListener(this IActivityListenerRegistration registration)
    {
        return registration.Logic.ToActivityListener(registration.ShouldListenTo);
    }

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
